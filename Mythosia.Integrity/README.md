# Mythosia.Integrity

CRC and 8-bit checksum calculation, frame encoding and error detection for byte
sequences. Hash algorithms are provided by `Mythosia.Security.Cryptography`.

Version `1.2.1` improves initialization safety and calculation efficiency while
preserving the public API and existing results. See the
[release notes](https://github.com/AJ-comp/Mythosia/blob/master/Mythosia.Integrity/RELEASE_NOTES.md).

The CRC16 CCITT-FALSE/X25, CRC32C, CRC64 and streaming CRC sections describe
unreleased source changes.
Use a source build until a package containing these APIs is published.

Each C# block below is a complete, independent example for a .NET 8 console
application referencing `Mythosia.Integrity`. Copy one block into `Program.cs`.
All examples use the ASCII bytes of `123456789`.

## CRC extensions

Numeric extensions return a `byte`, `ushort` or `uint` for CRC8, CRC16 and CRC32,
respectively. `WithCRC8`, `WithCRC16` and `WithCRC32` append the digest to the
payload and return `IEnumerable<byte>`; use `ToArray()` to materialize a frame.

```csharp
using System;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");

Console.WriteLine($"CRC8 Classic: {data.CRC8(CRC8Type.Classic):X2}");          // F4
Console.WriteLine($"CRC8 Maxim: {data.CRC8(CRC8Type.Maxim):X2}");              // A1
Console.WriteLine($"CRC16 Classic: {data.CRC16(CRC16Type.Classic):X4}");       // BB3D
Console.WriteLine($"CRC16 Modbus: {data.CRC16(CRC16Type.Modbus):X4}");         // 4B37
Console.WriteLine($"CRC16 XModem: {data.CRC16(CRC16Type.CCITTxModem):X4}");    // 31C3
Console.WriteLine($"CRC16 DNP: {data.CRC16(CRC16Type.DNP):X4}");               // 82EA (legacy representation)
Console.WriteLine($"CRC32 Classic: {data.CRC32(CRC32Type.Classic):X8}");       // CBF43926

byte[] crc8Frame = data.WithCRC8(CRC8Type.Classic).ToArray();
byte[] crc16Frame = data.WithCRC16(CRC16Type.Modbus).ToArray();
byte[] crc32Frame = data.WithCRC32(CRC32Type.Classic).ToArray();

Console.WriteLine($"CRC8 frame valid: {!new CRC8(CRC8Type.Classic).IsError(crc8Frame)}");
Console.WriteLine($"CRC16 frame valid: {!new CRC16(CRC16Type.Modbus).IsError(crc16Frame)}");
Console.WriteLine($"CRC32 frame valid: {!new CRC32(CRC32Type.Classic).IsError(crc32Frame)}");
// All three frame checks print True.
```

The examples select each algorithm explicitly. The obsolete `Basic` enum members
remain supported and calculate the same result as `Classic`. CRC16 and CRC32
extension methods retain `Basic` as their optional default; their constructors
default to `Classic`.

`CRC16Type.DNP` preserves Mythosia's existing byte-swapped numeric representation:
this input returns `0x82EA`. The canonical CRC-16/DNP check value is `0xEA82`, as
listed in the [CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/16.htm#crc.cat.crc-16-dnp).

## Checksum extensions

Use `Checksum8`, `WithChecksum8` and `Checksum8Type`. The older `CheckSum8`,
`WithCheckSum8` and `CheckSum8Type` spellings remain supported as obsolete aliases.

```csharp
using System;
using System.Linq;
using System.Text;
using Mythosia.Integrity.Checksum;

byte[] data = Encoding.ASCII.GetBytes("123456789");

Console.WriteLine($"XOR: {data.Checksum8(Checksum8Type.Xor):X2}");                        // 31
Console.WriteLine($"NMEA: {data.Checksum8(Checksum8Type.NMEA):X2}");                      // 31
Console.WriteLine($"Modulo 256: {data.Checksum8(Checksum8Type.Modulo256):X2}");            // DD
Console.WriteLine($"Two's complement: {data.Checksum8(Checksum8Type.TwosComplement):X2}"); // 23

byte[] frame = data.WithChecksum8(Checksum8Type.Xor).ToArray();
var detector = new Checksum8(Checksum8Type.Xor);
Console.WriteLine($"Checksum frame valid: {!detector.IsError(frame)}"); // True
```

`Checksum8Type.NMEA` applies XOR to the supplied bytes. It does not parse an NMEA
sentence or add `$`, `*` or hexadecimal ASCII checksum characters.

## Common API: compute, encode and validate

All detectors inherit from `ErrorDetection`:

| Method | Result |
| --- | --- |
| `Compute(source)` | Digest bytes for the payload. |
| `Encode(source)` | Payload followed by the digest bytes. |
| `IsError(frame)` | `true` when the frame is too short or its digest does not match. |
| `Decode(frame)` | Original payload for a valid frame; an empty sequence for an invalid frame. |

Pass the payload to `Compute`; there is no parameterless overload. The following
example uses the same API with checksum and CRC detectors, then changes one
digest bit to demonstrate error detection.

```csharp
using System;
using System.Linq;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;

byte[] content = Encoding.ASCII.GetBytes("123456789");
ErrorDetection[] detectors =
{
    new Checksum8(Checksum8Type.Xor),
    new Checksum8(Checksum8Type.Modulo256),
    new CRC8(CRC8Type.Classic),
    new CRC16(CRC16Type.Classic),
    new CRC32(CRC32Type.Classic)
};

foreach (ErrorDetection detector in detectors)
{
    byte[] digest = detector.Compute(content).ToArray();
    byte[] encoded = detector.Encode(content).ToArray();
    byte[] decoded = detector.Decode(encoded).ToArray();

    Console.WriteLine($"{detector.GetType().Name}/{detector.GetDetectionType()}: " +
        $"digest={Convert.ToHexString(digest)}, " +
        $"valid={!detector.IsError(encoded)}, decoded={decoded.SequenceEqual(content)}");

    byte[] damaged = (byte[])encoded.Clone();
    damaged[damaged.Length - 1] ^= 0x01;
    Console.WriteLine($"Damaged frame: {detector.IsError(damaged)}");
}
// Every detector prints valid=True, decoded=True and Damaged frame: True.
```

## CRC16 / CCITT-FALSE and X25 (unreleased)

Select `CRC16Type.CCITTFalse` or `CRC16Type.X25` with the existing CRC16 APIs.
Both are additions; existing enum values, defaults and CRC results are unchanged.
The polynomial is `0x1021`, but the initialization and reflection settings differ
from the existing `CCITTxModem` algorithm:

| Type | Standard name | Initial state | Reflect input/output | Final XOR | `123456789` | Empty input |
| --- | --- | --- | --- | --- | --- | --- |
| `CCITTFalse` | CRC-16/IBM-3740 (CCITT-FALSE) | `FFFF` | No | `0000` | `29B1` | `FFFF` |
| `X25` | CRC-16/IBM-SDLC (X-25) | `FFFF` | Yes | `FFFF` | `906E` | `0000` |

Tests use 80 published, fixed answers: 38 for CCITT-FALSE and 42 for X25,
with distinct non-empty inputs for each algorithm. They include seven
CCITT-FALSE vectors from
[AUTOSAR CRC Routines, R24-11, SWS_Crc_00078](https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf),
four X25 frames from
[ITU-T X.25 (10/96), Appendix I](https://www.itu.int/rec/dologin_pub.asp?id=T-REC-X.25-199610-I!!PDF-E&lang=e&type=items),
and three CRC_B examples from the
[ITSO TS 1000-2:2025-03, Annex A.1.1](https://www.itso.org.uk/download/42151.2/part-2-customer-media-data-and-customer-media-architecture/).
The two standard check values are also listed in the
[CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/16.htm).
Additional fixed answers come from pinned upstream tests in libcrc, howeyc/crc16,
Archipel, P4, RIOT, cppcrc, Tcllib, snksoft/crc, npat-efault/crc16 and two EMV QR
implementations. Cases cover 1-to-554-byte inputs, ASCII, binary bytes, repeated
data, UTF-8 packets and leading-zero CRC values. Exact inputs, expected values
and source commits are recorded in `Crc16VariantTests.cs`. Tests use these frozen
answers without network access or generating expected values from the library.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
foreach (CRC16Type type in new[] { CRC16Type.CCITTFalse, CRC16Type.X25 })
{
    var detector = new CRC16(type);
    Console.WriteLine($"CRC16 {type}: {data.CRC16(type):X4}");
    byte[] frame = data.WithCRC16(type).ToArray();
    Console.WriteLine($"CRC16 {type} frame valid: {!detector.IsError(frame)}");
    Console.WriteLine($"CRC16 {type} decoded: {detector.Decode(frame).SequenceEqual(data)}");

    var accumulator = new CRC16Accumulator(type);
    accumulator.Append(data.AsSpan(0, 4));
    accumulator.Append(data.AsSpan(4));
    Console.WriteLine($"Chunked CRC16 {type}: {accumulator.GetCurrentValue():X4}");

    using var syncStream = new MemoryStream(data, writable: false);
    using var asyncStream = new MemoryStream(data, writable: false);
    Console.WriteLine($"Stream CRC16 {type}: {syncStream.ComputeCRC16(type):X4}");
    Console.WriteLine($"Async stream CRC16 {type}: {await asyncStream.ComputeCRC16Async(type):X4}");
}
// CCITTFalse values are 29B1; X25 values are 906E. Frame checks print True.
```

Both types work with `CRC16`, `CRC16Accumulator`, `CRC16` / `WithCRC16` and
`ComputeCRC16` / `ComputeCRC16Async`. Reading an accumulator does not finalize or
reset its state; appending further bytes continues the same calculation.
The new types' `Compute` and numeric extension support single-pass enumerables.
The inherited frame methods still require repeatable input.

Digest and frame bytes retain the existing CRC16 native byte order. Protocol
serialization is separate: X.25 sends the low FCS byte first, whereas
CCITT-FALSE protocols may specify a different order. Serialize the numerical
result explicitly when implementing a protocol; these APIs do not add flags,
bit stuffing or protocol headers.

## CRC32C / Castagnoli (unreleased)

CRC32C is available through separate APIs in `Mythosia.Integrity.CRC`:
`data.CRC32C()`, `data.WithCRC32C()` and the `CRC32C` detector. The existing
`CRC32` APIs and `CRC32Type` values keep their existing behavior. For the ASCII
input `123456789`, CRC32C returns `E3069283` and CRC32 Classic returns `CBF43926`.

The algorithm is CRC-32/ISCSI (Castagnoli): polynomial `0x1EDC6F41`, initial
register and final XOR `0xFFFFFFFF`, with reflected input and output. Published
reference vectors are in [RFC 7143, Appendix A.4](https://www.rfc-editor.org/rfc/rfc7143.html#appendix-A.4)
and the [CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/17plus.htm#crc.cat.crc-32-iscsi).

Tests contain 37 distinct non-empty inputs with published fixed answers,
including the RFC vectors, Google's 2048-byte binary pattern and 30 Castagnoli
golden answers from
[Go 1.25.0](https://github.com/golang/go/blob/go1.25.0/src/hash/crc32/crc32_test.go).
Each is checked against an independent bitwise calculation, one-shot APIs,
every two-chunk split and both stream APIs. A corpus guard requires at least
30 distinct non-empty published inputs. Expected values are frozen in the tests;
they are not generated by the library or fetched during CI.

`CRC32CAccumulator` provides the same `Append`, `GetCurrentValue` and `Reset`
pattern as the other accumulators. `ComputeCRC32C` and `ComputeCRC32CAsync` read
from the current stream position to EOF using one reusable 64 KiB input buffer.
They leave the stream open; the async method accepts `cancellationToken`.
Chunk sizes and split positions do not change the CRC for the same ordered data.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
var detector = new CRC32C();

Console.WriteLine($"CRC32C: {data.CRC32C():X8}");
byte[] frame = data.WithCRC32C().ToArray();
Console.WriteLine($"CRC32C frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"CRC32C decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new CRC32CAccumulator();
accumulator.Append(data.AsSpan(0, 4));
accumulator.Append(data.AsSpan(4));
Console.WriteLine($"Chunked CRC32C: {accumulator.GetCurrentValue():X8}");
accumulator.Reset();
Console.WriteLine($"Reset CRC32C: {accumulator.GetCurrentValue():X8}");

using var syncStream = new MemoryStream(data, writable: false);
using var asyncStream = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream CRC32C: {syncStream.ComputeCRC32C():X8}");
Console.WriteLine($"Async stream CRC32C: {await asyncStream.ComputeCRC32CAsync():X8}");
// All CRC32C values are E3069283 except Reset, which returns 00000000.
// The frame validity and decoded-payload checks both print True.
```

Empty CRC32C input returns zero, including an empty accumulator or stream.
Empty appends have no effect, and reading the accumulator's current value does
not reset it. Appended buffers can be reused immediately. Accumulators require
no disposal; use a separate instance for each concurrent calculation.

`Compute`, `Encode`, `Decode`, `IsError` and the CRC32C byte-sequence extensions
accept single-pass enumerables. The frame methods buffer the payload or frame.
Use the accumulator or stream methods for input whose size should not determine
buffer memory. Null inputs throw `ArgumentNullException`.

`CRC32C.Compute` returns four digest bytes in the machine's native byte order,
matching the existing CRC32 framing convention. On a little-endian machine the
check input produces `83 92 06 E3`; on a big-endian machine it produces
`E3 06 92 83`. Protocols with a fixed byte order need explicit serialization of
the numerical CRC. `Encode` appends these digest bytes. As with CRC32, frames
containing only a digest are invalid: `IsError` returns `true` and `Decode`
returns an empty sequence for frames of four bytes or fewer.

## CRC64 / NVME (unreleased)

Select the CRC64 algorithm using `CRC64Type.NVME`, following the existing
`CRC16(type)` and `CRC32(type)` pattern. NVME is the only supported CRC64 type
and the default for every CRC64 constructor and extension. Numeric results use
`ulong`; the ASCII input `123456789` produces `AE8B14860A799888`.

This check value and the three original fixed answers come from Amazon's
[aws-checksums tests](https://github.com/awslabs/aws-checksums/blob/12cd22e81543b8547132762023076ab6afa25559/tests/crc64_test.c).
The algorithm uses polynomial `0xAD93D23594C93659`, reflected input/output,
and initial state and final XOR `0xFFFFFFFFFFFFFFFF`.

Tests contain 58 distinct non-empty inputs with published fixed answers. They
combine the original AWS vectors with AWS SDK fixtures,
[crc64fast-nvme](https://github.com/awesomized/crc64fast-nvme/blob/45116ba606e32c521e887e107e7353e019a576f9/src/lib.rs),
[SPDK](https://github.com/spdk/spdk/blob/ae5c062b91ae23ba466cd22c42a3fbc56a0b9885/test/unit/lib/util/crc64.c/crc64_ut.c)
and selected boundary cases from the published
[StorScale golden table](https://github.com/StorScale/buckets/blob/5833b187b0adb89505c22b65ca01e7a26ac0197d/tests/unit/golden_vectors.inc).
The latter's input pattern and independent Go reference calculation are recorded
alongside the source links in `Crc64Tests.cs`. Expected CRCs are fixed literals;
CI does not run an external generator or fetch reference answers. Numeric and
digest results, chunk splits and sync/async streams are checked against those
answers. A corpus guard requires at least 30 distinct non-empty published inputs.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
const CRC64Type algorithm = CRC64Type.NVME;
var detector = new CRC64(algorithm);

Console.WriteLine($"CRC64 NVME: {data.CRC64(algorithm):X16}");
byte[] frame = data.WithCRC64(algorithm).ToArray();
Console.WriteLine($"CRC64 frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"CRC64 decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new CRC64Accumulator(algorithm);
accumulator.Append(data.AsSpan(0, 4));
accumulator.Append(data.AsSpan(4));
Console.WriteLine($"Chunked CRC64 NVME: {accumulator.GetCurrentValue():X16}");
accumulator.Reset();
Console.WriteLine($"Reset CRC64 NVME: {accumulator.GetCurrentValue():X16}");

using var syncStream = new MemoryStream(data, writable: false);
using var asyncStream = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream CRC64 NVME: {syncStream.ComputeCRC64(algorithm):X16}");
Console.WriteLine($"Async stream CRC64 NVME: {await asyncStream.ComputeCRC64Async(algorithm):X16}");
// All CRC64 values are AE8B14860A799888 except Reset, which returns zero.
// The frame validity and decoded-payload checks both print True.
```

CRC64 has the same accumulator and stream lifetime rules described below.
Empty input returns zero; `GetCurrentValue` does not reset the accumulator.
Both `CRC64` and `CRC64Accumulator` expose the selected algorithm in `Type`.
Unsupported CRC64 enum values throw `ArgumentOutOfRangeException`.

CRC64 byte-sequence methods consume their input once. `Compute` returns eight
digest bytes in native machine byte order, and `Encode` appends those bytes.
For the check input, the bytes are `88 98 79 0A 86 14 8B AE` on a little-endian
machine and `AE 8B 14 86 0A 79 98 88` on a big-endian machine. Serialize the
numeric value explicitly when a protocol specifies a fixed byte order.
Frames require a non-empty payload: `IsError` rejects frames of eight bytes or
fewer, and `Decode` returns an empty sequence for an invalid frame. Frame methods
buffer the payload or frame; use the accumulator or stream methods for bounded
input-buffer memory. Null inputs throw `ArgumentNullException`.

## Streaming CRC (unreleased)

The following APIs are additions for the next release. Existing CRC methods and
their results remain unchanged. Both streaming forms return the same numerical
value as the corresponding numeric extension for non-empty input,
when the same algorithm processes the same bytes in the same order. Chunk sizes
and split positions do not affect the final value. CRC16 DNP keeps its legacy
representation.

| CRC | Stream methods | Incremental object | Numeric result |
| --- | --- | --- | --- |
| CRC8 | `ComputeCRC8` / `ComputeCRC8Async` | `CRC8Accumulator` | `byte` |
| CRC16 | `ComputeCRC16` / `ComputeCRC16Async` | `CRC16Accumulator` | `ushort` |
| CRC32 | `ComputeCRC32` / `ComputeCRC32Async` | `CRC32Accumulator` | `uint` |
| CRC32C | `ComputeCRC32C` / `ComputeCRC32CAsync` | `CRC32CAccumulator` | `uint` |
| CRC64 | `ComputeCRC64` / `ComputeCRC64Async` | `CRC64Accumulator` | `ulong` |

CRC8, CRC16 and CRC32 methods and constructors default to `Classic`. They support
the existing algorithm enums and `Basic` aliases. CRC32C has one fixed algorithm
and takes no algorithm argument. CRC64 defaults to `CRC64Type.NVME`.
Async methods return `Task<byte>`, `Task<ushort>`, `Task<uint>` or `Task<ulong>`.
All APIs are in `Mythosia.Integrity.CRC`.

### Read a stream

`ComputeCRC8`, `ComputeCRC16`, `ComputeCRC32`, `ComputeCRC32C` and `ComputeCRC64`
read from the stream's current position to its end. Their `Async` counterparts support a named
`cancellationToken` argument. Each calculation reuses one 64 KiB input buffer,
requires no seeking or known length, and leaves the stream open on success,
cancellation or failure. Pass a `FileStream` from `File.OpenRead` to process a
file; this complete example uses memory streams for the published check input.

For example, a 1 GiB file can be processed with a 64 KiB input buffer; input
buffer memory does not grow with file size. The buffer size is fixed by the
helper. With an accumulator, the caller controls the chunk sizes.

```csharp
using System;
using System.IO;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
using var crc8Stream = new MemoryStream(data, writable: false);
using var crc16Stream = new MemoryStream(data, writable: false);
using var crc32Stream = new MemoryStream(data, writable: false);

Console.WriteLine($"Stream CRC8 Classic: {crc8Stream.ComputeCRC8(CRC8Type.Classic):X2}");
Console.WriteLine($"Stream CRC16 Modbus: {await crc16Stream.ComputeCRC16Async(CRC16Type.Modbus):X4}");
Console.WriteLine($"Stream CRC32 Classic: {await crc32Stream.ComputeCRC32Async(CRC32Type.Classic):X8}");
// F4, 4B37 and CBF43926, respectively.
```

### Append arriving chunks

`CRC8Accumulator`, `CRC16Accumulator` and `CRC32Accumulator` consume a
`ReadOnlySpan<byte>` in each `Append` call and retain only the calculation state.
Input buffers may be overwritten or reused after `Append` returns. Empty chunks
are ignored. `GetCurrentValue` reads the current result without resetting or
changing the state; more chunks can be appended afterwards. `Reset` starts a new
calculation with the same algorithm. The objects require no disposal; use a
separate instance for each calculation and do not share one instance concurrently.

For a persistent network connection, append only the bytes belonging to one
message. Read the result at the message boundary, then reset for the next
message. Stream helpers finish at end-of-stream, so they treat all remaining
bytes as one payload.

```csharp
using System;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
var crc8 = new CRC8Accumulator(CRC8Type.Classic);
var crc16 = new CRC16Accumulator(CRC16Type.Modbus);
var crc32 = new CRC32Accumulator(CRC32Type.Classic);

for (int offset = 0; offset < data.Length; offset += 3)
{
    ReadOnlySpan<byte> chunk = data.AsSpan(offset, Math.Min(3, data.Length - offset));
    crc8.Append(chunk);
    crc16.Append(chunk);
    crc32.Append(chunk);
}

Console.WriteLine($"Chunked CRC8 Classic: {crc8.GetCurrentValue():X2}");
Console.WriteLine($"Chunked CRC16 Modbus: {crc16.GetCurrentValue():X4}");
Console.WriteLine($"Chunked CRC32 Classic: {crc32.GetCurrentValue():X8}");

crc32.Reset();
crc32.Append(data);
Console.WriteLine($"Reused CRC32 Classic: {crc32.GetCurrentValue():X8}");
// F4, 4B37, CBF43926 and CBF43926, respectively.
```

### Empty input, errors and validation

Before any non-empty append after construction or `Reset`, `CRC8Accumulator`
throws `InvalidOperationException` from `GetCurrentValue`. CRC8 stream methods
throw the same exception when there are no remaining bytes. The existing CRC8
API has no numeric result for empty input. Empty CRC16 returns
`FFFF` for Modbus and CCITTFalse, and `0000` for the other variants, including X25
and legacy DNP. Empty CRC32 and CRC32C return `00000000`; CRC64 returns zero.
Unsupported algorithm values throw
`ArgumentOutOfRangeException`.

Stream methods reject a null source with `ArgumentNullException` and an unreadable
source with `ArgumentException`. Read failures are propagated. Async methods
observe cancellation between reads and propagate `OperationCanceledException`;
they return no partial CRC. Bytes already read remain consumed after cancellation
or failure, and the stream stays open.

Streaming calculates a value. To validate a complete file or message, compare its
final value with the sender's expected CRC using the same algorithm and settings.
`GetCurrentValue` can also report a prefix CRC, but chunk-by-chunk validation
requires a separate expected CRC for each chunk.

## Input and byte order

For the existing frame APIs, use a non-empty payload. Existing empty-input
behavior differs by algorithm: CRC8 and Checksum8 return an empty digest, and
their numeric extensions and `Encode`
throw for empty inputs. CRC16 and CRC32 can produce digest-only frames, but
`IsError` rejects those frames and `Decode` returns an empty sequence.

The existing `IEnumerable<byte>` APIs require inputs that support repeated
enumeration. Materialize a one-shot iterator before calling those APIs, or use
the new streaming methods and accumulators for single-pass input.

CRC16 and CRC32 digest bytes use the current machine's native byte order.
For example, `new CRC16(CRC16Type.Classic).Compute(data)` produces `3D BB` on a
little-endian machine and `BB 3D` on a big-endian machine. A protocol that specifies
a fixed byte order needs explicit serialization for that protocol.
