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
