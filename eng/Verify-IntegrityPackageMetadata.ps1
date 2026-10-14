#requires -Version 7.0
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $PackagePath,

    [Parameter(Mandatory)]
    [string] $RepositoryRoot,

    [Parameter(Mandatory)]
    [ValidatePattern('^[0-9a-fA-F]{40}$')]
    [string] $ExpectedRepositoryCommit,

    [string] $OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-XmlDocument([byte[]] $Bytes) {
    $settings = [System.Xml.XmlReaderSettings]::new()
    $settings.DtdProcessing = [System.Xml.DtdProcessing]::Prohibit
    $settings.XmlResolver = $null
    $stream = [System.IO.MemoryStream]::new($Bytes, $false)
    $reader = [System.Xml.XmlReader]::Create($stream, $settings)
    try {
        $document = [System.Xml.XmlDocument]::new()
        $document.XmlResolver = $null
        $document.Load($reader)
        return $document
    }
    finally {
        $reader.Dispose()
        $stream.Dispose()
    }
}

function Get-ChildElements([System.Xml.XmlNode] $Parent, [string] $Name) {
    return @($Parent.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and
        $_.NamespaceURI -ceq $Parent.NamespaceURI -and $_.LocalName -ceq $Name
    })
}

function Get-RequiredElement([System.Xml.XmlNode] $Parent, [string] $Name) {
    $elements = @(Get-ChildElements $Parent $Name)
    if ($elements.Count -ne 1) {
        throw "Expected one '$Name' element in '$($Parent.LocalName)', found $($elements.Count)."
    }
    return $elements[0]
}

function Get-SourceProperty([System.Xml.XmlDocument] $Project, [string] $Name) {
    $properties = @(foreach ($group in @(Get-ChildElements $Project.DocumentElement 'PropertyGroup')) {
        Get-ChildElements $group $Name
    })
    if ($properties.Count -ne 1) {
        throw "Expected one literal source property '$Name', found $($properties.Count)."
    }
    $property = $properties[0]
    $value = $property.InnerText.Trim()
    if ([string]::IsNullOrWhiteSpace($value) -or $value.Contains('$(') -or
        $property.HasAttribute('Condition') -or $property.ParentNode.HasAttribute('Condition')) {
        throw "Source property '$Name' must be a non-empty, unconditional literal."
    }
    return $value
}

function Normalize-MetadataText([string] $Value) {
    # XML and pack can normalize line endings; preserve meaningful interior whitespace.
    return $Value.Replace("`r`n", "`n").Replace("`r", "`n").Trim()
}

function Assert-MetadataEqual([string] $Name, [string] $Expected, [string] $Actual) {
    $expectedText = Normalize-MetadataText $Expected
    $actualText = Normalize-MetadataText $Actual
    if (-not [string]::Equals($expectedText, $actualText, [StringComparison]::Ordinal)) {
        throw "Package '$Name' mismatch. Expected '$expectedText'; found '$actualText'."
    }
}

function Assert-SourceReleaseVersions($Project, [string] $ProjectDirectory, [string] $Version) {
    $packageNotes = Get-SourceProperty $Project 'PackageReleaseNotes'
    $firstNotesLine = @($packageNotes -split '\r\n|\r|\n' | Where-Object {
        -not [string]::IsNullOrWhiteSpace($_)
    })[0]
    Assert-MetadataEqual 'PackageReleaseNotes first line version' $Version $firstNotesLine

    $readme = [System.IO.File]::ReadAllText((Join-Path $ProjectDirectory 'README.md'))
    $readmeVersionLine = [regex]::Match($readme, '(?m)^Version\b[^\r\n]*')
    if (-not $readmeVersionLine.Success) { throw 'README is missing its Version line.' }
    $readmeVersion = [regex]::Match($readmeVersionLine.Value, '^Version `([^`]+)`(?:\s|$)')
    if (-not $readmeVersion.Success) { throw 'README Version line must put the version in backticks.' }
    Assert-MetadataEqual 'README version' $Version $readmeVersion.Groups[1].Value

    $releaseNotes = [System.IO.File]::ReadAllText((Join-Path $ProjectDirectory 'RELEASE_NOTES.md'))
    $firstHeading = [regex]::Match($releaseNotes, '(?m)^##[ \t]+([^\r\n]+)')
    if (-not $firstHeading.Success) { throw 'Release notes are missing their version heading.' }
    $heading = [regex]::Match($firstHeading.Groups[1].Value.Trim(), '^(\S+)[ \t]+-[ \t]+(\d{4}-\d{2}-\d{2})$')
    if (-not $heading.Success) { throw 'First release heading must be: ## VERSION - yyyy-MM-dd.' }
    Assert-MetadataEqual 'release notes heading version' $Version $heading.Groups[1].Value
    $releaseDate = [datetime]::MinValue
    if (-not [datetime]::TryParseExact($heading.Groups[2].Value, 'yyyy-MM-dd',
        [System.Globalization.CultureInfo]::InvariantCulture,
        [System.Globalization.DateTimeStyles]::None, [ref] $releaseDate)) {
        throw "Release notes contain an invalid release date: '$($heading.Groups[2].Value)'."
    }
}

function Read-EntryBytes([System.IO.Compression.ZipArchiveEntry] $Entry) {
    $inputStream = $Entry.Open()
    $buffer = [System.IO.MemoryStream]::new()
    try {
        $inputStream.CopyTo($buffer)
        return ,$buffer.ToArray()
    }
    finally {
        $buffer.Dispose()
        $inputStream.Dispose()
    }
}

function Get-StreamSha256([System.IO.Stream] $Stream) {
    $algorithm = [System.Security.Cryptography.SHA256]::Create()
    try {
        return [BitConverter]::ToString($algorithm.ComputeHash($Stream)).Replace('-', '').ToLowerInvariant()
    }
    finally {
        $algorithm.Dispose()
    }
}

function Get-FileSha256([string] $Path) {
    $stream = [System.IO.File]::OpenRead($Path)
    try { return Get-StreamSha256 $stream }
    finally { $stream.Dispose() }
}

function Get-EntrySha256([System.IO.Compression.ZipArchiveEntry] $Entry) {
    $stream = $Entry.Open()
    try { return Get-StreamSha256 $stream }
    finally { $stream.Dispose() }
}

function Get-SafePackageEntries([System.IO.Compression.ZipArchive] $Archive) {
    $entries = [System.Collections.Generic.Dictionary[string, System.IO.Compression.ZipArchiveEntry]]::new(
        [StringComparer]::Ordinal)
    $seenNames = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($entry in $Archive.Entries) {
        $name = $entry.FullName
        $normalizedName = $name.TrimEnd('/')
        $segments = $normalizedName.Split('/')
        if ([string]::IsNullOrEmpty($normalizedName) -or $name.Contains('\') -or
            $name.StartsWith('/', [StringComparison]::Ordinal) -or $name.Contains(':') -or
            @($segments | Where-Object { $_ -in @('', '.', '..') }).Count -gt 0 -or
            $name -match '[\x00-\x1f]') {
            throw "Unsafe package entry path: '$name'."
        }
        # Reject case collisions too: the same archive must behave identically on Windows and Linux.
        if (-not $seenNames.Add($normalizedName)) {
            throw "Duplicate or case-colliding package entry: '$name'."
        }
        if ($name.EndsWith('/', [StringComparison]::Ordinal)) { continue }
        $entries.Add($name, $entry)
    }
    return ,$entries
}

function Get-RequiredEntry($Entries, [string] $Name) {
    if (-not $Entries.ContainsKey($Name)) { throw "Package is missing '$Name'." }
    return $Entries[$Name]
}

function Assert-PackageLayout($Entries, [string[]] $RequiredPaths) {
    foreach ($path in $RequiredPaths) { $null = Get-RequiredEntry $Entries $path }
    $corePropertyCount = 0
    foreach ($name in $Entries.Keys) {
        if ($RequiredPaths -ccontains $name -or $name -ceq '.signature.p7s') { continue }
        if ($name -cmatch '^package/services/metadata/core-properties/[0-9a-fA-F]{32}\.psmdcp$') {
            $corePropertyCount++
            continue
        }
        throw "Unexpected package payload '$name'; referenced-project DLLs and extra library files are not allowed."
    }
    if ($corePropertyCount -ne 1) {
        throw "Expected one NuGet core-properties entry, found $corePropertyCount."
    }
}

function Assert-PackDocument($Entries, [string] $EntryPath, [string] $SourcePath) {
    if (-not (Test-Path -LiteralPath $SourcePath -PathType Leaf)) {
        throw "Source package document is missing: $SourcePath"
    }
    $entry = Get-RequiredEntry $Entries $EntryPath
    $sourceBytes = [System.IO.File]::ReadAllBytes($SourcePath)
    if ($entry.Length -ne $sourceBytes.LongLength) {
        throw "Packaged '$EntryPath' differs in length from its source file."
    }
    $packageBytes = Read-EntryBytes $entry
    if (-not [System.Collections.StructuralComparisons]::StructuralEqualityComparer.Equals($sourceBytes, $packageBytes)) {
        throw "Packaged '$EntryPath' does not match the source file byte for byte."
    }
    return Get-EntrySha256 $entry
}

if (-not (Test-Path -LiteralPath $PackagePath -PathType Leaf)) { throw "Package is missing: $PackagePath" }
if (-not (Test-Path -LiteralPath $RepositoryRoot -PathType Container)) {
    throw "Repository root is missing: $RepositoryRoot"
}
$resolvedPackagePath = (Resolve-Path -LiteralPath $PackagePath).ProviderPath
$resolvedRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$projectDirectory = Join-Path $resolvedRoot 'Mythosia.Integrity'
$projectPath = Join-Path $projectDirectory 'Mythosia.Integrity.csproj'
$dependencyProjectPath = Join-Path $resolvedRoot 'Mythosia/Mythosia.csproj'
$project = Read-XmlDocument ([System.IO.File]::ReadAllBytes($projectPath))
$dependencyProject = Read-XmlDocument ([System.IO.File]::ReadAllBytes($dependencyProjectPath))
foreach ($document in @($project, $dependencyProject)) {
    if ($document.DocumentElement.LocalName -cne 'Project') { throw 'Source XML must be an MSBuild Project.' }
    if ($document.DocumentElement.NamespaceURI -cnotin @('', 'http://schemas.microsoft.com/developer/msbuild/2003')) {
        throw 'Unexpected MSBuild project XML namespace.'
    }
}

$packageId = Get-SourceProperty $project 'PackageId'
$version = Get-SourceProperty $project 'Version'
Assert-SourceReleaseVersions $project $projectDirectory $version
$targetFramework = Get-SourceProperty $project 'TargetFramework'
Assert-MetadataEqual 'source target framework' 'netstandard2.1' $targetFramework
$dependencyVersion = Get-SourceProperty $dependencyProject 'Version'
$projectReferences = @(foreach ($group in @(Get-ChildElements $project.DocumentElement 'ItemGroup')) {
    Get-ChildElements $group 'ProjectReference'
})
if ($projectReferences.Count -ne 1) { throw 'Integrity must retain exactly one Mythosia project dependency.' }
$referencePath = [System.IO.Path]::GetFullPath((Join-Path $projectDirectory $projectReferences[0].GetAttribute('Include').Replace('\', '/')))
$expectedReferencePath = [System.IO.Path]::GetFullPath($dependencyProjectPath)
$pathComparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
if (-not [string]::Equals($referencePath, $expectedReferencePath, $pathComparison)) {
    throw "Unexpected Integrity project dependency: $referencePath"
}

$readmePath = Get-SourceProperty $project 'PackageReadmeFile'
Assert-MetadataEqual 'source readme path' 'README.md' $readmePath
$assemblyPath = "lib/$targetFramework/Mythosia.Integrity.dll"
$nuspecPath = "$packageId.nuspec"
$requiredPaths = @('_rels/.rels', '[Content_Types].xml', $nuspecPath, $assemblyPath, $readmePath, 'RELEASE_NOTES.md')
$archive = [System.IO.Compression.ZipFile]::OpenRead($resolvedPackagePath)
try {
    $entries = Get-SafePackageEntries $archive
    Assert-PackageLayout $entries $requiredPaths
    $nuspec = Read-XmlDocument (Read-EntryBytes (Get-RequiredEntry $entries $nuspecPath))
    if ($nuspec.DocumentElement.LocalName -cne 'package') { throw 'Nuspec root must be package.' }
    if ($nuspec.DocumentElement.NamespaceURI -cnotmatch '^http://schemas\.microsoft\.com/packaging/\d{4}/\d{2}/nuspec\.xsd$') {
        throw 'Unexpected nuspec XML namespace.'
    }
    $metadata = Get-RequiredElement $nuspec.DocumentElement 'metadata'
    $sourcePropertyMap = [ordered]@{
        id = 'PackageId'; version = 'Version'; title = 'Title'; authors = 'Authors'
        license = 'PackageLicenseExpression'; readme = 'PackageReadmeFile'
        projectUrl = 'PackageProjectUrl'; description = 'Description'
        releaseNotes = 'PackageReleaseNotes'; tags = 'PackageTags'
    }
    foreach ($field in $sourcePropertyMap.Keys) {
        $element = Get-RequiredElement $metadata $field
        Assert-MetadataEqual $field (Get-SourceProperty $project $sourcePropertyMap[$field]) $element.InnerText
    }
    $license = Get-RequiredElement $metadata 'license'
    Assert-MetadataEqual 'license type' 'expression' $license.GetAttribute('type')
    $repository = Get-RequiredElement $metadata 'repository'
    Assert-MetadataEqual 'repository type' (Get-SourceProperty $project 'RepositoryType') $repository.GetAttribute('type')
    Assert-MetadataEqual 'repository URL' (Get-SourceProperty $project 'RepositoryUrl') $repository.GetAttribute('url')
    Assert-MetadataEqual 'repository commit' $ExpectedRepositoryCommit.ToLowerInvariant() $repository.GetAttribute('commit')

    $dependencies = Get-RequiredElement $metadata 'dependencies'
    if (@($dependencies.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and
        ($_.LocalName -cne 'group' -or $_.NamespaceURI -cne $dependencies.NamespaceURI)
    }).Count -gt 0) { throw 'Dependencies must contain only the expected framework group.' }
    $group = Get-RequiredElement $dependencies 'group'
    Assert-MetadataEqual 'dependency target framework' '.NETStandard2.1' $group.GetAttribute('targetFramework')
    $dependency = Get-RequiredElement $group 'dependency'
    if (@($group.ChildNodes | Where-Object {
        $_ -is [System.Xml.XmlElement] -and
        ($_.LocalName -cne 'dependency' -or $_.NamespaceURI -cne $group.NamespaceURI)
    }).Count -gt 0) { throw 'Dependency group contains unexpected elements.' }
    Assert-MetadataEqual 'dependency ID' 'Mythosia' $dependency.GetAttribute('id')
    $actualDependencyVersion = $dependency.GetAttribute('version')
    if ($actualDependencyVersion -cne $dependencyVersion -and $actualDependencyVersion -cne "[$dependencyVersion,)") {
        throw "Expected dependency Mythosia >= $dependencyVersion, found '$actualDependencyVersion'."
    }
    if ($dependency.HasAttribute('include')) { throw 'Unexpected dependency include filter.' }
    Assert-MetadataEqual 'dependency exclude filter' 'Build,Analyzers' $dependency.GetAttribute('exclude')

    $readmeHash = Assert-PackDocument $entries $readmePath (Join-Path $projectDirectory $readmePath)
    $releaseNotesHash = Assert-PackDocument $entries 'RELEASE_NOTES.md' (Join-Path $projectDirectory 'RELEASE_NOTES.md')
    $assemblyEntry = Get-RequiredEntry $entries $assemblyPath
    if ($assemblyEntry.Length -eq 0) { throw 'Integrity package assembly is empty.' }
    $assemblyHash = Get-EntrySha256 $assemblyEntry
}
finally { $archive.Dispose() }

$summary = [ordered]@{
    packageId = $packageId
    version = $version
    targetFramework = $targetFramework
    repositoryCommit = $ExpectedRepositoryCommit.ToLowerInvariant()
    packageSha256 = Get-FileSha256 $resolvedPackagePath
    dependency = [ordered]@{ id = 'Mythosia'; minimumVersion = $dependencyVersion }
    files = [ordered]@{
        $readmePath = $readmeHash
        'RELEASE_NOTES.md' = $releaseNotesHash
        $assemblyPath = $assemblyHash
    }
}
if (-not [string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $resolvedOutputDirectory = [System.IO.Path]::GetFullPath($OutputDirectory)
    $null = [System.IO.Directory]::CreateDirectory($resolvedOutputDirectory)
    $summaryPath = Join-Path $resolvedOutputDirectory 'metadata-summary.json'
    $json = ($summary | ConvertTo-Json -Depth 5).Replace("`r`n", "`n") + "`n"
    [System.IO.File]::WriteAllText($summaryPath, $json, [System.Text.UTF8Encoding]::new($false))
}
Write-Output "Integrity package metadata verified: $packageId $version; $targetFramework; Mythosia >= $dependencyVersion; repository $ExpectedRepositoryCommit."
