#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string] $ArtifactsDirectory,
    [Parameter(Mandatory)] [ValidatePattern('^[0-9a-fA-F]{40}$')] [string] $ExpectedRepositoryCommit,
    [Parameter(Mandatory)] [ValidatePattern('^\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')] [string] $ExpectedVersion,
    [Parameter(Mandatory)] [string] $OutputPath,
    [string] $RepositoryRoot = (Split-Path $PSScriptRoot -Parent),
    [string] $GitHubOutputPath
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Read-Report([string] $Path) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "Missing release evidence: $Path" }
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Assert-Equal([string] $Name, $Actual, $Expected) {
    if ($Expected -is [string] -and $Actual -isnot [string]) { throw "Release evidence must contain a string: $Name." }
    if (($Expected -is [int] -or $Expected -is [long]) -and
        $Actual -isnot [int] -and $Actual -isnot [long]) { throw "Release evidence must contain an integer: $Name." }
    if ($null -eq $Actual -or $Actual -cne $Expected) { throw "Release evidence mismatch: $Name." }
}

function Assert-Passed([string] $Name, $Value) {
    if ($Value -isnot [bool] -or -not $Value) { throw "Release evidence did not pass: $Name." }
}

function Get-SourceProperty([xml] $Project, [string] $Name) {
    $values = @($Project.Project.PropertyGroup | ForEach-Object { $_.$Name } | Where-Object { $null -ne $_ })
    if ($values.Count -ne 1 -or [string]::IsNullOrWhiteSpace([string]$values[0])) {
        throw "Expected one explicit source property: $Name."
    }
    return ([string]$values[0]).Trim()
}

function Get-RequiredZipEntry($Archive, [string] $Name) {
    $entries = @($Archive.Entries | Where-Object { $_.FullName -ceq $Name })
    if ($entries.Count -ne 1) { throw "Expected one package entry: $Name." }
    return $entries[0]
}

function Get-EntryHash($Entry) {
    $stream = $Entry.Open()
    try { return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($stream)) }
    finally { $stream.Dispose() }
}

function Assert-PackageEvidence([string] $PackagePath, $Report, $Metadata) {
    $hash = (Get-FileHash -LiteralPath $PackagePath -Algorithm SHA256).Hash
    if ($Report.PackageSHA256 -isnot [string] -or $Report.PackageSHA256 -notmatch '^[0-9A-Fa-f]{64}$' -or
        $hash -ine $Report.PackageSHA256 -or $hash -ine $Metadata.packageSha256) {
        throw 'Validated package SHA256 does not match the candidate bytes.'
    }
    Assert-Equal 'metadata package ID' $Metadata.packageId 'Mythosia.Integrity'
    Assert-Equal 'metadata version' $Metadata.version $ExpectedVersion
    Assert-Equal 'metadata framework' $Metadata.targetFramework 'netstandard2.1'
    Assert-Equal 'metadata commit' $Metadata.repositoryCommit $ExpectedRepositoryCommit

    $archive = [IO.Compression.ZipFile]::OpenRead($PackagePath)
    try {
        foreach ($name in @('README.md', 'RELEASE_NOTES.md', 'lib/netstandard2.1/Mythosia.Integrity.dll')) {
            $entry = Get-RequiredZipEntry $archive $name
            if ((Get-EntryHash $entry) -ine $Metadata.files.PSObject.Properties[$name].Value) {
                throw "Package entry differs from metadata evidence: $name."
            }
        }
        $nuspecEntries = @($archive.Entries | Where-Object { $_.FullName.EndsWith('.nuspec', [StringComparison]::Ordinal) })
        if ($nuspecEntries.Count -ne 1) { throw 'Expected exactly one package nuspec.' }
        $settings = [Xml.XmlReaderSettings]::new()
        $settings.DtdProcessing = [Xml.DtdProcessing]::Prohibit
        $settings.XmlResolver = $null
        $stream = $nuspecEntries[0].Open()
        $reader = [Xml.XmlReader]::Create($stream, $settings)
        try {
            $nuspec = [Xml.XmlDocument]::new()
            $nuspec.XmlResolver = $null
            $nuspec.Load($reader)
            foreach ($field in @('id', 'version', 'repository')) {
                $nodes = $nuspec.SelectNodes("/*[local-name()='package']/*[local-name()='metadata']/*[local-name()='$field']")
                if ($nodes.Count -ne 1) { throw "Expected one nuspec $field." }
                $actual = if ($field -ceq 'repository') { $nodes[0].GetAttribute('commit') } else { $nodes[0].InnerText }
                $expected = switch ($field) {
                    'id' { 'Mythosia.Integrity' }
                    'version' { $ExpectedVersion }
                    'repository' { $ExpectedRepositoryCommit }
                }
                Assert-Equal "nuspec $field" $actual $expected
            }
        }
        finally { $reader.Dispose(); $stream.Dispose() }
    }
    finally { $archive.Dispose() }
    return $hash
}

function Assert-Consumers([string] $ReportDirectory, [string] $Configuration, $Metadata) {
    $consumers = @(Read-Report (Join-Path $ReportDirectory 'consumer-results.json'))
    if ($consumers.Count -ne $expectedCases.Count) { throw 'Incomplete package consumer evidence.' }
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($consumer in $consumers) {
        if ($consumer.Id -isnot [string] -or -not $ids.Add($consumer.Id) -or -not $expectedCases.ContainsKey($consumer.Id)) {
            throw 'Unexpected or duplicate package consumer.'
        }
        Assert-Passed "consumer $($consumer.Id)" $consumer.Passed
        Assert-Equal 'consumer configuration' $consumer.Configuration $Configuration
        if ($consumer.CodeSHA256 -cne $expectedCases[$consumer.Id]) { throw 'Package consumer source differs from the reviewed example.' }
    }
    $api = Read-Report (Join-Path $ReportDirectory 'api-assembly-evidence.json')
    Assert-Passed 'public API' $api.ApiMatches
    Assert-Passed 'assembly versions' $api.VersionMatches
    foreach ($name in @('ExpectedEntryCount', 'EntryCount')) { Assert-Equal $name $api.$name $apiEntries }
    foreach ($name in @('ExpectedTypeCount', 'TypeCount')) { Assert-Equal $name $api.$name $apiTypes }
    Assert-Equal 'assembly version' $api.AssemblyVersion $assemblyVersion
    Assert-Equal 'file version' $api.FileVersion $fileVersion
    $expectedAssemblyHash = $Metadata.files.PSObject.Properties['lib/netstandard2.1/Mythosia.Integrity.dll'].Value
    if ($api.AssemblySHA256 -isnot [string] -or $api.AssemblySHA256 -notmatch '^[0-9A-Fa-f]{64}$' -or
        $api.AssemblySHA256 -ine $expectedAssemblyHash) { throw 'Executed assembly hash differs from the validated package.' }
    if ($api.MissingEntries -isnot [array] -or $api.AddedEntries -isnot [array] -or
        $api.MissingEntries.Count -ne 0 -or $api.AddedEntries.Count -ne 0) { throw 'The public API evidence contains differences.' }
}

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$ArtifactsDirectory = [IO.Path]::GetFullPath($ArtifactsDirectory)
$OutputPath = [IO.Path]::GetFullPath($OutputPath)
$ExpectedRepositoryCommit = $ExpectedRepositoryCommit.ToLowerInvariant()
if (Test-Path -LiteralPath $OutputPath) { throw 'Release selection output must be a new file.' }
[xml]$project = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Mythosia.Integrity/Mythosia.Integrity.csproj') -Raw
Assert-Equal 'source package ID' (Get-SourceProperty $project 'PackageId') 'Mythosia.Integrity'
Assert-Equal 'source package version' (Get-SourceProperty $project 'Version') $ExpectedVersion
$assemblyVersion = Get-SourceProperty $project 'AssemblyVersion'
$fileVersion = Get-SourceProperty $project 'FileVersion'
$manifest = Read-Report (Join-Path $RepositoryRoot 'eng/IntegrityReadme.expected.json')
$expectedCases = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::Ordinal)
foreach ($case in $manifest.cases) { $expectedCases.Add($case.id, $case.codeSha256) }
$expectedCases.Add('public-api', $null)
$apiLines = @([IO.File]::ReadAllLines((Join-Path $RepositoryRoot 'Mythosia.Test/Integrity/PublicApi.approved.txt')) |
    Where-Object { $_.Length -gt 0 -and -not $_.StartsWith('#', [StringComparison]::Ordinal) })
$apiEntries = $apiLines.Count
$apiTypes = @($apiLines | Where-Object { $_.StartsWith('type ', [StringComparison]::Ordinal) }).Count
if ($apiEntries -eq 0 -or $apiTypes -eq 0 -or $manifest.cases.Count -eq 0) { throw 'Release source expectations cannot be empty.' }

$matrix = @(
    @{ Artifact = 'integrity-ubuntu-latest-Debug'; Configuration = 'Debug' },
    @{ Artifact = 'integrity-ubuntu-latest-Release'; Configuration = 'Release' },
    @{ Artifact = 'integrity-windows-latest-Debug'; Configuration = 'Debug' },
    @{ Artifact = 'integrity-windows-latest-Release'; Configuration = 'Release' }
)
$directories = @(Get-ChildItem -LiteralPath $ArtifactsDirectory -Directory)
if ($directories.Count -ne $matrix.Count -or @($directories | Where-Object { $_.Name -cnotin $matrix.Artifact }).Count -ne 0) {
    throw 'Release requires exactly the four expected CI artifact directories.'
}
$verified = [Collections.Generic.List[object]]::new()
$selectedPackage = $null
$selectedHash = $null
foreach ($entry in $matrix) {
    $artifactDirectory = Join-Path $ArtifactsDirectory $entry.Artifact
    & (Join-Path $PSScriptRoot 'Verify-IntegrityTestResults.ps1') -Path (Join-Path $artifactDirectory 'integrity.trx')
    if (-not $?) { throw "Unit test evidence did not pass: $($entry.Artifact)." }
    $reports = @(Get-ChildItem -LiteralPath (Join-Path $artifactDirectory 'package-validation') -Recurse -File -Filter 'package-verification.json')
    if ($reports.Count -ne 1) { throw 'Expected one package validation run per matrix artifact.' }
    $reportDirectory = $reports[0].DirectoryName
    $report = Read-Report $reports[0].FullName
    Assert-Passed 'package verification' $report.Passed
    Assert-Equal 'configuration' $report.Configuration $entry.Configuration
    Assert-Equal 'package ID' $report.PackageId 'Mythosia.Integrity'
    Assert-Equal 'package version' $report.PackageVersion $ExpectedVersion
    Assert-Equal 'repository commit' $report.RepositoryCommit $ExpectedRepositoryCommit
    if ($report.SdkVersion -isnot [string] -or $report.SdkVersion -notmatch '^8\.0\.\d+$') { throw 'Release evidence must use the selected .NET 8 SDK.' }
    Assert-Equal 'README example count' $report.ReadmeExamples $manifest.cases.Count
    Assert-Equal 'API entry count' $report.ApiEntries $apiEntries
    Assert-Equal 'API type count' $report.ApiTypes $apiTypes
    Assert-Equal 'completed consumers' $report.CompletedConsumers $expectedCases.Count
    $metadata = Read-Report (Join-Path $reportDirectory 'metadata-summary.json')
    $packages = @(Get-ChildItem -LiteralPath (Join-Path $reportDirectory 'package') -File -Filter '*.nupkg')
    if ($packages.Count -ne 1) { throw 'Expected exactly one validated package per matrix artifact.' }
    $hash = Assert-PackageEvidence $packages[0].FullName $report $metadata
    Assert-Consumers $reportDirectory $entry.Configuration $metadata
    $verified.Add([pscustomobject]@{ Artifact = $entry.Artifact; Configuration = $entry.Configuration; PackageSHA256 = $hash })
    if ($entry.Artifact -ceq 'integrity-ubuntu-latest-Release') {
        $selectedPackage = $packages[0].FullName
        $selectedHash = $hash
    }
}

# Revalidate the actual publish candidate against the checked-out source. The
# other platforms' raw documentation bytes may differ in their line endings.
& (Join-Path $PSScriptRoot 'Verify-IntegrityPackageMetadata.ps1') -PackagePath $selectedPackage -RepositoryRoot $RepositoryRoot -ExpectedRepositoryCommit $ExpectedRepositoryCommit
if (-not $?) { throw 'The selected publish candidate failed source metadata validation.' }
if ($selectedPackage -match '[\x00-\x1f\x7f]' -or (Get-FileHash -LiteralPath $selectedPackage -Algorithm SHA256).Hash -cne $selectedHash) {
    throw 'The selected package path is invalid or its bytes changed.'
}
$null = [IO.Directory]::CreateDirectory((Split-Path $OutputPath -Parent))
[ordered]@{
    Passed = $true; PackagePath = $selectedPackage; PackageSHA256 = $selectedHash
    PackageVersion = $ExpectedVersion; RepositoryCommit = $ExpectedRepositoryCommit
    VerifiedConfigurations = $verified.Count; Matrix = @($verified.ToArray())
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $OutputPath -Encoding utf8
if (-not [string]::IsNullOrWhiteSpace($GitHubOutputPath)) {
    @("package-path=$selectedPackage", "package-sha256=$selectedHash", "package-version=$ExpectedVersion") |
        Add-Content -LiteralPath $GitHubOutputPath -Encoding utf8
}
Write-Output "Integrity release gate passed: all four CI configurations; Mythosia.Integrity $ExpectedVersion; selected validated Ubuntu Release package."
