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
