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
  throws `InvalidOperationException`; CRC16 returns `FFFF` for Modbus and zero
  for other variants, including DNP; CRC32 returns zero.

### Verification and documentation

- Add 384 unit test cases for all supported CRC variants and aliases, including
  exhaustive two-byte combinations, every partition of small inputs, varying
  chunk sizes, split positions, 64 KiB boundaries, prefix snapshots and reset.
- Compare results with existing one-shot calculations, an independent bitwise
  reference and published or independently generated frozen vectors.
- Verify buffer reuse and lifetime, generated large streams, non-seekable and
  partial reads, current position, stream ownership, cancellation, read failures
  and asynchronous continuation behavior.
- Verify all 1,616 Integrity tests in local Debug and Release builds, plus the
  existing benchmark correctness validation.
- Add two executable README examples with fixed expected output. Validate all
  five examples, public API and metadata against actual candidate NuGet packages
  in local Debug and Release builds.
