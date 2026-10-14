# Mythosia.Integrity release notes

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

These changes are available in the source tree and have not been published to
NuGet. No package version or release date is assigned yet.

### CRC16 / CCITT-FALSE and X25

- Add `CRC16Type.CCITTFalse` (CRC-16/IBM-3740) and `CRC16Type.X25`
  (CRC-16/IBM-SDLC) to the existing detector, numeric/frame extensions,
  accumulator and synchronous/asynchronous stream APIs.
- Preserve all existing enum values, optional defaults, algorithms and native
  digest byte order. The new types return `FFFF` and `0000`, respectively, for
  empty input; their single-shot calculations accept single-pass enumerables.
- Add 80 published fixed vectors: 38 CCITT-FALSE and 42 X25 distinct non-empty
  inputs. Retain the AUTOSAR, ITU-T, ITSO and catalogue vectors and add fixed
  answers from pinned upstream library, RTOS, networking and EMV QR tests.
  Include short and long ASCII, binary, repeated and UTF-8 inputs and CRC values
  with leading zeros. Check these independently of production calculations.
- Extend the existing bitwise oracle, exhaustive short-input, partition and
  stream tests, including 64 KiB boundaries, cancellation and read failures.
- Add a package-backed README example and two reviewed enum declarations.
  Require the new vector suite in CI and release selection. No package version
  change or NuGet publication.
- Add 246 test cases through the focused vector suite and existing algorithm,
  accumulator and stream suites, bringing the complete Integrity suite to 2,092.
  Require at least 30 distinct non-empty published inputs for each new CRC16 type.

### CRC64 / NVME

- Add `CRC64(CRC64Type.NVME)`, `CRC64` / `WithCRC64` byte-sequence extensions,
  and `CRC64Accumulator` using the existing algorithm-selection pattern.
  NVME is the only defined CRC64 type and the default; numeric results are `ulong`.
- Add `ComputeCRC64` / `ComputeCRC64Async` stream methods using the existing
  reusable 64 KiB read loops, cancellation support and caller-owned stream lifetime.
- Preserve all existing APIs and behavior. CRC64 input is consumed once; empty
  input returns zero, digests use eight native-endian bytes, and digest-only
  frames are invalid. Unsupported algorithm values are rejected.
- Verify fixed CRC64/NVME answers from a pinned AWS test revision using a
  separately written bitwise reference, with essential incremental, framing,
  boundary and stream tests.
- Add an executable CRC64 README example and extend the existing package/API
  validation and CI result guard. No package version change or publication.
- Add 34 focused CRC64 test cases, bringing the complete Integrity suite to
  1,846. Preserve all 130 prior API declarations and review 24 CRC64 additions.

### CRC32C / Castagnoli

- Add a separate `CRC32C` detector and `CRC32C` / `WithCRC32C` byte-sequence
  extensions, preserving all existing APIs, enum values and CRC results.
- Add `CRC32CAccumulator` with `Append(ReadOnlySpan<byte>)`, non-destructive
  `GetCurrentValue()` and `Reset()`, plus `ComputeCRC32C` / `ComputeCRC32CAsync`
  stream extensions with cancellation and one reusable 64 KiB input buffer.
- Use the CRC-32/ISCSI parameters: polynomial `0x1EDC6F41`, reflected input and
  output, initial state and final XOR `0xFFFFFFFF`. Empty input returns zero.
- Follow the existing CRC32 native-endian digest and frame convention. CRC32C
  byte-sequence methods also support single-pass enumerables; use streams or
  accumulators for bounded input-buffer memory.
- Add fixed RFC 7143 and CRC RevEng/Google vectors, an independent normal-form
  bitwise reference, exhaustive two-byte inputs, partition and stream tests.
  Include the new tests and an executable package-backed README example in CI.
- Add 196 CRC32C test cases (122 calculation/frame/accumulator and 74 stream
  cases). Require both CRC32C suites in
  the CI result guard and test that missing suites block release selection.
- Retain the existing five README examples and their fixed output; add a sixth
  CRC32C example. Preserve all 113 prior API declarations and review 17 new ones.

### Streaming CRC

- Add `CRC8Accumulator`, `CRC16Accumulator` and `CRC32Accumulator` with
  `Append(ReadOnlySpan<byte>)`, non-destructive `GetCurrentValue()` and `Reset()`.
- Add `ComputeCRC8`, `ComputeCRC16` and `ComputeCRC32` stream extensions and
  asynchronous counterparts with cancellation support. Read once from the
  current position to end-of-stream using one reusable 64 KiB buffer, without
  requiring seeking or a known input length.
- Leave caller-owned streams open on success, cancellation and read failure.
  Bytes already read remain consumed; cancellation does not return a partial CRC.
- Preserve all released public API declarations and existing calculation
  behavior. Keep supported `Basic` aliases and the legacy CRC16/DNP numeric
  representation. Chunk boundaries do not affect results for the same ordered
  input and algorithm.
- Document explicit new-API empty-input behavior: CRC8 has no numeric value and
  throws `InvalidOperationException`; CRC16 returns `FFFF` for Modbus and
  CCITTFalse, and zero for other variants, including DNP and X25; CRC32 returns zero.

### Verification and documentation

- Add 384 unit test cases for all supported CRC variants and aliases, including
  exhaustive two-byte combinations, every partition of small inputs, varying
  chunk sizes, split positions, 64 KiB boundaries, prefix snapshots and reset.
- Compare results with existing one-shot calculations, an independent bitwise
  reference and published or independently generated frozen vectors.
- Verify buffer reuse and lifetime, generated large streams, non-seekable and
  partial reads, current position, stream ownership, cancellation, read failures
  and asynchronous continuation behavior.
- Run all 2,092 Integrity tests in Windows/Linux Debug/Release CI, plus the
  existing benchmark correctness validation.
- Add executable streaming, CRC16 variants, CRC32C and CRC64 README examples with fixed expected
  output. Validate all eight examples, public API and metadata against actual
  candidate NuGet packages in each CI configuration.
