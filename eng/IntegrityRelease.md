# Integrity NuGet release

The `publish` job in `.github/workflows/integrity.yml` is the release path for
`Mythosia.Integrity`. Ordinary pushes, pull requests and manual runs with the
default inputs only validate; they never publish. Adding this workflow does not
publish a package or change its version.

## Gates before publishing

1. Explicitly request publication with the Boolean `publish_package` input and
   provide `release_version`, exactly matching the project version.
2. Run from `master`. The same workflow run must complete all four Integrity jobs:
   Ubuntu/Windows, each in Debug/Release. A failed, cancelled or skipped job
   prevents the publish job from running.
3. Download the four result artifacts from that workflow run. Reject missing or
   duplicate reports, unsuccessful or incomplete TRX results, incorrect SDK,
   commit, version, configuration, API or README consumer evidence.
4. Check each candidate's SHA256 and package contents against its reports.
   Revalidate the Ubuntu Release candidate against the checked-out source.
5. Require the tested commit to still be the current `master` commit, then
   authenticate to NuGet. Check the branch and package hash again immediately
   before posting.
6. Publish that exact Ubuntu Release `.nupkg`. The publish job does not repack,
   build a replacement or use a previously published package.

`Select-IntegrityReleasePackage.ps1` writes its successful selection report and
GitHub outputs only after all four evidence sets pass. A failed gate produces no
package selection. Tests use simulated artifacts to verify successful selection
and rejection paths without contacting NuGet or requesting credentials.

CI reruns use fixed artifact names with `overwrite: true`. A previously successful
matrix job can retain its artifact when only failed jobs are rerun; its evidence
must still match the exact same commit and version. Each newly executed job
replaces its own artifact. Publication never reads artifacts from another workflow
run. Ordinary CI runs can cancel superseded CI, while explicitly requested
publication runs are queued separately and are not cancelled by ordinary pushes.
Publish jobs are serialized; a changed `master` still blocks an older candidate.

## One-time authentication setup

Use [NuGet Trusted Publishing](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing#github-actions-setup).
The workflow requests a short-lived key only after validation, instead of storing
a long-lived NuGet API key in the repository.

- Set the GitHub repository or `nuget` environment variable `NUGET_USER` to the
  NuGet profile username that owns the package (currently `JJW`), not an email.
- In that owner's NuGet account, register a GitHub Trusted Publishing policy with
  repository owner `AJ-comp`, repository `Mythosia`, workflow file `integrity.yml`
  and environment `nuget`. Scope it to `Mythosia.Integrity`.
- Use the GitHub environment `nuget` for this publish job. If configuring
  deployment branch rules, allow `master` only.

A missing username, unconfigured trust policy or failed authentication stops the
job before `dotnet nuget push`. Creating a trust policy and obtaining publication
credentials are separate from offline workflow verification; they are required
before the first actual release.

## Request a future release

Prepare and commit the intended version, package description/release notes,
README availability text and dated release notes. Run the `Integrity CI` workflow
on `master`, select `publish_package: true`, and enter that version in
`release_version`. Keep `publish_package: false` for validation-only runs.

The gates run again on the selected commit; passing an older CI run is insufficient.
A duplicate NuGet version causes a failed push. The workflow does not use
`--skip-duplicate` to treat it as a successful release.

This gate controls this workflow's publication path. NuGet account owners retain
their independent manual upload rights; use this workflow for project releases.

## Verify without publishing

From PowerShell 7.2 or newer:

```powershell
./eng/Test-IntegrityReleaseGate.ps1
```

The default test data is a synthetic package and simulated CI reports. To also
exercise the selector with an existing real candidate report directory and TRX:

```powershell
./eng/Test-IntegrityReleaseGate.ps1 -FixtureDirectory /path/to/package-validation/run-id -TestResultsPath /path/to/integrity.trx
```

Neither command invokes NuGet authentication or publication. The regression
script runs in each CI matrix job before the existing Integrity checks.
