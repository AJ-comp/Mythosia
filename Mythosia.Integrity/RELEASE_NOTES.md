# Mythosia.Integrity release notes

## 1.3.0 - 2026-10-15

This minor release adds CRC algorithms and incremental and streaming calculation
while preserving the existing public API and algorithm results.

### Added algorithms

- Add `CRC16Type.CCITTFalse` (CRC-16/IBM-3740) and `CRC16Type.X25`
  (CRC-16/IBM-SDLC) to the existing CRC16 detector, numeric/frame extensions,
  accumulator and synchronous/asynchronous stream APIs. The `123456789` check
  values are `29B1` and `906E`, respectively.
- Add the separate `CRC32C` detector, `CRC32C` / `WithCRC32C` extensions,
  `CRC32CAccumulator`, and `ComputeCRC32C` / `ComputeCRC32CAsync` stream methods.
  CRC-32/ISCSI (Castagnoli) returns `E3069283` for `123456789`.
- Add `CRC64`, `CRC64` / `WithCRC64` extensions, `CRC64Accumulator`, and
  `ComputeCRC64` / `ComputeCRC64Async` stream methods. `CRC64Type.NVME` is the
  only defined CRC64 algorithm and the default. Its numeric result is `ulong`;
  `123456789` returns `AE8B14860A799888`.

### Incremental and streaming CRC

- Add `CRC8Accumulator`, `CRC16Accumulator` and `CRC32Accumulator`, alongside
  the new CRC32C and CRC64 accumulators. `Append(ReadOnlySpan<byte>)` processes
  successive chunks; `GetCurrentValue()` reads the current CRC without changing
  state, and `Reset()` starts a new calculation.
- Add `ComputeCRC8`, `ComputeCRC16` and `ComputeCRC32` stream extensions and
  asynchronous counterparts with cancellation support. All CRC stream methods
  read from the current position to EOF using one reusable 64 KiB input buffer,
  without requiring seeking or a known input length.
- Keep streams open on success, cancellation and read failure. Bytes already
  read remain consumed; cancellation returns no partial CRC.
- Match the corresponding existing numeric result for the same non-empty bytes
  and algorithm, regardless of chunk sizes or split positions.
- Document empty-input behavior: CRC8 has no numeric value and throws
  `InvalidOperationException`; CRC16 returns `FFFF` for Modbus and CCITTFalse,
  and zero for the other variants; CRC32, CRC32C and CRC64 return zero.
- Support single-pass enumerables in the new CRC32C and CRC64 byte-sequence
  APIs and in the new CRC16 variants' calculations. Use accumulators or streams
  when input-buffer memory must remain bounded.

### Compatibility

- Retain all 85 released public declarations, including existing enum values,
  optional defaults, attributes and obsolete aliases. The reviewed surface has
  156 declarations across 25 public types, with 71 additive declarations.
- Preserve existing calculation results, empty-input behavior, input-enumeration
  requirements, native digest byte order and the legacy byte-swapped CRC16/DNP
  numeric representation.
- Follow the existing native byte order for CRC32C and CRC64 digests. Their frame
  APIs require a non-empty payload; use explicit serialization when a protocol
  specifies a fixed byte order.
- Keep the assembly version at `1.2.0.0`; update the package version to `1.3.0`
  and the file version to `1.3.0.0`.
- Continue targeting .NET Standard 2.1 with a dependency on Mythosia 1.4.0 or later.

### Verification, documentation and release automation

- Verify 175 published fixed answers for distinct non-empty inputs within each
  added algorithm: 38 CCITT-FALSE, 42 X25, 37 CRC32C and 58 CRC64/NVME. Sources
  include AUTOSAR, ITU-T, ITSO, RFC 7143, CRC RevEng and pinned upstream tests.
  Keep inputs, expected values and provenance in the tests, and require at least
  30 distinct published inputs per added algorithm.
- Calibrate independent bitwise calculations against those fixed answers; check
  numeric/digest and frame paths, arbitrary chunk splits, exhaustive short
  inputs, prefix snapshots, reset and synchronous/asynchronous streams.
- Cover 64 KiB boundaries, generated large inputs, buffer reuse and lifetime,
  non-seekable streams, partial reads, current position, stream ownership,
  cancellation and read failures. Tests do not fetch or generate expected answers
  from the library during CI.
- Run all 2,179 Integrity tests on Windows and Linux in Debug and Release, together
  with benchmark correctness validation for 132 parameter combinations and
  396 benchmark cases.
- Compile and execute all eight independent README examples from actual candidate
  NuGet packages, comparing their output with fixed reviewed expectations. Check
  packaged public API, metadata, assembly/file versions and artifact hashes.
- Add 50 release-gate regression cases. Allow an explicitly requested NuGet
  publication only after all four CI configurations and their artifact evidence
  pass; publish the exact validated Ubuntu Release package without repacking.
  Ordinary CI does not publish packages.

## 1.2.1 - 2026-10-14

This patch improves initialization safety and calculation efficiency while
preserving the public API and existing algorithm results.

### Runtime improvements

- Make CRC16 and CRC32 lookup-table initialization safe during concurrent first
  use, so callers cannot observe partially initialized tables.
- Reduce repeated enumeration in checksum, CRC16/DNP and CRC32 calculations,
  and avoid redundant frame-length checks during CRC/checksum validation.
- Add typed byte-array traversal for CRC8 and CRC16 calculations.
- Write CRC16 and CRC32 digest bytes directly, removing boxing, marshaling and
  temporary serialization buffers.

### Compatibility

- Preserve public types, members, overloads and optional defaults.
- Preserve existing calculation results, empty-input behavior and input
  enumeration requirements.
- Preserve native byte order for CRC16/CRC32 digests and the legacy byte-swapped
  CRC16/DNP numeric representation.
- Keep the assembly version at `1.2.0.0`; update the package version to `1.2.1`
  and the file version to `1.2.1.0`.
- Target .NET Standard 2.1 with a dependency on Mythosia 1.4.0 or later.

### Verification and documentation

- Expand independent reference-vector, boundary, randomized, frame-validation,
  input-enumeration and concurrent-initialization tests.
- Add a public API snapshot guard and benchmark correctness validation.
- Run Integrity tests and benchmark validation on Windows and Linux in both
  Debug and Release through GitHub Actions.
- Correct README namespaces, required arguments and variable declarations;
  provide independent executable examples with expected results and document
  input and byte-order behavior.

## Unreleased

These additions are available in source builds and are not included in the
published 1.3.0 package. No new package version is assigned yet.

### Adler-32

- Add `Adler32`, `Adler32` / `WithAdler32` byte-sequence extensions and
  `Adler32Accumulator` in `Mythosia.Integrity.Checksum`. Use the RFC 1950
  parameters; ASCII `123456789` produces `091E01DE`, and empty input produces
  `00000001`.
- Add `ComputeAdler32` / `ComputeAdler32Async` stream extensions with one reusable
  64 KiB buffer, current-position reads, non-seekable stream support and
  asynchronous cancellation. Leave the caller's stream open after success,
  cancellation or a read failure.
- Consume byte sequences once. Accumulators retain only calculation state,
  support non-destructive current values and reset, and give the same final
  value across arbitrary chunk boundaries.
- Follow the library's native-endian four-byte frame convention. Document
  explicit big-endian serialization for the zlib format. Reject digest-only
  frames; keep all existing APIs, defaults and algorithm behavior unchanged.
- Verify 42 distinct non-empty published fixed answers, plus the separate
  empty-input answer, from pinned Go 1.25.0 tests. Compare with an independent
  weighted-sum reference and cover 5,552-byte reduction boundaries, 64 KiB
  buffers, overflow, partitions, frames and stream lifecycle behavior.
- Add an executable README example and review 18 additive declarations,
  preserving all 156 declarations from 1.3.0. Require both Adler-32 suites in CI
  and release selection; test that missing suites block publication.
- Keep the package version and released metadata unchanged. This source change
  does not publish a NuGet package.

### CRC64 / ECMA-182 and XZ

- Add `CRC64Type.ECMA182` and `CRC64Type.XZ` to the existing CRC64 detector,
  numeric/frame extensions, accumulator and synchronous/asynchronous stream APIs.
  Keep `NVME = 0` and every existing optional default unchanged.
- Use separate ECMA-182 and XZ parameter sets. For ASCII `123456789`, ECMA-182
  returns `6C40DF5F0B497347` and XZ returns `995DC9BBDF1939FA`; empty input
  returns zero for either algorithm. Keep the existing native digest byte order.
- Verify 32 ECMA-182 and 31 XZ distinct non-empty published fixed answers,
  with an independent bitwise reference and numeric/digest, frame, partition,
  accumulator and stream checks. Distinguish Go's `crc64.ECMA` results, which use
  the XZ parameter set, from true ECMA-182 answers.
- Retain the original NVME test coverage and calculation behavior. Update only
  the enum-list and first-undefined-value expectations affected by the additions.

### Internet Checksum

- Add `InternetChecksum`, numeric/frame extensions and
  `InternetChecksumAccumulator`, plus `ComputeInternetChecksum` and its
  asynchronous counterpart, in `Mythosia.Integrity.Checksum`.
- Follow RFC 1071: combine bytes into big-endian 16-bit words, fold end-around
  carries and complement the sum. Empty input returns `FFFF`; a calculated
  `0000` remains zero. Keep an odd trailing byte across appends and pad it only
  for a non-destructive current result or at EOF.
- Consume sequences once and use a reusable 64 KiB buffer for streams. Preserve
  current-position, partial-read, cancellation and caller-owned stream behavior.
- Use native-endian two-byte library frames and compare their digest with a
  recalculated payload checksum. Document big-endian network serialization,
  protocol checksum-field placement and caller-supplied TCP/UDP pseudo-headers.
- Verify 40 published fixed answers from pinned Google gopacket packet fixtures,
  including IPv4 options and odd-length transport data, plus the RFC 1071 example,
  an independent byte-lane sum, carry/zero boundaries, partitions and streams.

### CRC8 / SAE-J1850 and AUTOSAR H2F

- Add `CRC8Type.SaeJ1850 = 3` and `CRC8Type.AutosarH2F = 4` to the existing
  detector, numeric/frame extensions, accumulator and synchronous/asynchronous
  stream APIs. Preserve the `Basic`, `Classic` and `Maxim` values and defaults.
- Use non-reflected polynomials `1D` and `2F`, respectively, with initial
  register and final XOR `FF`. ASCII `123456789` produces `4B` and `DF`.
  Keep raw accumulation state separate from the final XOR, support repeated
  prefix reads and restore the selected initial state on reset.
- Preserve CRC8's existing empty-input behavior: no digest from `Compute`,
  exceptions from numeric/frame encoding extensions, and no value from a
  new/reset accumulator or empty stream. Keep existing calculation paths and
  frame-enumeration contracts unchanged.
- Verify 40 SAE-J1850 and 30 AUTOSAR H2F distinct non-empty published answers
  from the AUTOSAR specification and pinned upstream fixtures. Record exact
  source revisions and input mapping; H2F Profile 2 fixture inputs include the
  published payload and selected Data ID. These are raw CRC tests rather than
  an implementation of an AUTOSAR end-to-end profile.
- Calibrate the independent bitwise reference against every fixed answer.
  Reuse the existing exhaustive one-/two-byte, partition and accumulator tests
  for both variants, and add focused frame, split and stream-lifecycle coverage.
- Add 150 test cases, including 81 focused calculation/frame/vector cases,
  33 stream cases and 36 additional cases in the existing shared suites.

### Additional CRC variants

- Add `CRC8Type.SensirionSht4x = 5`, `CRC16Type.MCRF4XX = 7` and `USB = 8`,
  and `CRC32Type.Mpeg2 = 2`, `Bzip2 = 3` and `AutosarP4 = 4` across existing
  calculation, frame, accumulator and synchronous/asynchronous stream APIs.
- Preserve every existing enum value, optional default, calculation path and
  empty-input contract. New MCRF4XX empty input returns `FFFF`; new MPEG-2 empty
  input returns `FFFFFFFF`. USB, BZIP2 and AUTOSAR P4 return zero. Sensirion keeps
  CRC8's existing no-value empty-input behavior.
- Verify 188 distinct non-empty published answers: 31 Sensirion, 30 MCRF4XX,
  30 USB, 30 MPEG-2, 30 BZIP2 and 37 AUTOSAR P4. Retain exact pinned message-pool
  inputs and final CRC values, the manufacturer's `BE EF -> 92` example and
  seven official AUTOSAR P4 vectors. Document protocol-specific input selection.

### xxHash32 and xxHash64

- Add `XxHash32` and `XxHash64` detectors, numeric/frame extensions, accumulators
  and synchronous/asynchronous stream methods in `Mythosia.Integrity.Checksum`.
  Support unsigned 32-/64-bit seeds, defaulting to zero and retained on reset.
- Keep only 16 or 32 pending bytes and the hash state; snapshots do not finalize
  the accumulator. Read input words as little-endian on every platform and use
  wrapping prime arithmetic. Empty seed-zero values are `02CC5D05` and
  `EF46DB3751D8E999`.
- Use native-endian library digest frames and distinguish them from the
  canonical big-endian xxHash digest representation and compression formats.
- Verify 155 published fixed answers, including 48 XXH32 and 49 XXH64 distinct
  non-empty seed-zero inputs, plus empty and nonzero/wide-seed cases. Compare
  against an independent whole-message BigInteger reference and cover stripe,
  tail, partition, reset, frame and stream lifecycle boundaries.

### Fletcher4 and OSPF LSA checksums

- Add OpenZFS `Fletcher4`, numeric/frame extensions, `Fletcher4Accumulator` and
  stream methods. Return a `Fletcher4Value` containing four unsigned 64-bit sums.
  Select input-word order explicitly with `Fletcher4ByteOrder`, defaulting to
  little-endian independently of the host. Library digest frames serialize the
  four sums in native byte order.
- Preserve partial 32-bit words across appends. A result requires a total byte
  length divisible by four; incomplete words are not silently padded. Empty
  input returns four zeros. Reset clears sums and pending bytes.
- Add `OspfLsaChecksum.Compute`, `Write` and `IsValid` for one complete OSPFv2 or
  OSPFv3 LSA. Exclude LS age, use the checksum field at bytes 16-17 in network
  order and validate the declared length. `Write` changes only those two bytes.
- Verify 30 literal OpenZFS send-stream Fletcher4 checkpoints and 39 complete
  LSAs from pinned tcpdump captures. Preserve exact source bytes, file hashes,
  checkpoint/frame offsets and notices. Independent weighted-sum references
  cover wrapping, word order and OSPF field correction.

### Verification and documentation

- Preserve all 195 prior API declarations and review 92 additive declarations.
  The complete surface has 287 declarations across 44 public types.
- Retain every previous unit test and expand shared enum-driven cases for the
  new CRCs. Add 765 cases, for 3,478 Integrity tests across all four CI
  configurations. Every newly added algorithm has at least 30 distinct non-empty
  published fixed answers; no lookup-table entries or locally generated answers
  are counted as public vectors.
- Preserve all 12 previous README examples and their fixed output; add six
  independent examples for all new CRC, xxHash, Fletcher4 and OSPF APIs. Validate
  all 18 against actual candidate NuGet packages, public API and metadata in CI.
- Require all seven additional calculation/stream/OSPF suites in test evidence
  and release selection. Add missing-suite rejection cases for 64 release-gate
  regression cases in total. Keep publication disabled for ordinary CI runs and
  keep the released package version and metadata unchanged.
