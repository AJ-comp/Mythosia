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

## CRC and checksum coverage

The README has 18 independent consumers. The original 12 cover CRC, checksum,
common API, CRC16 variants, CRC32C, CRC64, streaming CRC, chunked CRC, Adler-32,
CRC64 variants, Internet Checksum and CRC8 SAE-J1850/AUTOSAR H2F. Six further
examples cover Sensirion CRC8, MCRF4XX/USB, MPEG-2/BZIP2/AUTOSAR P4, xxHash32/64,
Fletcher4 and OSPF LSA checksums. Code hashes and expected outputs are fixed in
the reviewed manifest; all original 12 blocks and expectations remain intact.
The CRC16 example checks `CCITTFalse` (`29B1`) and `X25` (`906E`) using numeric,
frame, accumulator and synchronous/asynchronous stream APIs. The CRC32C and CRC64 examples
freeze the `123456789` check values `E3069283` and `AE8B14860A799888`, respectively,
validate and decode a frame, check incremental state/reset, and execute both
stream methods. Their numerical output is identical on either byte order.

The reviewed API snapshot retains all 85 declarations from version 1.2.1,
including their exact attributes and defaults. Version 1.3.0 adds 71 declarations
for streaming CRC, CRC32C, CRC64 and the two CRC16 variants. The unreleased
Adler-32 APIs add 18 declarations while preserving all 156 declarations from
1.3.0. The additional CRC64 variants and Internet Checksum add 19 declarations,
preserving all 174 prior declarations. CRC8 SAE-J1850/AUTOSAR H2F add only two
enum members, preserving all 193 prior declarations. The remaining CRC,
Fletcher/OSPF and xxHash additions retain those 195 entries and add 92 reviewed
declarations. The complete snapshot has 287 entries and 44 public types.
Keep existing declarations unchanged when
reviewing future additions; the snapshot
test and package consumer still require an exact match with the complete reviewed
surface.

`CrcAccumulatorTests` contributes 296 cases, `CrcAlgorithmTests` contributes 113,
and `CrcStreamTests` contributes 262.
`Crc32CTests` adds 153
calculation/frame/accumulator cases and `Crc32CStreamTests` adds 74 stream cases.
`Crc64Tests` adds 90 focused cases for calculation, frames, accumulators and streams.
`Crc16VariantTests` adds 166 focused cases. The shared algorithm, accumulator and
stream suites also cover both added CRC16 variants.
`Adler32Tests` adds 104 calculation/frame/accumulator cases and
`Adler32StreamTests` adds 32 stream cases.
`Crc64VariantTests` adds 123 cases, `InternetChecksumTests` adds 95 and
`InternetChecksumStreamTests` adds 30.
`Crc8VariantTests` adds 81 focused cases and `Crc8VariantStreamTests` adds 33;
the existing algorithm/accumulator suites also execute 36 additional cases.
The remaining CRC variants add 219 focused cases and 18 stream cases, plus
108 further cases in shared enum-driven suites. `XxHashTests` contributes 198
cases and `XxHashStreamTests` 60; `Fletcher4Tests` contributes 60,
`Fletcher4StreamTests` 45 and `OspfLsaChecksumTests` 57.
The TRX completeness check requires at least 3,478 passing tests, including the
public API guard and every required CRC, Adler-32, Internet Checksum, xxHash,
Fletcher4 and OSPF suite. Missing required suites block release selection; the
64 release-gate regression cases cover those omissions.
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

Version 1.3.0 includes CRC16 CCITT-FALSE/X25, CRC32C, CRC64/NVME and streaming CRC.
Adler-32, CRC64/ECMA-182, CRC64/XZ, Internet Checksum, all further CRC variants,
xxHash, Fletcher4 and OSPF remain unreleased source additions; their README
sections and release notes state that availability.
Candidate packages remain validation outputs; this validation step does not publish them.

Adler-32 correctness uses 42 distinct non-empty published fixed answers plus a
separate empty-input answer from Go 1.25.0, pinned to commit
`6e676ab2b809d46623acb5988248d95d1eb7939c`. The source and BSD notice accompany
the literals. A corpus guard requires at least 30 distinct non-empty inputs.
A separate weighted-sum reference uses positional coefficients, rather than
the product's running second sum, and is calibrated against every fixed answer.
Tests cover numeric/digest and frame paths, chunk splits, reset, 5,552-byte
reduction boundaries, 64 KiB buffer boundaries, overflow, single-pass input,
synchronous/asynchronous streams, buffer reuse, cancellation and failures.
The numeric checksum follows RFC 1950; library frames use native byte order,
and zlib's big-endian serialization is documented and tested explicitly.
The ninth README consumer exercises calculation, frame validation, decoding,
accumulation, reset and both stream methods against fixed output.

CRC64 variant correctness uses 32 distinct non-empty ECMA-182 fixed answers
from .NET, the XCKS specification, Classless.Hasher, EmbeddedUtilityLibraries
and crc-any, plus 31 distinct non-empty XZ answers from Go's golden table.
Pinned source revisions or the specification snapshot hash accompany the data.
An independent normal-polynomial bitwise reference explicitly reflects input
and output for XZ, separately from the product's reflected lookup table.
Checks cover native digest packing, frames, arbitrary short splits, selected
long splits, reset, single-pass enumeration, 64 KiB boundaries and both stream
methods. The existing NVME coverage is retained. Go's `crc64.ECMA` names its
polynomial; its golden answers are used only for the XZ parameter set.

Internet Checksum correctness uses 40 distinct non-empty fixed answers from
Google gopacket v1.1.19, commit `a9779d139771f6a06fc983b18e0efd23ca30222f`.
The source fixture, line and byte offset accompany each input. Checksum fields
are zeroed, and TCP/UDP cases include the protocol pseudo-header and complete
segment. Expected answers are the source packet's published checksum fields;
they are not calculated by this library. An independent byte-lane reference,
the RFC 1071 example and exhaustive two-byte values check carry and byte order.
Additional cases cover odd chunk boundaries and snapshots, positive/negative
zero, long all-ones input, native frame validation, single-pass enumeration,
bounded buffer reuse, real asynchronous reads, cancellation and read failures.

The optional publish job selects the exact validated Ubuntu Release candidate
only after all four CI jobs and their artifact evidence pass. Ordinary CI keeps
publication disabled. See [Integrity releases](IntegrityRelease.md) for the
publication gates, authentication setup and non-publishing regression tests.

CRC8 SAE-J1850 and AUTOSAR H2F correctness uses 70 published fixed answers:
40 SAE-J1850 and 30 H2F distinct non-empty inputs, with corpus guards for each
algorithm. Both retain the seven official AUTOSAR rows and standard check.
SAE-J1850 also uses pinned EmbeddedUtilityLibraries, Nicoretti/crc, ratriches/crc
and jmacheta/checksum fixtures; the last source freezes complete CRCs for
prefixes of its public binary message pool, rather than lookup-table entries.
H2F additionally uses pinned autosar-e2e Profile 2 output frames, js-crc and
CRC-Forge fixtures. Profile 2 raw input is the published protected payload
followed by the selected Data ID; the expected result is the literal frame CRC
byte. No expected CRC is derived from residue arithmetic or the production code.
Source links, revisions, input mapping and applicable notices accompany the
literals. CI requires no external calculator or network access for these tests.

The existing normal-polynomial bitwise oracle is calibrated against every
published answer. Existing enum-driven suites then cover all single- and
two-byte inputs, binary/random boundaries, partitions, snapshots, reset and
span ownership for both new algorithms. Focused suites cover numeric/digest,
frames and bit corruption, all splits of short public inputs, synchronous and
asynchronous streams, short reads, 64 KiB buffers, current position, cancellation,
read errors and stream ownership. Preserve CRC8's existing no-value semantics
for empty input. The twelfth README consumer exercises both models through
calculation, frame validation/decoding, chunking, reuse and both stream methods.


## Additional algorithms and published answers

All ten additional models retain at least thirty distinct nonempty public
input/output facts. For xxHash, the counts below use only seed-zero inputs;
extra seeded and empty cases are tested separately.

| Algorithm | Distinct nonempty public answers |
| --- | ---: |
| CRC8 Sensirion SHT4x / NRSC-5 | 31 |
| CRC16 MCRF4XX | 30 |
| CRC16 USB | 30 |
| CRC32 MPEG-2 | 30 |
| CRC32 BZIP2 | 30 |
| CRC32 AUTOSAR P4 | 37 |
| XXH32 | 48 |
| XXH64 | 49 |
| OpenZFS Fletcher4 | 30 |
| OSPF LSA Fletcher checksum | 39 |

The six CRC models use thirty frozen whole-message answers each from
jmacheta/checksum commit `39358d88bb755c74ca0f50096eb607a564155755`.
The literal message pool, byte lengths and final CRC array entries are retained;
they are not table entries or bit-length variants. Sensirion's official
`BE EF -> 92` example and seven official AUTOSAR P4 answers provide further
calibration. A normal-polynomial bitwise reference covers both reflected and
unreflected models. Existing shared suites exercise all one-/two-byte inputs,
partitions, reset and span ownership for each newly added enum value.

XXH32/64 use 155 public fixed answers from pinned jmacheta/checksum, pierrec/xxHash
and upstream xxHash fixtures. Empty, nonzero and wide seeds supplement the 97
nonempty seed-zero cases. A whole-message BigInteger reference uses modular
arithmetic independently of the product's bounded streaming state. Tests cover
16-/32-byte stripes, tail boundaries, prefix queries, reset, frame mutation,
single-pass inputs and synchronous/asynchronous stream lifecycle behavior.

Fletcher4 uses thirty literal checkpoints physically stored in OpenZFS's public
`dedup.zsend.bz2` at commit `34f96a15c73eab27dd6ad17bb5f1263bf26e37d7`.
The frozen input is a losslessly compressed byte slice of the original stream;
source hashes, BEGIN/checkpoint offsets and upstream field mapping accompany it.
Expected four-word values are transcribed from those fields rather than
calculated locally. A separate BigInteger weighted-sum reference tests both word
orders and wrapping. Partial words remain pending until completed; result queries
and EOF reject an incomplete word. Input-word order and native digest order are
independent and documented.

OSPF uses thirty-nine complete, distinct LSAs from pinned tcpdump captures,
including OSPFv2 and OSPFv3. Capture hashes, frame numbers and byte offsets identify
each public checksum field. Tests exclude LS age, check exact declared length,
verify checksum-field placement and mutation, and exercise maximum and malformed
LSAs. This protocol helper takes a complete LSA and does not append a generic
digest or parse an entire OSPF packet. Its independent weighted-sum oracle checks
field correction without reusing the product recurrence.
