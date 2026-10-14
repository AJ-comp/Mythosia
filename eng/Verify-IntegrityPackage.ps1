#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $OutputDirectory,
    [ValidateSet('Debug', 'Release')]
    [string] $Configuration = 'Release',
    [string] $RepositoryRoot = (Split-Path $PSScriptRoot -Parent),
    [string] $DotnetPath = 'dotnet',
    [string] $ExpectedRepositoryCommit
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-JsonFile([string] $Path, $Value) {
    $Value | ConvertTo-Json -Depth 12 | Set-Content -LiteralPath $Path -Encoding utf8
}

function Normalize-Newlines([string] $Text) {
    return $Text.Replace("`r`n", "`n").Replace("`r", "`n")
}

function Get-CodeHash([string] $Code) {
    $bytes = [Text.Encoding]::UTF8.GetBytes((Normalize-Newlines $Code))
    return [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData($bytes))
}

function Test-SamePath([string] $Left, [string] $Right) {
    $comparison = if ($IsWindows) { [StringComparison]::OrdinalIgnoreCase } else { [StringComparison]::Ordinal }
    return [string]::Equals([IO.Path]::GetFullPath($Left).TrimEnd('\', '/'),
        [IO.Path]::GetFullPath($Right).TrimEnd('\', '/'), $comparison)
}

function Invoke-Dotnet([string[]] $CommandArguments, [string] $Directory,
    [string] $LogName, [int] $TimeoutSeconds = 180) {
    $startInfo = [Diagnostics.ProcessStartInfo]::new()
    $startInfo.FileName = $dotnetExecutable
    $startInfo.WorkingDirectory = $Directory
    $startInfo.UseShellExecute = $false
    $startInfo.RedirectStandardOutput = $true
    $startInfo.RedirectStandardError = $true
    $startInfo.Environment['DOTNET_CLI_UI_LANGUAGE'] = 'en'
    $startInfo.Environment['DOTNET_CLI_TELEMETRY_OPTOUT'] = '1'
    foreach ($entry in $invocationEnvironment.GetEnumerator()) {
        $startInfo.Environment[$entry.Key] = $entry.Value
    }
    foreach ($argument in $CommandArguments) { $startInfo.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::new()
    $process.StartInfo = $startInfo
    $stdoutPath = Join-Path $logsDirectory "$LogName.stdout.log"
    $stderrPath = Join-Path $logsDirectory "$LogName.stderr.log"
    [IO.File]::WriteAllText($stdoutPath, '')
    [IO.File]::WriteAllText($stderrPath, '')
    try {
        if (-not $process.Start()) { throw 'Could not start dotnet.' }
        $stdoutTask = $process.StandardOutput.ReadToEndAsync()
        $stderrTask = $process.StandardError.ReadToEndAsync()
        $completed = $process.WaitForExit($TimeoutSeconds * 1000)
        if (-not $completed) {
            if (-not $process.HasExited) { $process.Kill($true) }
            [void]$process.WaitForExit(5000)
        }
        # A child retaining a redirected pipe must not make stream capture wait
        # forever after the parent process exits or times out.
        $captured = $false
        try { $captured = [Threading.Tasks.Task]::WaitAll([Threading.Tasks.Task[]]@($stdoutTask, $stderrTask), 5000) }
        catch { $captured = $false }
        $stdout = if ($stdoutTask.IsCompletedSuccessfully) { $stdoutTask.Result } else { 'stdout capture did not complete.' }
        $stderr = if ($stderrTask.IsCompletedSuccessfully) { $stderrTask.Result } else { 'stderr capture did not complete.' }
        [IO.File]::WriteAllText($stdoutPath, $stdout)
        [IO.File]::WriteAllText($stderrPath, $stderr)
        if (-not $completed) { throw "dotnet $LogName timed out after ${TimeoutSeconds}s; see $stdoutPath and $stderrPath." }
        if (-not $captured) { throw "dotnet $LogName stream capture did not complete; see $stdoutPath and $stderrPath." }
        if ($process.ExitCode -ne 0) { throw "dotnet $LogName exited with $($process.ExitCode); see $stdoutPath and $stderrPath." }
        return [pscustomobject]@{ Stdout = $stdout; Stderr = $stderr; StdoutLog = $stdoutPath; StderrLog = $stderrPath }
    }
    catch {
        [IO.File]::AppendAllText($stderrPath, "`n" + $_.Exception.Message + "`n")
        throw
    }
    finally { $process.Dispose() }
}

function Read-ExampleCases {
    $readme = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'Mythosia.Integrity/README.md'))
    $blocks = [regex]::Matches($readme, '(?ms)^```csharp[ \t]*\r?\n(?<Code>.*?)^```[ \t]*\r?$')
    $manifest = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'IntegrityReadme.expected.json') -Raw | ConvertFrom-Json
    if (($manifest.schemaVersion -isnot [int] -and $manifest.schemaVersion -isnot [long]) -or
        $manifest.schemaVersion -ne 1 -or $manifest.cases -isnot [array] -or $blocks.Count -eq 0 -or $blocks.Count -ne $manifest.cases.Count) {
        throw 'Every README csharp block must have one reviewed expected-output manifest entry.'
    }
    $ids = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    for ($index = 0; $index -lt $blocks.Count; $index++) {
        $case = $manifest.cases[$index]
        if ($case.id -isnot [string] -or $case.id -cnotmatch '^[a-z][a-z0-9-]*$' -or -not $ids.Add($case.id) -or
            $case.id -cin @('public-api', 'candidate-feed', 'packages', 'http-cache', 'plugin-cache')) { throw 'README case IDs must be unique safe names outside the reserved working directories.' }
        $code = Normalize-Newlines $blocks[$index].Groups['Code'].Value
        if ($case.codeSha256 -isnot [string] -or $case.codeSha256 -notmatch '^[A-Fa-f0-9]{64}$' -or (Get-CodeHash $code) -ine $case.codeSha256) {
            throw "README example '$($case.id)' changed. Review the code and frozen expected output before updating its manifest hash."
        }
        foreach ($outputName in @('stdoutLittleEndian', 'stdoutBigEndian')) {
            $outputLines = $case.PSObject.Properties[$outputName].Value
            $lines = @($outputLines)
            if ($outputLines -isnot [array] -or $lines.Count -eq 0 -or @($lines | Where-Object {
                $_ -isnot [string] -or [string]::IsNullOrWhiteSpace($_) -or $_.Contains("`r") -or $_.Contains("`n")
            }).Count -ne 0) {
                throw "README example '$($case.id)' needs non-empty frozen output lines for both byte orders."
            }
        }
        $expected = if ([BitConverter]::IsLittleEndian) { @($case.stdoutLittleEndian) } else { @($case.stdoutBigEndian) }
        [pscustomobject]@{ Id = $case.id; Code = $code; CodeSHA256 = $case.codeSha256; ExpectedOutput = $expected; IsApi = $false }
    }
}

function Write-ApiFormatter([string] $CaseDirectory) {
    $source = [IO.File]::ReadAllText((Join-Path $RepositoryRoot 'Mythosia.Test/Integrity/IntegrityPublicApiTests.cs'))
    $marker = 'internal static class IntegrityPublicApiSnapshot'
    $start = $source.IndexOf($marker, [StringComparison]::Ordinal)
    if ($start -lt 0 -or $source.IndexOf($marker, $start + $marker.Length, [StringComparison]::Ordinal) -ge 0) {
        throw 'Expected exactly one existing Integrity public API formatter.'
    }
    # Reuse the existing formatter without compiling the xUnit test class or
    # introducing a second independently maintained snapshot implementation.
    $imports = @'
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.Json;
namespace Mythosia.Test.Integrity;
'@
    [IO.File]::WriteAllText((Join-Path $CaseDirectory 'IntegrityPublicApiSnapshot.cs'), $imports + "`n" + $source.Substring($start))
}

function New-ConsumerProject($Case, [string] $CaseDirectory) {
    $apiItems = ''
    if ($Case.IsApi) {
        Write-ApiFormatter $CaseDirectory
        $apiItems = '<Compile Include="IntegrityPublicApiSnapshot.cs" />'
    }
    [IO.File]::WriteAllText((Join-Path $CaseDirectory 'Program.cs'), $Case.Code)
    $escapedId = [Security.SecurityElement]::Escape($packageId)
    $escapedVersion = [Security.SecurityElement]::Escape($packageVersion)
    $project = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <AssemblyName>IntegrityPackageConsumer</AssemblyName>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>disable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
    <RestoreFallbackFolders></RestoreFallbackFolders>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="Program.cs" />
    $apiItems
    <PackageReference Include="$escapedId" Version="[$escapedVersion]" />
  </ItemGroup>
</Project>
"@
    $projectPath = Join-Path $CaseDirectory 'Consumer.csproj'
    [IO.File]::WriteAllText($projectPath, $project)
    return $projectPath
}

function Read-ConsumedPackages([string] $CaseDirectory) {
    $assets = Get-Content -LiteralPath (Join-Path $CaseDirectory 'obj/project.assets.json') -Raw | ConvertFrom-Json
    $folders = @($assets.packageFolders.PSObject.Properties.Name)
    if ($folders.Count -ne 1 -or -not (Test-SamePath $folders[0] $packagesDirectory)) { throw 'Consumer used an unexpected package cache.' }
    if (@($assets.libraries.PSObject.Properties | Where-Object { $_.Value.type -ne 'package' }).Count -ne 0) { throw 'Consumer must use packages, never project references.' }
    $targets = @($assets.targets.PSObject.Properties)
    if ($targets.Count -ne 1) { throw 'Expected one consumer target framework.' }
    $target = $targets[0].Value
    foreach ($specification in @(
        @{ Id = $packageId; Version = $packageVersion; Source = $feedDirectory },
        @{ Id = $dependencyId; Version = $dependencyVersion; Source = $officialSource }
    )) {
        $key = "$($specification.Id)/$($specification.Version)"
        $libraryProperty = $assets.libraries.PSObject.Properties[$key]
        if ($null -eq $libraryProperty) { throw "Required package $key was not restored." }
        $packageRoot = Join-Path $packagesDirectory $libraryProperty.Value.path
        $metadata = Get-Content -LiteralPath (Join-Path $packageRoot '.nupkg.metadata') -Raw | ConvertFrom-Json
        $correctSource = if ($specification.Id -ceq $packageId) { Test-SamePath $metadata.source $feedDirectory }
            else { [string]::Equals($metadata.source.TrimEnd('/'), $officialSource, [StringComparison]::OrdinalIgnoreCase) }
        if (-not $correctSource) { throw "$key came from an unexpected source: $($metadata.source)." }
        $archive = Join-Path $packageRoot ($specification.Id.ToLowerInvariant() + '.' + $specification.Version + '.nupkg')
        $archiveHash = (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash
        if ($specification.Id -ceq $packageId -and $archiveHash -cne $packageHash) { throw 'Restored Integrity package differs from the CI candidate.' }
        $runtime = @($target.PSObject.Properties[$key].Value.runtime.PSObject.Properties.Name | Where-Object { $_.EndsWith('/' + $specification.Id + '.dll', [StringComparison]::Ordinal) })
        if ($runtime.Count -ne 1) { throw "Expected one runtime DLL for $key." }
        $runtimePath = Join-Path $packageRoot $runtime[0]
        [pscustomobject]@{
            Id = $specification.Id; Version = $specification.Version; Source = $metadata.source
            ArchiveSHA256 = $archiveHash; RuntimeAssemblySHA256 = (Get-FileHash -LiteralPath $runtimePath -Algorithm SHA256).Hash
        }
    }
}

function Assert-ExpectedOutput([string] $Actual, [string[]] $Expected, [string] $CaseId) {
    $normalized = Normalize-Newlines $Actual
    if ($normalized.EndsWith("`n", [StringComparison]::Ordinal)) { $normalized = $normalized.Substring(0, $normalized.Length - 1) }
    if (-not [string]::Equals($normalized, ($Expected -join "`n"), [StringComparison]::Ordinal)) {
        throw "Consumer '$CaseId' did not produce the reviewed expected output."
    }
}

function Test-Consumer($Case) {
    $caseDirectory = Join-Path $workDirectory $Case.Id
    [void](New-Item -ItemType Directory -Path $caseDirectory)
    $projectPath = New-ConsumerProject $Case $caseDirectory
    $null = Invoke-Dotnet @('restore', $projectPath, '--configfile', $configPath, '--packages', $packagesDirectory, '--no-cache', '--verbosity', 'minimal') $workDirectory "$($Case.Id)-restore"
    $consumed = @(Read-ConsumedPackages $caseDirectory)
    $build = Invoke-Dotnet @('build', $projectPath, '--configuration', $Configuration, '--no-restore', '--verbosity', 'minimal') $workDirectory "$($Case.Id)-build"
    if ($build.Stdout -notmatch '(?m)^\s*0 Warning\(s\)\s*$' -or $build.Stdout -notmatch '(?m)^\s*0 Error\(s\)\s*$') { throw "Consumer '$($Case.Id)' must build without warnings or errors." }
    $executable = Join-Path $caseDirectory "bin/$Configuration/net8.0/IntegrityPackageConsumer.dll"
    $arguments = @($executable)
    $evidencePath = Join-Path $reportDirectory 'api-assembly-evidence.json'
    if ($Case.IsApi) { $arguments += @($approvedPath, $evidencePath, $assemblyVersion, $fileVersion) }
    $execution = Invoke-Dotnet $arguments $workDirectory "$($Case.Id)-run" 30
    Assert-ExpectedOutput $execution.Stdout $Case.ExpectedOutput $Case.Id
    if ($execution.Stderr.Length -ne 0) { throw "Consumer '$($Case.Id)' wrote unexpected stderr." }
    if ($Case.IsApi) {
        $evidence = Get-Content -LiteralPath $evidencePath -Raw | ConvertFrom-Json
        if ($evidence.AssemblySHA256 -cne $consumed[0].RuntimeAssemblySHA256 -or $evidence.MythosiaAssemblySHA256 -cne $consumed[1].RuntimeAssemblySHA256) { throw 'Executed assemblies differ from the restored package DLLs.' }
    }
    return [pscustomobject]@{ Id = $Case.Id; Configuration = $Configuration; Passed = $true; CodeSHA256 = $Case.CodeSHA256; Packages = $consumed }
}

$RepositoryRoot = [IO.Path]::GetFullPath($RepositoryRoot)
$dotnetExecutable = (Get-Command $DotnetPath -CommandType Application -ErrorAction Stop | Select-Object -First 1).Source
$invocationEnvironment = @{}
$reportDirectory = Join-Path ([IO.Path]::GetFullPath($OutputDirectory)) ('run-' + [guid]::NewGuid().ToString('N'))
$logsDirectory = Join-Path $reportDirectory 'logs'
$packageDirectory = Join-Path $reportDirectory 'package'
$workDirectory = Join-Path ([IO.Path]::GetTempPath()) ('Mythosia.IntegrityPackage-' + [guid]::NewGuid().ToString('N'))
foreach ($directory in @($logsDirectory, $packageDirectory, $workDirectory)) { [void](New-Item -ItemType Directory -Path $directory -Force) }
Write-Output "Integrity package results: $reportDirectory"
$results = [Collections.Generic.List[object]]::new()
$packagePath = $null
$packageHash = $null
try {
    if ([string]::IsNullOrWhiteSpace($ExpectedRepositoryCommit)) {
        $ExpectedRepositoryCommit = (& git -C $RepositoryRoot rev-parse HEAD).Trim()
        if ($LASTEXITCODE -ne 0) { throw 'Could not determine the package repository commit.' }
    }
    if ($ExpectedRepositoryCommit -notmatch '^[a-fA-F0-9]{40}$') { throw 'ExpectedRepositoryCommit must be a full Git commit SHA.' }
    $ExpectedRepositoryCommit = $ExpectedRepositoryCommit.ToLowerInvariant()
    $sdk = (Invoke-Dotnet @('--version') $RepositoryRoot 'sdk').Stdout.Trim()
    if ($sdk -notmatch '^8\.0\.\d+$') { throw "Package validation requires the workflow-selected .NET 8 SDK; selected $sdk." }
    Write-JsonFile (Join-Path $workDirectory 'global.json') @{ sdk = @{ version = $sdk; rollForward = 'disable' } }
    $cases = @(Read-ExampleCases)
    [xml]$project = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Mythosia.Integrity/Mythosia.Integrity.csproj') -Raw
    [xml]$dependencyProject = Get-Content -LiteralPath (Join-Path $RepositoryRoot 'Mythosia/Mythosia.csproj') -Raw
    $packageId = [string]$project.Project.PropertyGroup.PackageId
    $packageVersion = [string]$project.Project.PropertyGroup.Version
    $assemblyVersion = [string]$project.Project.PropertyGroup.AssemblyVersion
    $fileVersion = [string]$project.Project.PropertyGroup.FileVersion
    $dependencyId = 'Mythosia'
    $dependencyVersion = [string]$dependencyProject.Project.PropertyGroup.Version
    if ($packageId -cne 'Mythosia.Integrity' -or [string]::IsNullOrWhiteSpace($packageVersion)) { throw 'Expected an explicit Integrity package ID/version.' }
    $approvedPath = Join-Path $RepositoryRoot 'Mythosia.Test/Integrity/PublicApi.approved.txt'
    $approved = @([IO.File]::ReadAllLines($approvedPath) | Where-Object { $_.Length -gt 0 -and -not $_.StartsWith('#', [StringComparison]::Ordinal) })
    $typeCount = @($approved | Where-Object { $_.StartsWith('type ', [StringComparison]::Ordinal) }).Count
    if ($approved.Count -eq 0 -or $typeCount -eq 0) { throw 'The approved API snapshot cannot be empty.' }
    $cases += [pscustomobject]@{
        Id = 'public-api'; IsApi = $true; CodeSHA256 = $null
        Code = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'IntegrityPackageApiConsumer.cs'))
        ExpectedOutput = @("Public API snapshot matched: $($approved.Count) entries, $typeCount types.")
    }
    $null = Invoke-Dotnet @('pack', (Join-Path $RepositoryRoot 'Mythosia.Integrity/Mythosia.Integrity.csproj'), '--configuration', $Configuration,
        '--output', $packageDirectory, '-p:ContinuousIntegrationBuild=true', "-p:RepositoryCommit=$ExpectedRepositoryCommit") $RepositoryRoot 'pack' 240
    $packages = @(Get-ChildItem -LiteralPath $packageDirectory -Filter '*.nupkg' -File)
    if ($packages.Count -ne 1) { throw 'Expected exactly one newly packed candidate.' }
    $packagePath = $packages[0].FullName
    $packageHash = (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash
    & (Join-Path $PSScriptRoot 'Verify-IntegrityPackageMetadata.ps1') -PackagePath $packagePath -RepositoryRoot $RepositoryRoot -ExpectedRepositoryCommit $ExpectedRepositoryCommit -OutputDirectory $reportDirectory
    if (-not $?) { throw 'Integrity package metadata verification failed.' }
    $officialSource = 'https://api.nuget.org/v3/index.json'
    $feedDirectory = Join-Path $workDirectory 'candidate-feed'
    $packagesDirectory = Join-Path $workDirectory 'packages'
    [void](New-Item -ItemType Directory -Path $feedDirectory)
    Copy-Item -LiteralPath $packagePath -Destination $feedDirectory
    $escapedFeed = [Security.SecurityElement]::Escape($feedDirectory)
    $configPath = Join-Path $workDirectory 'NuGet.Config'
    $config = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources><clear /><add key="candidate" value="$escapedFeed" /><add key="nuget.org" value="$officialSource" /></packageSources>
  <fallbackPackageFolders><clear /></fallbackPackageFolders>
  <packageSourceMapping>
    <packageSource key="candidate"><package pattern="Mythosia.Integrity" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
"@
    [IO.File]::WriteAllText($configPath, $config)
    $invocationEnvironment = @{
        NUGET_PACKAGES = $packagesDirectory
        NUGET_HTTP_CACHE_PATH = (Join-Path $workDirectory 'http-cache')
        NUGET_PLUGINS_CACHE_PATH = (Join-Path $workDirectory 'plugin-cache')
    }
    foreach ($case in $cases) {
        $results.Add((Test-Consumer $case))
        Write-JsonFile (Join-Path $reportDirectory 'consumer-results.json') @($results.ToArray())
    }
    if ($results.Count -ne $cases.Count -or (Get-FileHash -LiteralPath $packagePath -Algorithm SHA256).Hash -cne $packageHash) { throw 'Candidate changed or a consumer did not execute.' }
    Write-JsonFile (Join-Path $reportDirectory 'package-verification.json') @{
        Passed = $true; Configuration = $Configuration; SdkVersion = $sdk; RepositoryCommit = $ExpectedRepositoryCommit
        PackageId = $packageId; PackageVersion = $packageVersion; PackageSHA256 = $packageHash
        ReadmeExamples = $cases.Count - 1; ApiEntries = $approved.Count; ApiTypes = $typeCount
        CompletedConsumers = $results.Count; WorkDirectory = $workDirectory
    }
    Write-Output "Integrity package validation passed: $($cases.Count - 1) README examples, public API, metadata; configuration=$Configuration; package=$packageId $packageVersion."
}
catch {
    Write-JsonFile (Join-Path $reportDirectory 'package-verification.json') @{
        Passed = $false; Configuration = $Configuration; Error = $_.Exception.Message
        CompletedConsumers = $results.Count; PackagePath = $packagePath; PackageSHA256 = $packageHash; WorkDirectory = $workDirectory
    }
    throw
}
