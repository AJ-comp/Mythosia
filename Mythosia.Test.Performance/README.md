# Performance benchmarks

Run these commands from the repository root with a .NET SDK and the .NET 8 runtime installed.
Use Release builds for measurement. The existing no-argument command still runs
the `EnumerableExtensionsBenchmark` benchmarks.

## Integrity coverage

`IntegrityBenchmarks` measures the public `Compute` and `IsError` calls:

| Parameter | Values |
| --- | --- |
| Algorithm | Checksum XOR, NMEA, modulo 256, two's complement; CRC8 Classic and Maxim; CRC16 Classic, Modbus, XModem and DNP; CRC32 Classic |
| Input | `byte[]`, `List<byte>`, repeatable `yield` iterator without a collection interface |
| Payload length | 1, 64, 4,096 and 65,536 bytes |
| Operation | `Compute`, `IsErrorValid`, `IsErrorCorruptedTrailer` |

This is **396 benchmark cases**. Obsolete `Basic` aliases share the corresponding
Classic implementations and remain covered by the correctness tests.

Inputs use a fixed xorshift32 seed (`0xD1B54A35`), making identical prefixes at
every payload length. Frame construction, input conversion, independent digest
verification and lookup table warming happen in `GlobalSetup`, outside timing.
Valid frames use independently calculated digests; corrupted frames flip one bit
in the last trailer byte. `Compute` is eager and returns its existing result
object to BenchmarkDotNet without an extra `ToArray` copy or enumeration.

The benchmark reuses the tests' independent bitwise `CrcReference.cs` as linked
source, without a dependency on xUnit or the test project. DNP preserves Mythosia's
existing swapped result, and multi-byte digests retain native byte order.
These are measurements of steady-state public calls; cold initialization, empty
inputs, exceptions and corrupted payloads remain covered by the functional tests.

## Validate every case without measuring

```powershell
dotnet run --project Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release -- --validate-integrity
```

This verifies the `123456789` known vectors and all 132 parameter combinations.
Each of the three operations is checked twice on the same input objects to verify
repeatability. It exits with an error if an expected digest or validation result
differs. This command does not report timing or allocation measurements.

## Check BenchmarkDotNet execution

```powershell
dotnet run --project Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release -- --filter '*IntegrityBenchmarks.*(Length: 4096, Algorithm: Crc32Classic, Input: *)' --job dry --artifacts './BenchmarkDotNet.Artifacts/integrity-crc32-4k-dry'
```

This runs nine cases: three input forms and three operations. `Dry` verifies the
execution and reporting pipeline; its timings and allocations are not suitable
for performance comparisons. `--list flat` lists distinct method names rather
than every parameter combination; use the run log or JSON report to check the
number of executed cases.

## Measure time and allocations

For a short measurement of the same nine cases:

```powershell
dotnet run --project Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release -- --filter '*IntegrityBenchmarks.*(Length: 4096, Algorithm: Crc32Classic, Input: *)' --job short --artifacts './BenchmarkDotNet.Artifacts/integrity-crc32-4k-short'
```

For the default, longer measurement of one case:

```powershell
dotnet run --project Mythosia.Test.Performance/Mythosia.Test.Performance.csproj -c Release -- --filter '*IntegrityBenchmarks.IsErrorValid(Length: 4096, Algorithm: Crc32Classic, Input: Yield)' --artifacts './BenchmarkDotNet.Artifacts/integrity-crc32-4k-yield'
```

Change the method or parameter names to select other cases. Use
`--filter '*IntegrityBenchmarks*'` to run the complete matrix; the default job can
take several hours for all 396 cases. `--list flat` lists available methods,
including the existing Enumerable benchmarks.

`MemoryDiagnoser` reports managed bytes allocated per operation and collection
counts. The `results` subdirectory contains CSV, Markdown, HTML and full JSON
reports, including timing statistics, raw measurements and runtime/machine
metadata. Compare measurements on the same machine, runtime, power settings and
job with other CPU-intensive processes idle. Keep before/after results in separate
artifact directories; repeating the same directory overwrites its reports.
