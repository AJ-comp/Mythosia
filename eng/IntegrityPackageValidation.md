# Integrity package validation

The Integrity CI matrix keeps the existing unit tests, API guard and benchmark
validation, then runs `Verify-IntegrityPackage.ps1` on Windows and Linux in Debug
and Release. This validation creates a candidate package and does not publish it.

## Checks

- Pack the current Integrity project with the matrix configuration and Git commit.
- Compare the candidate's nuspec metadata with the current project: identity,
  version, description, release notes, tags, license, repository and dependency.
- Require the expected .NET Standard 2.1 DLL and byte-identical README and release
  notes. Reject missing files, extra library/source payloads and ambiguous ZIP paths.
- Extract every `csharp` README block and build it as an independent .NET 8 console
  app with implicit usings disabled and warnings treated as errors.
- Execute the examples and compare stdout with the reviewed, fixed expectations in
  `IntegrityReadme.expected.json`. Expected results are not computed by the library.
- Execute a separate API consumer using the existing snapshot formatter and
  `Mythosia.Test/Integrity/PublicApi.approved.txt`; check assembly and file versions.
- Check restore source, candidate archive hash and executed DLL hashes to prove the
  consumers used the package that this CI job built.

Consumers use exact `PackageReference` versions and no project references. Each
run creates fresh package, HTTP and plugin caches outside the repository. Source
mapping sends `Mythosia.Integrity` exclusively to the candidate feed and restores
dependencies from NuGet.org, even when the candidate's version already exists
on NuGet.org. The selected .NET 8 SDK is also pinned in the consumer directory.

Each native command has a timeout. A build/run error, warning, unexpected output,
missing case or package mismatch fails the step. Reports, stdout/stderr logs and
the candidate `.nupkg` are included in the existing CI result artifact, including
on failure. Temporary consumer sources and caches stay outside that artifact.

## Run locally

Use PowerShell 7.2 or newer and the same .NET 8 SDK as the workflow:

```powershell
./eng/Verify-IntegrityPackage.ps1 -Configuration Release -OutputDirectory ./artifacts/IntegrityPackageCI
```

Pass `-DotnetPath` when the .NET 8 executable is not on PATH. If the machine also
has newer SDKs, use a local `global.json` to select an installed .NET 8 SDK.
`-ExpectedRepositoryCommit` defaults to `git rev-parse HEAD`.

## Review example changes

The manifest stores a case ID, code SHA256, and little-/big-endian stdout arrays
for each README block, in document order. A changed, added or removed example
requires a reviewed manifest change. The code hash normalizes CRLF and CR to LF,
then hashes UTF-8 without a BOM, preserving all other whitespace and the final LF.
The output check accepts the console's final line ending and compares all result
lines exactly. Use independent published vectors or direct checksum arithmetic
to review expected values; do not generate them from production calculations.

Package and dependency versions come from the project files. Release notes use
the package version on their first line; the README's `Version` line and the first
`## version - yyyy-MM-dd` release heading must match it. Version bumps require
updating those descriptions, not changing fixed CRC/checksum results or the
approved public API snapshot.

## Streaming CRC and CRC32C coverage

The README has six independent consumers: CRC, checksum, common API, CRC32C,
streaming CRC and chunked CRC. The five examples that preceded CRC32C keep their
code hashes and expected outputs unchanged. The new CRC32C example freezes the
published `123456789` check value `E3069283`, validates and decodes a frame, checks
incremental state/reset, and executes both stream methods. Its numerical output
is identical on either byte order.

The reviewed API snapshot retains all 85 released declarations, the 28 streaming
additions and their exact attributes/defaults, and adds 17 CRC32C declarations.
The complete snapshot has 130 entries and 21 public types. Keep
existing declarations unchanged when reviewing future additions; the snapshot
test and package consumer still require an exact match with the complete reviewed
surface.

`CrcAccumulatorTests` contributes 164 cases and `CrcStreamTests` contributes 220.
Their 384 cases run alongside the earlier 1,232 cases. `Crc32CTests` adds 122
calculation/frame/accumulator cases and `Crc32CStreamTests` adds 74 stream cases.
The TRX completeness check requires at least 1,812 passing tests, including the
public API guard and both CRC32C suites. Coverage includes all supported algorithms and aliases,
independent bitwise and frozen reference values, arbitrary partitions, 64 KiB
boundaries, generated large input without whole-input allocation, buffer lifetime,
single-pass non-seekable streams, cancellation and read failures.

CRC32C's oracle uses normal polynomial `1EDC6F41`, a left-shifting register and
explicit reflection, independently of the product's reflected lookup table.
RFC 7143 Appendix A.4 octets and fixed CRC RevEng/Google answers calibrate that
oracle and every product path. Every one- and two-byte input, all partitions of
small inputs, prefix snapshots, native digest packing, residue, frame errors,
single-pass enumerables and generated 8 MiB streams are covered. Tests retain
source links alongside the fixed vectors. The release gate regression also
rejects reports that omit either CRC32C suite even if totals still pass.

Until these additions are released, the README and release notes identify them as
unreleased source changes. Candidate packages are validation outputs only; this
validation step does not publish them. Ordinary CI leaves publication disabled.

The optional publish job selects the exact validated Ubuntu Release candidate
only after all four CI jobs and their artifact evidence pass. Ordinary CI keeps
publication disabled. See [Integrity releases](IntegrityRelease.md) for the
publication gates, authentication setup and non-publishing regression tests.
