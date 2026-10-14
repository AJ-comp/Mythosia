[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string] $Path
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# A floor catches an accidentally narrowed filter without blocking new tests.
$minimumTestCount = 1616
$testNamePrefix = 'Mythosia.Test.Integrity.'
$legacyTestNamePrefix = 'Mythosia.Test.IntegrityTest.'
$apiTestName = 'Mythosia.Test.Integrity.IntegrityPublicApiTests.PublicApi_MatchesApprovedSnapshot'

if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
    throw "Integrity test results are missing: $Path"
}

[xml] $report = Get-Content -LiteralPath $Path -Raw
$namespaces = [System.Xml.XmlNamespaceManager]::new($report.NameTable)
$namespaces.AddNamespace('t', 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010')
$summary = $report.SelectSingleNode('/t:TestRun/t:ResultSummary', $namespaces)
$counters = $report.SelectSingleNode('/t:TestRun/t:ResultSummary/t:Counters', $namespaces)
if ($null -eq $summary -or $null -eq $counters) {
    throw 'Integrity TRX must contain a test summary and counters.'
}

function Read-TestCounter([string] $Name) {
    $value = 0
    if (-not [int]::TryParse($counters.GetAttribute($Name), [ref] $value) -or $value -lt 0) {
        throw "Integrity TRX has an invalid '$Name' counter."
    }
    return $value
}

$total = Read-TestCounter 'total'
$executed = Read-TestCounter 'executed'
$passed = Read-TestCounter 'passed'
$failed = Read-TestCounter 'failed'
$notExecuted = Read-TestCounter 'notExecuted'
if ($summary.GetAttribute('outcome') -ne 'Completed' -or
    $total -lt $minimumTestCount -or $executed -ne $total -or $passed -ne $total -or
    $failed -ne 0 -or $notExecuted -ne 0) {
    throw "Integrity tests did not complete: outcome=$($summary.GetAttribute('outcome')), total=$total (minimum=$minimumTestCount), executed=$executed, passed=$passed, failed=$failed, notExecuted=$notExecuted."
}

# VSTest has additional outcomes besides Failed and NotExecuted. Reject even a
# contradictory report that claims all tests passed but records one of these.
$otherOutcomeCounters = @(
    'error', 'timeout', 'aborted', 'inconclusive', 'passedButRunAborted',
    'notRunnable', 'disconnected', 'warning', 'completed', 'inProgress', 'pending'
)
foreach ($counterName in $otherOutcomeCounters) {
    if ((Read-TestCounter $counterName) -ne 0) {
        throw "Integrity TRX contains an unsuccessful '$counterName' outcome."
    }
}

$results = $report.SelectNodes('/t:TestRun/t:Results/t:UnitTestResult', $namespaces)
if ($results.Count -ne $total) {
    throw "Integrity TRX contains $($results.Count) results, but reports $total tests."
}

$apiTestExecuted = $false
foreach ($result in $results) {
    $testName = $result.GetAttribute('testName')
    $isIntegrityTest = $testName.StartsWith($testNamePrefix, [StringComparison]::Ordinal) -or
        $testName.StartsWith($legacyTestNamePrefix, [StringComparison]::Ordinal)
    if ($result.GetAttribute('outcome') -ne 'Passed' -or
        -not $isIntegrityTest) {
        throw "Unexpected Integrity test result: $testName ($($result.GetAttribute('outcome')))."
    }
    if ([string]::Equals($testName, $apiTestName, [StringComparison]::Ordinal)) {
        $apiTestExecuted = $true
    }
}
if (-not $apiTestExecuted) {
    throw 'The Integrity public API snapshot test did not execute.'
}

Write-Output "Integrity results verified: $passed passed, 0 failed, 0 skipped; public API snapshot executed."
