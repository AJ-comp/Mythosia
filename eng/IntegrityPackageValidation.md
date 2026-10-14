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

## CRC16 variants, streaming CRC, CRC32C and CRC64 coverage

The README has eight independent consumers: CRC, checksum, common API, CRC16
variants, CRC32C, CRC64, streaming CRC and chunked CRC. Their code hashes and
expected outputs are fixed in the reviewed manifest.
The CRC16 example checks `CCITTFalse` (`29B1`) and `X25` (`906E`) using numeric,
frame, accumulator and synchronous/asynchronous stream APIs. The CRC32C and CRC64 examples
freeze the `123456789` check values `E3069283` and `AE8B14860A799888`, respectively,
validate and decode a frame, check incremental state/reset, and execute both
stream methods. Their numerical output is identical on either byte order.

The reviewed API snapshot retains all 85 declarations from version 1.2.1,
including their exact attributes and defaults. Version 1.3.0 adds 71 declarations
for streaming CRC, CRC32C, CRC64 and the two CRC16 variants. The complete snapshot
has 156 entries and 25 public types. Keep existing declarations unchanged when
reviewing future additions; the snapshot
test and package consumer still require an exact match with the complete reviewed
surface.

`CrcAccumulatorTests` contributes 192 cases and `CrcStreamTests` contributes 262.
`Crc32CTests` adds 153
calculation/frame/accumulator cases and `Crc32CStreamTests` adds 74 stream cases.
`Crc64Tests` adds 90 focused cases for calculation, frames, accumulators and streams.
`Crc16VariantTests` adds 166 focused cases. The shared algorithm, accumulator and
stream suites also cover both added CRC16 variants.
The TRX completeness check requires at least 2,179 passing tests, including the
public API guard, both CRC32C suites, the CRC64 suite and the CRC16 variant suite.
Coverage includes all supported algorithms and aliases,
independent bitwise and frozen reference values, arbitrary partitions, 64 KiB
boundaries, generated large input without whole-input allocation, buffer lifetime,
single-pass non-seekable streams, cancellation and read failures.

CRC32C's oracle uses normal polynomial `1EDC6F41`, a left-shifting register and
explicit reflection, independently of the product's reflected lookup table.
Its 37 distinct non-empty published answers include RFC 7143 Appendix A.4,
CRC RevEng, Google's binary pattern and 30 Go Castagnoli golden answers.
These calibrate the oracle and numeric/digest paths, every two-chunk split and
both stream methods. A corpus guard requires at least 30 distinct inputs.
Every one- and two-byte input, all partitions of
small inputs, prefix snapshots, native digest packing, residue, frame errors,
single-pass enumerables and generated 8 MiB streams are covered. Tests retain
source links alongside the fixed vectors. The release gate regression also
rejects reports that omit a required CRC16 variant, CRC32C or CRC64 suite even if
totals still pass.

CRC16 CCITT-FALSE and X25 correctness uses 80 published fixed answers: 38
CCITT-FALSE and 42 X25 distinct non-empty inputs. They retain the seven AUTOSAR
CRC Routines vectors, four ITU-T X.25 frames, three ITSO TS 1000-2 CRC_B examples
and two catalogue checks, and add 64 fixed answers from pinned upstream tests.
These include library, RTOS, networking and EMV QR fixtures, 1-to-554-byte inputs,
ASCII, binary, repeated data, UTF-8 and CRC values with leading zeros. A corpus
guard rejects duplicates and requires at least 30 distinct inputs per algorithm.
The existing normal-polynomial bitwise
oracle is calibrated against those answers. Each vector is checked through the
numeric/digest, frame, every split and sync/async stream paths. Protocol X25
residue uses explicit low-byte-first FCS independently of native-endian library
frames. Existing exhaustive short-input, partition, buffer and stream lifecycle
tests also run for both added variants, without a separate test harness.

CRC64 correctness uses 58 distinct non-empty published numeric answers. The
original three AWS `aws-checksums` vectors are preserved, with additional fixed
AWS SDK, Rust, SPDK and StorScale fixtures from pinned revisions. The StorScale
records use its documented input pattern and frozen fourth-column NVME values,
originally produced by the independent Go `hash/crc64` implementation. No
external generator or network lookup is used in CI. A corpus guard requires at
least 30 distinct inputs. A separate MSB-first bitwise oracle uses normal
polynomial `AD93D23594C93659`, independently of the product's reflected lookup
table. The fixed answers check numeric/native-digest results, chunk splits and
both stream methods. Larger vectors use selected boundary splits and bounded
short reads rather than enumerating every partition. The suite also checks exhaustive short inputs,
representative lengths and chunk boundaries, incremental state, eight-byte frame
packing, single-pass inputs, and the sync/async stream wrappers. It reuses the
existing CI and package validation without adding a separate test harness.

Version 1.3.0 includes these algorithms and streaming APIs. Candidate packages
remain validation outputs; this validation step does not publish them.

The optional publish job selects the exact validated Ubuntu Release candidate
only after all four CI jobs and their artifact evidence pass. Ordinary CI keeps
publication disabled. See [Integrity releases](IntegrityRelease.md) for the
publication gates, authentication setup and non-publishing regression tests.
