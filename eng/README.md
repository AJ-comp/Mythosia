# Integrity CI

`.github/workflows/integrity.yml` runs on every push, pull request and manual
dispatch. The matrix covers Windows and Ubuntu, each in Debug and Release,
with the latest .NET 8 SDK. A runner-local `global.json` selects the installed
SDK even when a newer SDK is preinstalled; it is not a repository change.

Each job builds the functional test project and runs the
`Mythosia.Test.Integrity` tests, including the approved public API snapshot,
and the four existing `Mythosia.Test.IntegrityTest` smoke tests.
`Verify-IntegrityTestResults.ps1` rejects missing or incomplete TRX reports,
failed or skipped tests, fewer than 1,812 cases, unexpected test namespaces,
a missing API snapshot test, and either missing CRC32C test suite. The CRC32C
coverage includes fixed standard vectors, an independent bitwise oracle,
exhaustive two-byte inputs, incremental partitions and sync/async streams.
New tests can increase the count without
changing the floor. Intentional removals require reviewing the floor too.

The benchmark project is also built, then `--validate-integrity` verifies
all 132 parameter combinations / 396 cases twice. Its complete success summary
is required; changes to the benchmark matrix require reviewing this expected
summary as well. CI does not run timed benchmarks or enforce machine-dependent
performance thresholds. Test reports and hang diagnostics are uploaded for
14 days, including on failed jobs.

Each job then packs the current project and validates the actual NuGet candidate,
README examples, installed public API and package metadata with
`Verify-IntegrityPackage.ps1`. Consumers use an isolated local feed and fresh
caches, so an already published package cannot substitute for the candidate.
Package reports, logs and the candidate are included in the same result artifact.
See [package validation](IntegrityPackageValidation.md) for the checks and the
review procedure for expected example output. This step does not publish packages.

The optional publish job depends on all four successful matrix jobs. Its manual
`publish_package` input defaults to `false`; ordinary CI never publishes.
The job checks all four artifacts, verifies their commit/version and package
hashes, then publishes the exact validated Ubuntu Release candidate. A failed,
cancelled, skipped or incomplete check blocks publication. See
[Integrity releases](IntegrityRelease.md) for authentication setup, release inputs
and offline regression tests for the gates.

The public API baseline is
`Mythosia.Test/Integrity/PublicApi.approved.txt`. It is an embedded test resource
and never regenerates automatically. The snapshot includes public and protected
members, parameter names and defaults, enum values, modifiers and contract
attributes. Review any intentional contract change before editing this file;
updating the baseline is not a fix for an accidental API change.

Product code, target frameworks, package versions and existing CRC/checksum
representations are not changed by this CI setup. Only Integrity tests are
selected; unrelated functional tests are outside this workflow.

To run the same checks locally from the repository root with PowerShell 7.2
or newer and a .NET 8 SDK installed:

```powershell
dotnet restore Mythosia.Test/Mythosia.Test.Function.csproj
dotnet build Mythosia.Test/Mythosia.Test.Function.csproj -c Release --no-restore
dotnet test Mythosia.Test/Mythosia.Test.Function.csproj -c Release --no-build --no-restore --filter 'FullyQualifiedName~Mythosia.Test.Integrity.|FullyQualifiedName~Mythosia.Test.IntegrityTest.' --logger 'trx;LogFileName=integrity.trx' --results-directory ./TestResults/Integrity --blame-hang --blame-hang-timeout 3m
./eng/Verify-IntegrityTestResults.ps1 -Path ./TestResults/Integrity/integrity.trx
dotnet restore Mythosia.Test.Performance/Mythosia.Test.Performance.csproj
dotnet build Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release --no-restore
dotnet run --project Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release --no-build --no-restore -- --validate-integrity
./eng/Verify-IntegrityPackage.ps1 -Configuration Release -OutputDirectory ./TestResults/Integrity/package-validation
```

Repeat with `Debug` to cover both configurations. If a local results directory
already exists, use a fresh directory to keep prior reports separate.

Actions are pinned to verified release commit hashes. Validation jobs use
read-only repository permissions, no publication credentials and ordinary
`pull_request` events. Only the explicitly requested publish job can request an
OIDC token, after its CI dependency and evidence gates pass. It becomes active
when these files are pushed to GitHub; repository branch-protection settings are
managed separately.
