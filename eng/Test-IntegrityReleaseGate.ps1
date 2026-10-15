#requires -Version 7.0
<#
.SYNOPSIS
Exercises release-package selection with isolated, deliberately altered artifacts.
.DESCRIPTION
Default fixtures are synthetic CI evidence, not proof that CI or consumers ran.
The package is a real ZIP with source metadata and documents, plus a nonempty
test assembly payload. No build, restore, network, credentials, or publication
is used. FixtureDirectory optionally seeds the tests from one existing package
validation run; its reports are copied and configurations are simulated.
TestResultsPath optionally supplies an existing TRX instead of synthetic tests.
All generated files are retained in a fresh directory for inspection.
#>
[CmdletBinding()]
param(
    [string] $RepositoryRoot = (Split-Path -Parent $PSScriptRoot),
    [string] $FixtureDirectory,
    [string] $TestResultsPath,
    [string] $OutputDirectory = ([IO.Path]::GetTempPath())
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$matrix = @(
    @{ Name = 'integrity-ubuntu-latest-Debug'; Configuration = 'Debug' },
    @{ Name = 'integrity-ubuntu-latest-Release'; Configuration = 'Release' },
    @{ Name = 'integrity-windows-latest-Debug'; Configuration = 'Debug' },
    @{ Name = 'integrity-windows-latest-Release'; Configuration = 'Release' }
)
$selectedArtifact = 'integrity-ubuntu-latest-Release'
$runName = 'run-00000000000000000000000000000001'
$minimumTestCount = 3478
$apiTestName = 'Mythosia.Test.Integrity.IntegrityPublicApiTests.PublicApi_MatchesApprovedSnapshot'
$crc32CTestPrefix = 'Mythosia.Test.Integrity.Crc32CTests.'
$crc32CStreamTestPrefix = 'Mythosia.Test.Integrity.Crc32CStreamTests.'
$crc64TestPrefix = 'Mythosia.Test.Integrity.Crc64Tests.'
$crc16VariantTestPrefix = 'Mythosia.Test.Integrity.Crc16VariantTests.'
$adler32TestPrefix = 'Mythosia.Test.Integrity.Adler32Tests.'
$adler32StreamTestPrefix = 'Mythosia.Test.Integrity.Adler32StreamTests.'
$crc64VariantTestPrefix = 'Mythosia.Test.Integrity.Crc64VariantTests.'
$internetChecksumTestPrefix = 'Mythosia.Test.Integrity.InternetChecksumTests.'
$internetChecksumStreamTestPrefix = 'Mythosia.Test.Integrity.InternetChecksumStreamTests.'
$crc8VariantTestPrefix = 'Mythosia.Test.Integrity.Crc8VariantTests.'
$crc8VariantStreamTestPrefix = 'Mythosia.Test.Integrity.Crc8VariantStreamTests.'
$remainingCrcVariantsTestPrefix = 'Mythosia.Test.Integrity.RemainingCrcVariantsTests.'
$remainingCrcVariantsStreamTestPrefix = 'Mythosia.Test.Integrity.RemainingCrcVariantsStreamTests.'
$xxHashTestPrefix = 'Mythosia.Test.Integrity.XxHashTests.'
$xxHashStreamTestPrefix = 'Mythosia.Test.Integrity.XxHashStreamTests.'
$fletcher4TestPrefix = 'Mythosia.Test.Integrity.Fletcher4Tests.'
$fletcher4StreamTestPrefix = 'Mythosia.Test.Integrity.Fletcher4StreamTests.'
$ospfLsaTestPrefix = 'Mythosia.Test.Integrity.OspfLsaChecksumTests.'
$githubSentinel = "existing-output=preserved`n"
$utf8 = [Text.UTF8Encoding]::new($false)

function Write-Json([string] $Path, $Value) {
    [IO.File]::WriteAllText($Path, (ConvertTo-Json -InputObject $Value -Depth 20) + "`n", $utf8)
}

function Read-Json([string] $Path) {
    return Get-Content -LiteralPath $Path -Raw | ConvertFrom-Json
}

function Get-Hash([string] $Path) {
    return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash
}

function Get-ProjectProperty([xml] $Project, [string] $Name) {
    $properties = @($Project.SelectNodes("/Project/PropertyGroup/$Name"))
    if ($properties.Count -ne 1) { throw "Fixture source needs exactly one '$Name' property." }
    return $properties[0].InnerText.Trim()
}

function Add-ZipBytes($Archive, [string] $Name, [byte[]] $Bytes) {
    $entry = $Archive.CreateEntry($Name)
    $entry.LastWriteTime = [DateTimeOffset]::new(2020, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
    $stream = $entry.Open()
    try { $stream.Write($Bytes, 0, $Bytes.Length) }
    finally { $stream.Dispose() }
}

function New-SyntheticPackage([string] $Path, [string] $Commit) {
    $fieldMap = [ordered]@{
        id = 'PackageId'; version = 'Version'; title = 'Title'; authors = 'Authors'
        license = 'PackageLicenseExpression'; readme = 'PackageReadmeFile'
        projectUrl = 'PackageProjectUrl'; description = 'Description'
        releaseNotes = 'PackageReleaseNotes'; tags = 'PackageTags'
    }
    $fields = foreach ($name in $fieldMap.Keys) {
        $value = [Security.SecurityElement]::Escape((Get-ProjectProperty $project $fieldMap[$name]))
        $attribute = if ($name -ceq 'license') { ' type="expression"' } else { '' }
        "<$name$attribute>$value</$name>"
    }
    $repositoryType = [Security.SecurityElement]::Escape((Get-ProjectProperty $project 'RepositoryType'))
    $repositoryUrl = [Security.SecurityElement]::Escape((Get-ProjectProperty $project 'RepositoryUrl'))
    $dependencyVersion = [Security.SecurityElement]::Escape($dependencyVersion)
    $nuspec = @"
<?xml version="1.0" encoding="utf-8"?>
<package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
  <metadata>
    $($fields -join "`n    ")
    <repository type="$repositoryType" url="$repositoryUrl" commit="$Commit" />
    <dependencies><group targetFramework=".NETStandard2.1">
      <dependency id="Mythosia" version="$dependencyVersion" exclude="Build,Analyzers" />
    </group></dependencies>
  </metadata>
</package>
"@
    $archive = [IO.Compression.ZipFile]::Open($Path, [IO.Compression.ZipArchiveMode]::Create)
    try {
        Add-ZipBytes $archive '_rels/.rels' ($utf8.GetBytes('<Relationships />'))
        Add-ZipBytes $archive '[Content_Types].xml' ($utf8.GetBytes('<Types />'))
        Add-ZipBytes $archive 'Mythosia.Integrity.nuspec' ($utf8.GetBytes($nuspec))
        Add-ZipBytes $archive 'lib/netstandard2.1/Mythosia.Integrity.dll' ($utf8.GetBytes('Synthetic gate-test assembly payload; never published.'))
        foreach ($name in @('README.md', 'RELEASE_NOTES.md')) {
            Add-ZipBytes $archive $name ([IO.File]::ReadAllBytes((Join-Path $RepositoryRoot "Mythosia.Integrity/$name")))
        }
        Add-ZipBytes $archive 'package/services/metadata/core-properties/00000000000000000000000000000001.psmdcp' ($utf8.GetBytes('<coreProperties />'))
    }
    finally { $archive.Dispose() }
}

function New-PassingTrx([string] $Path) {
    $document = [Text.StringBuilder]::new()
    [void] $document.Append('<TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>')
    for ($index = 0; $index -lt $minimumTestCount; $index++) {
        $name = switch ($index) {
            0 { $apiTestName }
            1 { "${crc32CTestPrefix}SyntheticGateFixture" }
            2 { "${crc32CStreamTestPrefix}SyntheticGateFixture" }
            3 { "${crc64TestPrefix}SyntheticGateFixture" }
            4 { "${crc16VariantTestPrefix}SyntheticGateFixture" }
            5 { "${adler32TestPrefix}SyntheticGateFixture" }
            6 { "${adler32StreamTestPrefix}SyntheticGateFixture" }
            7 { "${crc64VariantTestPrefix}SyntheticGateFixture" }
            8 { "${internetChecksumTestPrefix}SyntheticGateFixture" }
            9 { "${internetChecksumStreamTestPrefix}SyntheticGateFixture" }
            10 { "${crc8VariantTestPrefix}SyntheticGateFixture" }
            11 { "${crc8VariantStreamTestPrefix}SyntheticGateFixture" }
            12 { "${remainingCrcVariantsTestPrefix}SyntheticGateFixture" }
            13 { "${remainingCrcVariantsStreamTestPrefix}SyntheticGateFixture" }
            14 { "${xxHashTestPrefix}SyntheticGateFixture" }
            15 { "${xxHashStreamTestPrefix}SyntheticGateFixture" }
            16 { "${fletcher4TestPrefix}SyntheticGateFixture" }
            17 { "${fletcher4StreamTestPrefix}SyntheticGateFixture" }
            18 { "${ospfLsaTestPrefix}SyntheticGateFixture" }
            default { "Mythosia.Test.Integrity.SyntheticGateFixture.Case$index" }
        }
        [void] $document.Append("<UnitTestResult testName=`"$name`" outcome=`"Passed`" />")
    }
    [void] $document.Append('</Results><ResultSummary outcome="Completed"><Counters ')
    [void] $document.Append("total=`"$minimumTestCount`" executed=`"$minimumTestCount`" passed=`"$minimumTestCount`" failed=`"0`" notExecuted=`"0`" ")
    [void] $document.Append('error="0" timeout="0" aborted="0" inconclusive="0" passedButRunAborted="0" notRunnable="0" disconnected="0" warning="0" completed="0" inProgress="0" pending="0" /></ResultSummary></TestRun>')
    [IO.File]::WriteAllText($Path, $document.ToString(), $utf8)
}

function New-SyntheticSeed([string] $Directory) {
    $packageDirectory = Join-Path $Directory 'package'
    [void] [IO.Directory]::CreateDirectory($packageDirectory)
    $packagePath = Join-Path $packageDirectory "Mythosia.Integrity.$version.nupkg"
    $commit = 'a' * 40
    New-SyntheticPackage $packagePath $commit
    $null = & $metadataVerifier -PackagePath $packagePath -RepositoryRoot $RepositoryRoot -ExpectedRepositoryCommit $commit -OutputDirectory $Directory
    $metadata = Read-Json (Join-Path $Directory 'metadata-summary.json')
    $packageHash = Get-Hash $packagePath
    # The C# consumer writes uppercase hashes; the metadata verifier writes lowercase.
    $assemblyHash = $metadata.files.'lib/netstandard2.1/Mythosia.Integrity.dll'.ToUpperInvariant()
    $dependencyHash = 'c' * 64
    $packages = @(
        @{ Id = 'Mythosia.Integrity'; Version = $version; Source = (Join-Path $Directory 'candidate-feed'); ArchiveSHA256 = $packageHash; RuntimeAssemblySHA256 = $assemblyHash },
        @{ Id = 'Mythosia'; Version = $dependencyVersion; Source = 'https://api.nuget.org/v3/index.json'; ArchiveSHA256 = $dependencyHash; RuntimeAssemblySHA256 = $dependencyHash }
    )
    $consumers = @(foreach ($case in $manifest.cases) {
        @{ Id = $case.id; Configuration = 'Release'; Passed = $true; CodeSHA256 = $case.codeSha256; Packages = $packages }
    })
    $consumers += @{ Id = 'public-api'; Configuration = 'Release'; Passed = $true; CodeSHA256 = $null; Packages = $packages }
    Write-Json (Join-Path $Directory 'consumer-results.json') $consumers
    Write-Json (Join-Path $Directory 'package-verification.json') @{
        Passed = $true; Configuration = 'Release'; SdkVersion = '8.0.425'; RepositoryCommit = $commit
        PackageId = 'Mythosia.Integrity'; PackageVersion = $version; PackageSHA256 = $packageHash
        ReadmeExamples = $manifest.cases.Count; ApiEntries = $approved.Count; ApiTypes = $typeCount
        CompletedConsumers = $consumers.Count; WorkDirectory = $Directory
    }
    Write-Json (Join-Path $Directory 'api-assembly-evidence.json') @{
        AssemblyName = 'Mythosia.Integrity'; AssemblyVersion = $assemblyVersion; FileVersion = $fileVersion
        AssemblyPath = (Join-Path $Directory 'fixture/Mythosia.Integrity.dll'); AssemblySHA256 = $assemblyHash
        MythosiaAssemblyName = 'Mythosia'; MythosiaAssemblyVersion = "$dependencyVersion.0"; MythosiaFileVersion = "$dependencyVersion.0"
        MythosiaAssemblyPath = (Join-Path $Directory 'fixture/Mythosia.dll'); MythosiaAssemblySHA256 = $dependencyHash
        ApprovedSnapshotPath = $approvedPath; ApprovedSnapshotSHA256 = (Get-Hash $approvedPath)
        ExpectedAssemblyVersion = $assemblyVersion; ExpectedFileVersion = $fileVersion
        ExpectedEntryCount = $approved.Count; EntryCount = $approved.Count; ExpectedTypeCount = $typeCount; TypeCount = $typeCount
        ApiMatches = $true; VersionMatches = $true; MissingEntries = @(); AddedEntries = @()
    }
}

function Copy-ExistingSeed([string] $Directory) {
    $reports = @(Get-ChildItem -LiteralPath $FixtureDirectory -Recurse -File -Filter 'package-verification.json')
    if ($reports.Count -ne 1) { throw 'FixtureDirectory must contain exactly one package validation run.' }
    $sourceRun = $reports[0].DirectoryName
    foreach ($name in @('package-verification.json', 'metadata-summary.json', 'consumer-results.json', 'api-assembly-evidence.json')) {
        Copy-Item -LiteralPath (Join-Path $sourceRun $name) -Destination $Directory
    }
    $packages = @(Get-ChildItem -LiteralPath (Join-Path $sourceRun 'package') -File -Filter '*.nupkg')
    if ($packages.Count -ne 1) { throw 'FixtureDirectory needs exactly one candidate package in its run/package directory.' }
    $packageDirectory = Join-Path $Directory 'package'
    [void] [IO.Directory]::CreateDirectory($packageDirectory)
    Copy-Item -LiteralPath $packages[0].FullName -Destination $packageDirectory
}

function New-MatrixFixture([string] $Directory) {
    foreach ($entry in $matrix) {
        $artifact = Join-Path $Directory $entry.Name
        $run = Join-Path $artifact "package-validation/$runName"
        [void] [IO.Directory]::CreateDirectory($run)
        foreach ($item in @(Get-ChildItem -LiteralPath $seedDirectory)) {
            Copy-Item -LiteralPath $item.FullName -Destination $run -Recurse
        }
        $reportPath = Join-Path $run 'package-verification.json'
        $report = Read-Json $reportPath
        $report.Configuration = $entry.Configuration
        Write-Json $reportPath $report
        $consumerPath = Join-Path $run 'consumer-results.json'
        $consumers = @(Read-Json $consumerPath)
        foreach ($consumer in $consumers) { $consumer.Configuration = $entry.Configuration }
        Write-Json $consumerPath $consumers
        $trxPath = Join-Path $artifact 'integrity.trx'
        if ([string]::IsNullOrWhiteSpace($TestResultsPath)) { New-PassingTrx $trxPath }
        else { Copy-Item -LiteralPath $TestResultsPath -Destination $trxPath }
    }
}

function Get-RunPath($Context, [string] $Artifact = $selectedArtifact) {
    return Join-Path $Context.ArtifactsDirectory "$Artifact/package-validation/$runName"
}

function Edit-Json($Context, [string] $Name, [scriptblock] $Change) {
    $path = Join-Path (Get-RunPath $Context) $Name
    $value = Read-Json $path
    $changed = & $Change $value
    Write-Json $path $changed
}

function Edit-Trx($Context, [scriptblock] $Change) {
    $path = Join-Path $Context.ArtifactsDirectory "$selectedArtifact/integrity.trx"
    [xml] $document = Get-Content -LiteralPath $path -Raw
    & $Change $document
    $document.Save($path)
}

function Get-CandidatePath($Context) {
    $packages = @(Get-ChildItem -LiteralPath (Join-Path (Get-RunPath $Context) 'package') -File -Filter '*.nupkg')
    if ($packages.Count -ne 1) { throw 'Test mutation expected one candidate.' }
    return $packages[0].FullName
}

function Edit-ArchiveText($Context, [string] $Name, [scriptblock] $Change) {
    $archive = [IO.Compression.ZipFile]::Open((Get-CandidatePath $Context), [IO.Compression.ZipArchiveMode]::Update)
    try {
        $entry = $archive.GetEntry($Name)
        $reader = [IO.StreamReader]::new($entry.Open(), $utf8)
        try { $text = $reader.ReadToEnd() }
        finally { $reader.Dispose() }
        $entry.Delete()
        Add-ZipBytes $archive $Name ($utf8.GetBytes((& $Change $text)))
    }
    finally { $archive.Dispose() }
    # Keep claimed archive hashes consistent so the changed nuspec, rather than
    # an unrelated digest mismatch, is the reason this fixture is rejected.
    $hash = Get-Hash (Get-CandidatePath $Context)
    Edit-Json $Context 'package-verification.json' { param($value) $value.PackageSHA256 = $hash; $value }
    Edit-Json $Context 'metadata-summary.json' { param($value) $value.packageSha256 = $hash; $value }
    Edit-Json $Context 'consumer-results.json' {
        param($value)
        foreach ($consumer in @($value)) {
            foreach ($package in $consumer.Packages) {
                if ($package.Id -ceq 'Mythosia.Integrity') { $package.ArchiveSHA256 = $hash }
            }
        }
        ,@($value)
    }
}

function Assert-SelectedPackage($Context) {
    if (-not (Test-Path -LiteralPath $Context.OutputPath -PathType Leaf)) { throw 'Successful gate did not write its report.' }
    $report = Read-Json $Context.OutputPath
    $packagePath = Get-CandidatePath $Context
    $hash = Get-Hash $packagePath
    if ($report.Passed -isnot [bool] -or -not $report.Passed -or $report.VerifiedConfigurations -ne 4 -or
        $report.PackagePath -cne $packagePath -or $report.PackageSHA256 -ine $hash -or
        $report.PackageVersion -cne $version -or $report.RepositoryCommit -cne $expectedCommit) {
        throw 'Successful gate did not select the exact Ubuntu Release candidate with all four configurations.'
    }
    $lines = @(Get-Content -LiteralPath $Context.GitHubOutputPath)
    if ($lines.Count -ne 4 -or $lines[0] -cne 'existing-output=preserved' -or
        $lines -cnotcontains "package-path=$packagePath" -or
        $lines -cnotcontains "package-version=$version" -or
        @($lines | Where-Object { $_ -imatch "^package-sha256=$hash$" }).Count -ne 1) {
        throw 'Successful gate did not append the selected package outputs exactly once.'
    }
}

function Invoke-GateCase($Case) {
    $caseDirectory = Join-Path $workDirectory $Case.Name
    $artifacts = Join-Path $caseDirectory 'artifacts'
    [void] [IO.Directory]::CreateDirectory($artifacts)
    foreach ($item in @(Get-ChildItem -LiteralPath $baselineDirectory)) {
        Copy-Item -LiteralPath $item.FullName -Destination $artifacts -Recurse
    }
    $context = [pscustomobject]@{
        Directory = $caseDirectory; ArtifactsDirectory = $artifacts
        OutputPath = (Join-Path $caseDirectory 'release-gate.json')
        GitHubOutputPath = (Join-Path $caseDirectory 'github-output.txt')
        ExpectedVersion = $version; ExpectedRepositoryCommit = $expectedCommit
    }
    [IO.File]::WriteAllText($context.GitHubOutputPath, $githubSentinel, $utf8)
    if ($null -ne $Case.Change) { & $Case.Change $context }
    $rejection = $null
    $log = Join-Path $caseDirectory 'selector.log'
    try {
        & $selector -ArtifactsDirectory $artifacts -RepositoryRoot $RepositoryRoot -ExpectedRepositoryCommit $context.ExpectedRepositoryCommit -ExpectedVersion $context.ExpectedVersion -OutputPath $context.OutputPath -GitHubOutputPath $context.GitHubOutputPath *> $log
    }
    catch {
        $rejection = $_.Exception.Message
        [IO.File]::AppendAllText($log, "`n$rejection`n", $utf8)
    }
    if ($Case.ExpectSuccess) {
        if ($null -ne $rejection) { throw "Valid fixture was rejected: $rejection" }
        Assert-SelectedPackage $context
        return 'Exact Ubuntu Release package selected; four configurations verified.'
    }
    if ($null -eq $rejection) { throw 'Invalid fixture was accepted.' }
    if ((Test-Path -LiteralPath $context.OutputPath) -or
        [IO.File]::ReadAllText($context.GitHubOutputPath) -cne $githubSentinel) {
        throw 'Rejected fixture produced release output.'
    }
    return $rejection
}

$RepositoryRoot = (Resolve-Path -LiteralPath $RepositoryRoot).ProviderPath
$selector = Join-Path $PSScriptRoot 'Select-IntegrityReleasePackage.ps1'
$metadataVerifier = Join-Path $PSScriptRoot 'Verify-IntegrityPackageMetadata.ps1'
if (-not (Test-Path -LiteralPath $selector -PathType Leaf)) { throw "Release selector is missing: $selector" }
if (-not [string]::IsNullOrWhiteSpace($FixtureDirectory)) {
    $FixtureDirectory = (Resolve-Path -LiteralPath $FixtureDirectory).ProviderPath
}
if (-not [string]::IsNullOrWhiteSpace($TestResultsPath)) {
    $TestResultsPath = (Resolve-Path -LiteralPath $TestResultsPath).ProviderPath
    $null = & (Join-Path $PSScriptRoot 'Verify-IntegrityTestResults.ps1') -Path $TestResultsPath
}
[xml] $project = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Mythosia.Integrity/Mythosia.Integrity.csproj') -Raw
[xml] $dependencyProject = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Mythosia/Mythosia.csproj') -Raw
$version = Get-ProjectProperty $project 'Version'
$assemblyVersion = Get-ProjectProperty $project 'AssemblyVersion'
$fileVersion = Get-ProjectProperty $project 'FileVersion'
$dependencyVersion = Get-ProjectProperty $dependencyProject 'Version'
$manifest = Read-Json (Join-Path $RepositoryRoot 'eng/IntegrityReadme.expected.json')
$approvedPath = Join-Path $RepositoryRoot 'Mythosia.Test/Integrity/PublicApi.approved.txt'
$approved = @([IO.File]::ReadAllLines($approvedPath) | Where-Object { $_.Length -gt 0 -and -not $_.StartsWith('#', [StringComparison]::Ordinal) })
$typeCount = @($approved | Where-Object { $_.StartsWith('type ', [StringComparison]::Ordinal) }).Count
$workDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ('IntegrityReleaseGateTests-' + [guid]::NewGuid().ToString('N'))
$seedDirectory = Join-Path $workDirectory 'seed'
$baselineDirectory = Join-Path $workDirectory 'baseline'
[void] [IO.Directory]::CreateDirectory($seedDirectory)
if ([string]::IsNullOrWhiteSpace($FixtureDirectory)) { New-SyntheticSeed $seedDirectory }
else { Copy-ExistingSeed $seedDirectory }
$seedReport = Read-Json (Join-Path $seedDirectory 'package-verification.json')
$expectedCommit = $seedReport.RepositoryCommit
if ($seedReport.PackageVersion -cne $version) { throw 'Existing fixture version differs from the repository source version.' }
New-MatrixFixture $baselineDirectory

$cases = @(
    @{ Name = 'valid-four-configurations'; ExpectSuccess = $true; Change = $null },
    @{ Name = 'missing-matrix-artifact'; ExpectSuccess = $false; Change = {
        param($c) Move-Item -LiteralPath (Join-Path $c.ArtifactsDirectory 'integrity-windows-latest-Debug') -Destination (Join-Path $c.Directory 'removed-artifact')
    } },
    @{ Name = 'failed-package-validation'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.Passed = $false; $v }
    } },
    @{ Name = 'cancelled-test-run'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.ResultSummary.SetAttribute('outcome', 'Aborted') }
    } },
    @{ Name = 'skipped-test'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.ResultSummary.Counters.SetAttribute('notExecuted', '1'); $d.TestRun.Results.UnitTestResult[1].SetAttribute('outcome', 'NotExecuted') }
    } },
    @{ Name = 'failed-test'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.ResultSummary.Counters.SetAttribute('failed', '1'); $d.TestRun.Results.UnitTestResult[1].SetAttribute('outcome', 'Failed') }
    } },
    @{ Name = 'contradictory-trx-counter'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.ResultSummary.Counters.SetAttribute('passed', [string]($minimumTestCount - 1)) }
    } },
    @{ Name = 'narrowed-test-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($name in @('total', 'executed', 'passed')) {
                $d.TestRun.ResultSummary.Counters.SetAttribute($name, [string]($minimumTestCount - 1))
            }
            while ($d.TestRun.Results.UnitTestResult.Count -ge $minimumTestCount) {
                $result = $d.TestRun.Results.UnitTestResult | Where-Object { $_.GetAttribute('testName') -cne $apiTestName } | Select-Object -First 1
                [void] $d.TestRun.Results.RemoveChild($result)
            }
        }
    } },
    @{ Name = 'trx-error-with-all-passed'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.ResultSummary.Counters.SetAttribute('error', '1') }
    } },
    @{ Name = 'missing-api-snapshot-test'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            $api = @($d.TestRun.Results.UnitTestResult | Where-Object { $_.GetAttribute('testName') -ceq $apiTestName })
            if ($api.Count -ne 1) { throw 'Fixture needs exactly one public API snapshot test.' }
            $api[0].SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OtherCase')
        }
    } },
    @{ Name = 'missing-crc32c-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc32CTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCalculation')
                }
            }
        }
    } },
    @{ Name = 'missing-crc32c-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc32CStreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedStream')
                }
            }
        }
    } },
    @{ Name = 'missing-crc64-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc64TestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCrc64')
                }
            }
        }
    } },
    @{ Name = 'missing-crc16-variants-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc16VariantTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCrc16Variants')
                }
            }
        }
    } },
    @{ Name = 'missing-adler32-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($adler32TestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedAdler32Calculation')
                }
            }
        }
    } },
    @{ Name = 'missing-adler32-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($adler32StreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedAdler32Stream')
                }
            }
        }
    } },
    @{ Name = 'missing-crc64-variants-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc64VariantTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCrc64Variants')
                }
            }
        }
    } },
    @{ Name = 'missing-internet-checksum-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($internetChecksumTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedInternetChecksumCalculation')
                }
            }
        }
    } },
    @{ Name = 'missing-internet-checksum-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($internetChecksumStreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedInternetChecksumStream')
                }
            }
        }
    } },
    @{ Name = 'missing-crc8-variants-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc8VariantTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCrc8VariantsCalculation')
                }
            }
        }
    } },
    @{ Name = 'missing-crc8-variants-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($crc8VariantStreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedCrc8VariantsStream')
                }
            }
        }
    } },
    @{ Name = 'missing-remaining-crc-variants-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($remainingCrcVariantsTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedRemainingCrcVariantsCalculation')
                }
            }
        }
    } },
    @{ Name = 'missing-remaining-crc-variants-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($remainingCrcVariantsStreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedRemainingCrcVariantsStream')
                }
            }
        }
    } },
    @{ Name = 'missing-xxhash-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($xxHashTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedXxHashCalculation')
                }
            }
        }
    } },
    @{ Name = 'missing-xxhash-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($xxHashStreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedXxHashStream')
                }
            }
        }
    } },
    @{ Name = 'missing-fletcher4-calculation-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($fletcher4TestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedFletcher4Calculation')
                }
            }
        }
    } },
    @{ Name = 'missing-fletcher4-stream-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($fletcher4StreamTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedFletcher4Stream')
                }
            }
        }
    } },
    @{ Name = 'missing-ospf-lsa-suite'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c {
            param($d)
            foreach ($result in $d.TestRun.Results.UnitTestResult) {
                if ($result.GetAttribute('testName').StartsWith($ospfLsaTestPrefix, [StringComparison]::Ordinal)) {
                    $result.SetAttribute('testName', 'Mythosia.Test.Integrity.SyntheticGateFixture.OmittedOspfLsa')
                }
            }
        }
    } },
    @{ Name = 'foreign-test-namespace'; ExpectSuccess = $false; Change = {
        param($c) Edit-Trx $c { param($d) $d.TestRun.Results.UnitTestResult[1].SetAttribute('testName', 'Mythosia.Test.Other.Tests.Passes') }
    } },
    @{ Name = 'foreign-trx-namespace'; ExpectSuccess = $false; Change = {
        param($c) $p = Join-Path $c.ArtifactsDirectory "$selectedArtifact/integrity.trx"; [IO.File]::WriteAllText($p, ([IO.File]::ReadAllText($p).Replace('http://microsoft.com/schemas/VisualStudio/TeamTest/2010', 'urn:foreign:test')), $utf8)
    } },
    @{ Name = 'missing-trx'; ExpectSuccess = $false; Change = {
        param($c) Remove-Item -LiteralPath (Join-Path $c.ArtifactsDirectory "$selectedArtifact/integrity.trx")
    } },
    @{ Name = 'string-passed-report'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.Passed = 'true'; $v }
    } },
    @{ Name = 'wrong-sdk'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.SdkVersion = '9.0.100'; $v }
    } },
    @{ Name = 'stale-configuration'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.Configuration = 'Debug'; $v }
    } },
    @{ Name = 'wrong-configuration-case'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.Configuration = 'release'; $v }
    } },
    @{ Name = 'array-configuration'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.Configuration = @('Release', 'Release'); $v }
    } },
    @{ Name = 'stale-repository-commit'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.RepositoryCommit = 'b' * 40; $v }
    } },
    @{ Name = 'array-repository-commit'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.RepositoryCommit = @($expectedCommit, $expectedCommit); $v }
    } },
    @{ Name = 'wrong-package-version'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.PackageVersion = '999.0.0'; $v }
    } },
    @{ Name = 'corrupted-package-bytes'; ExpectSuccess = $false; Change = {
        param($c) $p = Get-CandidatePath $c; $s = [IO.File]::Open($p, [IO.FileMode]::Append); try { $s.WriteByte(0) } finally { $s.Dispose() }
    } },
    @{ Name = 'malformed-package-digest'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.PackageSHA256 = 'abc'; $v }
    } },
    @{ Name = 'missing-readme-example-count'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.ReadmeExamples--; $v }
    } },
    @{ Name = 'wrong-api-entry-count'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.ApiEntries--; $v }
    } },
    @{ Name = 'incomplete-consumer-count'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.CompletedConsumers--; $v }
    } },
    @{ Name = 'string-consumer-count'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'package-verification.json' { param($v) $v.CompletedConsumers = [string]$v.CompletedConsumers; $v }
    } },
    @{ Name = 'missing-example-consumer'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) ,@($v | Select-Object -Skip 1) }
    } },
    @{ Name = 'extra-consumer'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) $extra = $v[0] | ConvertTo-Json -Depth 20 | ConvertFrom-Json; $extra.Id = 'unexpected'; ,(@($v) + $extra) }
    } },
    @{ Name = 'public-api-consumer-error'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) ($v | Where-Object Id -CEQ 'public-api').Passed = $false; ,@($v) }
    } },
    @{ Name = 'string-passed-consumer'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) $v[0].Passed = 'true'; ,@($v) }
    } },
    @{ Name = 'consumer-wrong-configuration'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) $v[0].Configuration = 'Debug'; ,@($v) }
    } },
    @{ Name = 'consumer-wrong-example-hash'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'consumer-results.json' { param($v) $v[0].CodeSHA256 = 'd' * 64; ,@($v) }
    } },
    @{ Name = 'api-evidence-mismatch'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'api-assembly-evidence.json' { param($v) $v.ApiMatches = $false; $v }
    } },
    @{ Name = 'api-evidence-string-entry-count'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'api-assembly-evidence.json' { param($v) $v.EntryCount = [string]$v.EntryCount; $v }
    } },
    @{ Name = 'api-evidence-version-error'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'api-assembly-evidence.json' { param($v) $v.VersionMatches = $false; $v }
    } },
    @{ Name = 'api-evidence-wrong-assembly'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'api-assembly-evidence.json' { param($v) $v.AssemblySHA256 = 'd' * 64; $v }
    } },
    @{ Name = 'metadata-wrong-version'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'metadata-summary.json' { param($v) $v.version = '999.0.0'; $v }
    } },
    @{ Name = 'metadata-wrong-commit'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'metadata-summary.json' { param($v) $v.repositoryCommit = 'b' * 40; $v }
    } },
    @{ Name = 'metadata-wrong-hash'; ExpectSuccess = $false; Change = {
        param($c) Edit-Json $c 'metadata-summary.json' { param($v) $v.packageSha256 = 'd' * 64; $v }
    } },
    @{ Name = 'archive-wrong-version-with-consistent-hash'; ExpectSuccess = $false; Change = {
        param($c) Edit-ArchiveText $c 'Mythosia.Integrity.nuspec' { param($t) [regex]::Replace($t, '<version>[^<]+</version>', '<version>999.0.0</version>') }
    } },
    @{ Name = 'archive-wrong-commit-with-consistent-hash'; ExpectSuccess = $false; Change = {
        param($c) Edit-ArchiveText $c 'Mythosia.Integrity.nuspec' { param($t) [regex]::Replace($t, 'commit="[a-fA-F0-9]{40}"', ('commit="' + ('b' * 40) + '"')) }
    } },
    @{ Name = 'duplicate-package-report'; ExpectSuccess = $false; Change = {
        param($c) $extra = Join-Path $c.ArtifactsDirectory "$selectedArtifact/package-validation/run-00000000000000000000000000000002"; [void] [IO.Directory]::CreateDirectory($extra); Copy-Item -LiteralPath (Join-Path (Get-RunPath $c) 'package-verification.json') -Destination $extra
    } },
    @{ Name = 'duplicate-candidate-package'; ExpectSuccess = $false; Change = {
        param($c) $p = Get-CandidatePath $c; Copy-Item -LiteralPath $p -Destination (Join-Path (Split-Path -Parent $p) 'duplicate.nupkg')
    } },
    @{ Name = 'malformed-expected-commit'; ExpectSuccess = $false; Change = {
        param($c) $c.ExpectedRepositoryCommit = 'abc123'
    } },
    @{ Name = 'malformed-expected-version'; ExpectSuccess = $false; Change = {
        param($c) $c.ExpectedVersion = 'invalid-version'
    } }
)

$results = [Collections.Generic.List[object]]::new()
foreach ($case in $cases) {
    try {
        $detail = Invoke-GateCase $case
        $results.Add([pscustomobject]@{ Name = $case.Name; Passed = $true; Detail = $detail })
        Write-Output "PASS $($case.Name)"
    }
    catch {
        $results.Add([pscustomobject]@{ Name = $case.Name; Passed = $false; Detail = $_.Exception.Message })
        Write-Output "FAIL $($case.Name): $($_.Exception.Message)"
    }
}
$failed = @($results | Where-Object { -not $_.Passed })
$summaryPath = Join-Path $workDirectory 'test-results.json'
Write-Json $summaryPath @{
    Passed = ($failed.Count -eq 0); Cases = $results.Count; Failures = $failed.Count
    FixtureKind = $(if ([string]::IsNullOrWhiteSpace($FixtureDirectory)) { 'synthetic' } else { 'copied-local-candidate' })
    SimulatedMatrix = $true; Results = @($results.ToArray())
}
Write-Output "Release gate regression results: $summaryPath"
if ($failed.Count -ne 0) { throw "$($failed.Count) release gate regression case(s) failed." }
Write-Output "Release gate regression passed: $($results.Count) cases; no package was published."
