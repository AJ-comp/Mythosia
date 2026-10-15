# Mythosia.Integrity

CRC, checksum and non-cryptographic xxHash calculation, frame encoding and error
detection for byte sequences. Cryptographic hash algorithms are provided by
`Mythosia.Security.Cryptography`.

Version `1.3.0` adds CRC16 CCITT-FALSE/X25, CRC32C, CRC64/NVME and incremental
and streaming CRC calculation while preserving the existing public API and
results. See the
[release notes](https://github.com/AJ-comp/Mythosia/blob/master/Mythosia.Integrity/RELEASE_NOTES.md).

Sections marked `(unreleased)` describe source additions not included in the
published 1.3.0 package. Use a source build for those APIs until a package
containing them is released.

Each C# block below is a complete, independent example for a .NET 8 console
application referencing `Mythosia.Integrity`. Copy one block into `Program.cs`.
CRC and xxHash examples use the ASCII bytes of `123456789` unless stated
otherwise. Fletcher4 and OSPF examples supply the required binary input.

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

## CRC16 / CCITT-FALSE and X25

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

## CRC32C / Castagnoli

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

## CRC64 / NVME

Select the CRC64 algorithm using `CRC64Type.NVME`, following the existing
`CRC16(type)` and `CRC32(type)` pattern. Version 1.3.0 supports NVME; the
unreleased XZ and ECMA-182 additions are described below. NVME remains the
default for every CRC64 constructor and extension. Numeric results use
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

## Streaming CRC

Version 1.3.0 adds the following APIs. Existing CRC methods and their results
remain unchanged. Both streaming forms return the same numerical
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
`FFFF` for Modbus, CCITTFalse and MCRF4XX, and `0000` for the other variants,
including X25, USB and legacy DNP. Empty CRC32/Mpeg2 returns `FFFFFFFF`;
the other CRC32 variants and CRC32C return `00000000`. CRC64 returns zero.
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

## Adler-32 (unreleased)

Adler-32 is the checksum defined in [RFC 1950](https://www.rfc-editor.org/rfc/rfc1950.html)
for zlib. These APIs are additions for the next release; existing algorithms and
their results remain unchanged. Import `Mythosia.Integrity.Checksum` for the
numeric and frame extensions, `Adler32`, `Adler32Accumulator` and stream methods.

All calculations produce the same numeric value for the same ordered bytes,
regardless of chunk sizes or split positions. Empty input and a reset
accumulator return `00000001`. `GetCurrentValue()` leaves the state unchanged,
so more bytes can be appended afterward. An accumulator retains no input bytes;
use separate instances for independent calculations.

Byte-sequence APIs consume their input once. `Compute` returns four digest bytes
and `WithAdler32` appends those bytes to a copied payload. The library's frame
helpers use native byte order, matching its existing CRC32 frame convention.
For zlib's specified big-endian checksum field, serialize the numeric value
explicitly, for example with `BinaryPrimitives.WriteUInt32BigEndian`.
Frame validation requires a non-empty payload and rejects digest-only frames.

Stream methods read from the current position to EOF using one reusable 64 KiB
buffer, support non-seekable streams and partial reads, and leave the stream
open on success, cancellation and read failure. The asynchronous method accepts
an optional `CancellationToken` and returns no partial value when cancelled.
Null sources throw `ArgumentNullException`; unreadable streams throw
`ArgumentException`. Read failures propagate to the caller.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mythosia.Integrity.Checksum;

byte[] data = Encoding.ASCII.GetBytes("123456789");
var detector = new Adler32();
Console.WriteLine($"Adler-32: {data.Adler32():X8}");

byte[] frame = data.WithAdler32().ToArray();
Console.WriteLine($"Adler-32 frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"Adler-32 decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new Adler32Accumulator();
accumulator.Append(data.AsSpan(0, 4));
accumulator.Append(data.AsSpan(4));
Console.WriteLine($"Chunked Adler-32: {accumulator.GetCurrentValue():X8}");
accumulator.Reset();
Console.WriteLine($"Reset Adler-32: {accumulator.GetCurrentValue():X8}");

using var stream = new MemoryStream(data);
Console.WriteLine($"Stream Adler-32: {stream.ComputeAdler32():X8}");
using var asyncStream = new MemoryStream(data);
Console.WriteLine($"Async Adler-32: {await asyncStream.ComputeAdler32Async():X8}");
// Numeric, chunked and stream values: 091E01DE. Reset: 00000001.
```

## CRC64 / XZ and ECMA-182 (unreleased)

Select `CRC64Type.ECMA182` or `CRC64Type.XZ` with the existing CRC64 APIs,
including their accumulator and synchronous/asynchronous stream methods.
`CRC64Type.NVME = 0` and every existing CRC64 default remain unchanged;
the additions are `ECMA182 = 1` and `XZ = 2`.

These two algorithms use polynomial `0x42F0E1EBA9EA3693`, with different
initialization, reflection and final XOR settings:

| Type | Reflect input/output | Initial state / Final XOR | `123456789` | Empty input |
| --- | --- | --- | --- | --- |
| `ECMA182` | No | `0000000000000000` | `6C40DF5F0B497347` | `0000000000000000` |
| `XZ` | Yes | `FFFFFFFFFFFFFFFF` | `995DC9BBDF1939FA` | `0000000000000000` |

XZ uses its reflected variant, as specified by the
[XZ file format](https://tukaani.org/xz/xz-file-format.txt).
Both algorithms and check values are listed in the
[CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/17plus.htm).
The CRC64 numeric result remains `ulong`; digest and frame methods keep the
existing eight-byte native-endian convention. Serialize the numeric value
explicitly for a file format or protocol's prescribed byte order.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
foreach (CRC64Type type in new[] { CRC64Type.ECMA182, CRC64Type.XZ })
{
    var detector = new CRC64(type);
    Console.WriteLine($"CRC64 {type}: {data.CRC64(type):X16}");
    Console.WriteLine($"CRC64 {type} digest: {Convert.ToHexString(detector.Compute(data).ToArray())}");
    byte[] frame = data.WithCRC64(type).ToArray();
    Console.WriteLine($"CRC64 {type} frame valid: {!detector.IsError(frame)}");
    Console.WriteLine($"CRC64 {type} decoded: {detector.Decode(frame).SequenceEqual(data)}");

    var accumulator = new CRC64Accumulator(type);
    accumulator.Append(data.AsSpan(0, 4));
    accumulator.Append(data.AsSpan(4));
    Console.WriteLine($"Chunked CRC64 {type}: {accumulator.GetCurrentValue():X16}");
    accumulator.Reset();
    Console.WriteLine($"Reset CRC64 {type}: {accumulator.GetCurrentValue():X16}");

    using var syncStream = new MemoryStream(data, writable: false);
    using var asyncStream = new MemoryStream(data, writable: false);
    Console.WriteLine($"Stream CRC64 {type}: {syncStream.ComputeCRC64(type):X16}");
    Console.WriteLine($"Async stream CRC64 {type}: {await asyncStream.ComputeCRC64Async(type):X16}");
}
// ECMA182 values are 6C40DF5F0B497347; XZ values are 995DC9BBDF1939FA.
// Reset returns zero. Frame validity and decoded-payload checks print True.
```

## Internet Checksum (unreleased)

`InternetChecksum`, its numeric/frame extensions, `InternetChecksumAccumulator`
and stream methods are in `Mythosia.Integrity.Checksum`. The numeric result is
`ushort`, calculated as the one's complement of the one's-complement sum of
big-endian 16-bit words, as defined in
[RFC 1071](https://www.rfc-editor.org/rfc/rfc1071.html).

An odd final byte is treated as the high byte of a word with a zero low byte.
An accumulator retains that pending byte across chunks; `GetCurrentValue()`
applies padding only to the reported result and leaves the state unchanged.
Empty input and `Reset` return `FFFF`. A calculated `0000` remains `0000`.

`Compute` returns two digest bytes in native byte order, and `WithInternetChecksum`
appends them to a copied payload. `IsError`/`Decode` compare the payload's checksum
with that trailing digest and require a non-empty payload. This library framing
is separate from a network protocol's checksum field. For an actual IP, TCP or
UDP packet, prepare the exact required input, zero the checksum field, add any
required pseudoheader, and write the numeric result explicitly in big-endian
order, for example with `BinaryPrimitives.WriteUInt16BigEndian`. Apply the UDP
zero-checksum convention when the relevant protocol requires it.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.Checksum;

byte[] data = Encoding.ASCII.GetBytes("123456789");
var detector = new InternetChecksum();
Console.WriteLine($"Internet Checksum: {data.InternetChecksum():X4}");
Console.WriteLine($"Internet Checksum digest: {Convert.ToHexString(detector.Compute(data).ToArray())}");
byte[] frame = data.WithInternetChecksum().ToArray();
Console.WriteLine($"Internet Checksum frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"Internet Checksum decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new InternetChecksumAccumulator();
accumulator.Append(data.AsSpan(0, 4));
accumulator.Append(data.AsSpan(4));
Console.WriteLine($"Chunked Internet Checksum: {accumulator.GetCurrentValue():X4}");
accumulator.Reset();
Console.WriteLine($"Reset Internet Checksum: {accumulator.GetCurrentValue():X4}");

using var syncStream = new MemoryStream(data, writable: false);
using var asyncStream = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream Internet Checksum: {syncStream.ComputeInternetChecksum():X4}");
Console.WriteLine($"Async Internet Checksum: {await asyncStream.ComputeInternetChecksumAsync():X4}");
// Numeric, chunked and stream values: F62A. Reset: FFFF.
```

Byte-sequence APIs consume input once. Accumulators retain only calculation
state. Both stream methods start at the current position, read to EOF with one
reusable 64 KiB buffer and leave the stream open. The asynchronous method accepts
an optional `CancellationToken`; cancellation or read failure returns no partial
value. Read failures propagate. Null inputs throw `ArgumentNullException`, and
unreadable streams throw `ArgumentException`.

## CRC8 / SAE-J1850 and AUTOSAR H2F (unreleased)

Select `CRC8Type.SaeJ1850` or `CRC8Type.AutosarH2F` in the existing CRC8 detector,
numeric/frame extensions, accumulator and synchronous/asynchronous stream APIs.
The default remains `Classic`; existing enum values and calculation results are
unchanged. Both new algorithms return one CRC byte, with these distinct parameters:

| Parameter | SAE-J1850 | AUTOSAR H2F |
| --- | --- | --- |
| Polynomial | `1D` | `2F` |
| Initial register | `FF` | `FF` |
| Reflect input/output | No / No | No / No |
| Final XOR | `FF` | `FF` |
| ASCII `123456789` | `4B` | `DF` |

These models are specified by the
[AUTOSAR CRC Library](https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf).
The library supplies the parameters; callers select the algorithm and provide
the exact bytes required by their protocol. An AUTOSAR end-to-end profile may
also require fields such as a Data ID, counter and specific checksum placement;
prepare that input according to the profile before calculating the CRC.

`GetCurrentValue()` applies the final XOR to the reported value without changing
the accumulator, so reading a prefix does not prevent further appends. `Reset()`
restores the selected algorithm's initial state. CRC8's empty-input contract is
preserved: `Compute` returns an empty digest, numeric/frame encoding extensions
throw `ArgumentOutOfRangeException`, and a new/reset accumulator or an empty
stream has no numeric result and throws `InvalidOperationException`.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
foreach (CRC8Type type in new[] { CRC8Type.SaeJ1850, CRC8Type.AutosarH2F })
{
    var detector = new CRC8(type);
    Console.WriteLine($"CRC8 {type}: {data.CRC8(type):X2}");
    Console.WriteLine($"CRC8 {type} digest: {Convert.ToHexString(detector.Compute(data).ToArray())}");
    byte[] frame = data.WithCRC8(type).ToArray();
    Console.WriteLine($"CRC8 {type} frame valid: {!detector.IsError(frame)}");
    Console.WriteLine($"CRC8 {type} decoded: {detector.Decode(frame).SequenceEqual(data)}");

    var accumulator = new CRC8Accumulator(type);
    accumulator.Append(data.AsSpan(0, 4));
    accumulator.Append(data.AsSpan(4));
    Console.WriteLine($"Chunked CRC8 {type}: {accumulator.GetCurrentValue():X2}");
    accumulator.Reset();
    accumulator.Append(data);
    Console.WriteLine($"Reused CRC8 {type}: {accumulator.GetCurrentValue():X2}");

    using var syncStream = new MemoryStream(data, writable: false);
    using var asyncStream = new MemoryStream(data, writable: false);
    Console.WriteLine($"Stream CRC8 {type}: {syncStream.ComputeCRC8(type):X2}");
    Console.WriteLine($"Async stream CRC8 {type}: {await asyncStream.ComputeCRC8Async(type):X2}");
}
// SAE-J1850 values are 4B; AUTOSAR H2F values are DF.
// Frame validity and decoded-payload checks print True.
```

Both stream methods start at the current position, use one reusable 64 KiB
buffer and leave the stream open. The asynchronous method accepts an optional
`CancellationToken`. Cancellation and read failure return no partial CRC.

## CRC8 / Sensirion SHT4x (unreleased)

Available in source builds. `CRC8Type.SensirionSht4x` is an additive selector in
all existing CRC8 detector, numeric/frame, accumulator and stream APIs. The
`Classic` default and existing enum values and results remain unchanged.

The [Sensirion SHT4x datasheet](https://sensirion.com/resource/datasheet/sht4x),
v7.3 (June 2026), section 4.4, table 7, specifies these parameters. This model is
also catalogued as
[CRC-8/NRSC-5](https://reveng.sourceforge.io/crc-catalogue/all.htm#crc.cat.crc-8-nrsc-5).

| Parameter | Value |
| --- | --- |
| Polynomial | `31` |
| Initial register | `FF` |
| Reflect input/output | No / No |
| Final XOR | `00` |
| ASCII `123456789` | `F7` |
| Sensor bytes `BE EF` | `92` |

SHT4x checks each 16-bit sensor word separately: two data bytes followed by one
CRC byte. Supply those two data bytes in their received order when validating
that word. The generic CRC API also accepts other message lengths, as the
standard check string below illustrates.

CRC8 retains its existing empty-input contract: `Compute` returns an empty
digest; numeric/frame encoding throws `ArgumentOutOfRangeException`; a new or
reset accumulator and an empty stream throw `InvalidOperationException` when
asked for a numeric result. `Reset()` keeps the selected model and restores its
initial register. Reading a nonempty prefix never changes the accumulator.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
CRC8Type type = CRC8Type.SensirionSht4x;
var detector = new CRC8(type);
Console.WriteLine($"CRC8 {type}: {data.CRC8(type):X2}");
byte[] frame = data.WithCRC8(type).ToArray();
Console.WriteLine($"CRC8 {type} frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"CRC8 {type} decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new CRC8Accumulator(type);
accumulator.Append(data.AsSpan(0, 4));
accumulator.Append(data.AsSpan(4));
Console.WriteLine($"Chunked CRC8 {type}: {accumulator.GetCurrentValue():X2}");
accumulator.Reset();
accumulator.Append(data);
Console.WriteLine($"Reused CRC8 {type}: {accumulator.GetCurrentValue():X2}");

using var syncStream = new MemoryStream(data, writable: false);
using var asyncStream = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream CRC8 {type}: {syncStream.ComputeCRC8(type):X2}");
Console.WriteLine($"Async stream CRC8 {type}: {await asyncStream.ComputeCRC8Async(type):X2}");
byte[] sensorWord = { 0xBE, 0xEF };
Console.WriteLine($"SHT4x BE EF: {sensorWord.CRC8(type):X2}");
// General check: F7. Manufacturer's two-byte example: 92.
```

## CRC16 / MCRF4XX and USB (unreleased)

Available in source builds. Select `CRC16Type.MCRF4XX` or `CRC16Type.USB` in the
existing CRC16 APIs. Existing enum values and defaults remain unchanged:
detectors, accumulators and streams default to `Classic`, and numeric/frame
extensions retain their legacy `Basic` default, which calculates the same CRC.

| Parameter | MCRF4XX | USB |
| --- | --- | --- |
| Polynomial | `1021` | `8005` |
| Initial register | `FFFF` | `FFFF` |
| Reflect input/output | Yes / Yes | Yes / Yes |
| Final XOR | `0000` | `FFFF` |
| ASCII `123456789` | `6F91` | `B4C8` |
| Empty input / reset | `FFFF` | `0000` |

The exact models and checks are listed in the
[CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/16.htm).
[USB-IF's CRC explanation](https://www.usb.org/sites/default/files/crcdes.pdf)
describes USB data-packet CRC16.
[MAVLink's serialization guide](https://mavlink.io/en/guide/serialization.html#checksum)
uses the MCRF4XX calculation with the required packet fields and message-specific
`CRC_EXTRA` byte. Prepare that exact input before calling the raw CRC API.
For USB data packets, use the data field bytes before bit stuffing.

Both results are `ushort`. `Compute` and frame helpers preserve the library's
two-byte native-endian digest convention. A protocol's packet fields, byte order,
bit transmission order and checksum placement must be serialized according to
that protocol. `IsError` and `Decode` require a nonempty payload followed by the
library digest; a digest-only frame is invalid.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
foreach (CRC16Type type in new[] { CRC16Type.MCRF4XX, CRC16Type.USB })
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
    accumulator.Reset();
    Console.WriteLine($"Reset CRC16 {type}: {accumulator.GetCurrentValue():X4}");

    using var syncStream = new MemoryStream(data, writable: false);
    using var asyncStream = new MemoryStream(data, writable: false);
    Console.WriteLine($"Stream CRC16 {type}: {syncStream.ComputeCRC16(type):X4}");
    Console.WriteLine($"Async stream CRC16 {type}: {await asyncStream.ComputeCRC16Async(type):X4}");
}
// MCRF4XX: 6F91, reset FFFF. USB: B4C8, reset 0000.
```

## CRC32 / MPEG-2, BZIP2 and AUTOSAR P4 (unreleased)

Available in source builds. Choose `CRC32Type.Mpeg2`, `CRC32Type.Bzip2` or
`CRC32Type.AutosarP4` in the existing CRC32 APIs. Existing enum values and
calculation results remain unchanged. Detectors, accumulators and streams
continue to default to `Classic`; numeric/frame extensions retain the equivalent
legacy `Basic` default.

| Parameter | MPEG-2 | BZIP2 | AUTOSAR P4 |
| --- | --- | --- | --- |
| Polynomial | `04C11DB7` | `04C11DB7` | `F4ACFB13` |
| Initial register | `FFFFFFFF` | `FFFFFFFF` | `FFFFFFFF` |
| Reflect input/output | No / No | No / No | Yes / Yes |
| Final XOR | `00000000` | `FFFFFFFF` | `FFFFFFFF` |
| ASCII `123456789` | `0376E6E7` | `FC891918` | `1697D06A` |
| Empty input / reset | `FFFFFFFF` | `00000000` | `00000000` |

The exact models and checks are listed in the
[CRC RevEng catalogue](https://reveng.sourceforge.io/crc-catalogue/17plus.htm).
[TSDuck documents MPEG section CRC32](https://tsduck.io/docs/tsduck.html#tscrc32),
and the [bzip2 manual](https://www.sourceware.org/bzip2/manual/manual.html)
describes its per-block CRC32 integrity check. The BZIP2 selector calculates a
block CRC; composing a bzip2 container's combined CRC follows the file format.
[AUTOSAR's CRC Library specification](https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf),
section 7.2.3.2, defines CRC32P4. An AUTOSAR end-to-end profile also defines which
fields and Data ID bytes are included; construct that input for the profile.

Numeric results are `uint`; the existing digest/frame convention is four bytes
in native byte order. Serialize numeric results explicitly for a file format or
protocol that requires a specific byte order. `IsError` and `Decode` require a
nonempty payload and its trailing digest. Accumulator snapshots apply the final
XOR without modifying the calculation state, and `Reset()` restores the
selected model's empty value shown above.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.CRC;

byte[] data = Encoding.ASCII.GetBytes("123456789");
foreach (CRC32Type type in new[] { CRC32Type.Mpeg2, CRC32Type.Bzip2, CRC32Type.AutosarP4 })
{
    var detector = new CRC32(type);
    Console.WriteLine($"CRC32 {type}: {data.CRC32(type):X8}");
    byte[] frame = data.WithCRC32(type).ToArray();
    Console.WriteLine($"CRC32 {type} frame valid: {!detector.IsError(frame)}");
    Console.WriteLine($"CRC32 {type} decoded: {detector.Decode(frame).SequenceEqual(data)}");

    var accumulator = new CRC32Accumulator(type);
    accumulator.Append(data.AsSpan(0, 4));
    accumulator.Append(data.AsSpan(4));
    Console.WriteLine($"Chunked CRC32 {type}: {accumulator.GetCurrentValue():X8}");
    accumulator.Reset();
    Console.WriteLine($"Reset CRC32 {type}: {accumulator.GetCurrentValue():X8}");

    using var syncStream = new MemoryStream(data, writable: false);
    using var asyncStream = new MemoryStream(data, writable: false);
    Console.WriteLine($"Stream CRC32 {type}: {syncStream.ComputeCRC32(type):X8}");
    Console.WriteLine($"Async stream CRC32 {type}: {await asyncStream.ComputeCRC32Async(type):X8}");
}
// MPEG-2: 0376E6E7. BZIP2: FC891918. AUTOSAR P4: 1697D06A.
```

For all three sections above, streaming starts at the current position, consumes
to EOF with one reusable 64 KiB buffer and leaves the source open. Asynchronous
methods accept an optional `CancellationToken`. Cancellation and read failure
return no partial CRC.

## xxHash32 and xxHash64 (unreleased)

These APIs are available in source builds and have not been published as a new
NuGet version. `XxHash32` and `XxHash64` implement the original XXH32 and XXH64
algorithms described by the [xxHash specification](https://github.com/Cyan4973/xxHash/blob/v0.8.3/doc/xxhash_spec.md).
These are non-cryptographic hashes, suitable for checksums and hash keys. They
do not provide authentication.

Numeric extensions return `uint` and `ulong`. The optional seed defaults to zero
across the detector, accumulator, numeric/frame extensions and stream methods.
For example, use `data.XxHash64(seed: 42)` and `new XxHash64(seed: 42)` together
when that seed is required. `Seed` records the selected value, and `Reset()`
retains it. A result snapshot leaves the accumulator available for more input.

`Compute` and the trailing digest added by `WithXxHash32`/`WithXxHash64` use native
byte order, following the library's framing convention. The xxHash canonical
representation is big-endian; serialize the numeric value explicitly for that
representation. Frame validation requires a nonempty payload. Empty calculations
are valid and, with seed zero, produce `02CC5D05` and `EF46DB3751D8E999`.

Stream methods start at the current position, reuse one 64 KiB buffer and leave
the stream open. Asynchronous methods accept a cancellation token following the
seed parameter. Cancellation or read failure returns no partial hash.
Accumulators keep calculation state and a 16-byte or 32-byte holdback.

```csharp
using System;
using System.IO;
using System.Linq;
using System.Text;
using Mythosia.Integrity.Checksum;

byte[] data = Encoding.ASCII.GetBytes("123456789");
var detector32 = new XxHash32(seed: 0);
Console.WriteLine($"XXH32: {data.XxHash32():X8}");
Console.WriteLine($"XXH32 digest: {Convert.ToHexString(detector32.Compute(data).ToArray())}");
byte[] frame32 = data.WithXxHash32().ToArray();
Console.WriteLine($"XXH32 frame valid: {!detector32.IsError(frame32)}");
Console.WriteLine($"XXH32 decoded: {detector32.Decode(frame32).SequenceEqual(data)}");
var accumulator32 = new XxHash32Accumulator(seed: 0);
accumulator32.Append(data.AsSpan(0, 4));
accumulator32.Append(data.AsSpan(4));
Console.WriteLine($"Chunked XXH32: {accumulator32.GetCurrentValue():X8}");
accumulator32.Reset();
Console.WriteLine($"Reset XXH32: {accumulator32.GetCurrentValue():X8}");
using var sync32 = new MemoryStream(data, writable: false);
using var async32 = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream XXH32: {sync32.ComputeXxHash32():X8}");
Console.WriteLine($"Async XXH32: {await async32.ComputeXxHash32Async():X8}");

var detector64 = new XxHash64(seed: 0);
Console.WriteLine($"XXH64: {data.XxHash64():X16}");
Console.WriteLine($"XXH64 digest: {Convert.ToHexString(detector64.Compute(data).ToArray())}");
byte[] frame64 = data.WithXxHash64().ToArray();
Console.WriteLine($"XXH64 frame valid: {!detector64.IsError(frame64)}");
Console.WriteLine($"XXH64 decoded: {detector64.Decode(frame64).SequenceEqual(data)}");
var accumulator64 = new XxHash64Accumulator(seed: 0);
accumulator64.Append(data.AsSpan(0, 4));
accumulator64.Append(data.AsSpan(4));
Console.WriteLine($"Chunked XXH64: {accumulator64.GetCurrentValue():X16}");
accumulator64.Reset();
Console.WriteLine($"Reset XXH64: {accumulator64.GetCurrentValue():X16}");
using var sync64 = new MemoryStream(data, writable: false);
using var async64 = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream XXH64: {sync64.ComputeXxHash64():X16}");
Console.WriteLine($"Async XXH64: {await async64.ComputeXxHash64Async():X16}");
// XXH32: 937BAD67; XXH64: 8CB841DB40E6AE83.
```

## OpenZFS Fletcher-4 (unreleased)

This source-build feature follows the [OpenZFS Fletcher-4 algorithm](https://github.com/openzfs/zfs/blob/34f96a15c73eab27dd6ad17bb5f1263bf26e37d7/module/zcommon/zfs_fletcher.c).
It reads 32-bit input words and returns `Fletcher4Value`, containing the four
wrapping 64-bit sums `A`, `B`, `C` and `D`. It has not yet been published as a
new NuGet version.

Choose the input word byte order explicitly with `Fletcher4ByteOrder.LittleEndian`
or `BigEndian`; the default is `LittleEndian` on every platform. This choice
controls how four input bytes form one word. It is independent of digest byte
order: `Compute` and `WithFletcher4` serialize A/B/C/D as four native-endian
64-bit values, for a 32-byte digest. This is the library's trailing-digest frame
format; an OpenZFS block pointer or send-stream record has its own format.

Each complete calculation must contain a multiple of four bytes. An accumulator
retains up to three pending bytes across chunks; it never pads incomplete words.
`GetCurrentValue()` throws `InvalidOperationException` while a partial word is
pending and preserves that state so more bytes can be appended. Byte-sequence
calculation/encoding throws `ArgumentException` for an unaligned final length;
stream calculation throws `InvalidDataException` if EOF leaves a partial word.
A valid encoded frame requires an aligned, nonempty payload. Empty aligned
calculation and `Reset()` produce four zeros.

Streams start at the current position, reuse one 64 KiB buffer and leave the
source open. The asynchronous method accepts a cancellation token after the
byte-order parameter; cancellation and read failures return no partial value.

```csharp
using System;
using System.Buffers.Binary;
using System.IO;
using System.Linq;
using Mythosia.Integrity.Checksum;

// Four little-endian uint32 input words: 1, 2, 3, 4.
byte[] data = Convert.FromHexString("01000000020000000300000004000000");
var detector = new Fletcher4(Fletcher4ByteOrder.LittleEndian);
Fletcher4Value value = data.Fletcher4(Fletcher4ByteOrder.LittleEndian);
Console.WriteLine($"Fletcher4 A/B/C/D: {value.A}/{value.B}/{value.C}/{value.D}");
Console.WriteLine($"Fletcher4 digest: {Convert.ToHexString(detector.Compute(data).ToArray())}");
byte[] frame = data.WithFletcher4(Fletcher4ByteOrder.LittleEndian).ToArray();
Console.WriteLine($"Fletcher4 frame valid: {!detector.IsError(frame)}");
Console.WriteLine($"Fletcher4 decoded: {detector.Decode(frame).SequenceEqual(data)}");

var accumulator = new Fletcher4Accumulator(Fletcher4ByteOrder.LittleEndian);
accumulator.Append(data.AsSpan(0, 3)); // Keep an incomplete word until the next append.
accumulator.Append(data.AsSpan(3));
Console.WriteLine($"Chunked Fletcher4 matches: {accumulator.GetCurrentValue() == value}");
accumulator.Reset();
Fletcher4Value empty = accumulator.GetCurrentValue();
Console.WriteLine($"Reset Fletcher4 A/B/C/D: {empty.A}/{empty.B}/{empty.C}/{empty.D}");

using var syncStream = new MemoryStream(data, writable: false);
using var asyncStream = new MemoryStream(data, writable: false);
Console.WriteLine($"Stream Fletcher4 matches: {syncStream.ComputeFletcher4(Fletcher4ByteOrder.LittleEndian) == value}");
Fletcher4Value asyncValue = await asyncStream.ComputeFletcher4Async(Fletcher4ByteOrder.LittleEndian);
Console.WriteLine($"Async Fletcher4 matches: {asyncValue == value}");

// The same numeric words in a big-endian input format.
var bigEndianData = new byte[16];
for (int index = 0; index < 4; index++)
    BinaryPrimitives.WriteUInt32BigEndian(bigEndianData.AsSpan(index * 4, 4), (uint)(index + 1));
Console.WriteLine($"Big-endian Fletcher4 matches: {bigEndianData.Fletcher4(Fletcher4ByteOrder.BigEndian) == value}");
// A=10, B=20, C=35, D=56. All matching/validity checks print True.
```

## OSPF LSA checksum (unreleased)

`OspfLsaChecksum` is available in source builds and has not yet been published
as a new NuGet version. It calculates and validates the LSA Fletcher checksum
specified by [RFC 2328 section 12.1.7](https://www.rfc-editor.org/rfc/rfc2328.html#section-12.1.7)
and [appendix B](https://www.rfc-editor.org/rfc/rfc2328.html#appendix-B).

Supply exactly one complete OSPFv2 or OSPFv3 LSA, including its 20-byte header and
all bytes declared by the length field at offsets 18 and 19. The helper checks
that the supplied and declared lengths agree; it does not parse LSA body
semantics. Supplying an entire OSPF packet or a truncated LSA is invalid input.

`Compute` returns the `ushort` checksum to put in the LSA's field, ignoring the
field's existing contents. `Write` replaces only bytes 16 and 17 in network
(big-endian) byte order. `IsValid` checks the existing field and checksum
residues and returns false for malformed input or an invalid checksum. The first
two LS age bytes are excluded, so changing only age preserves validity. This
helper updates the embedded protocol field rather than appending a library digest.

The example uses the first fixed public LSA vector from
[tcpdump's capture corpus](https://github.com/the-tcpdump-group/tcpdump/blob/3a2f6acd865e5b845d9f63caf5f2c23cb73a2bc9/tests/ospf-nssa-bitnt.pcap),
whose published checksum field is `51CB`.

```csharp
using System;
using Mythosia.Integrity.Checksum;

// One complete 48-byte LSA from tcpdump's published ospf-nssa-bitnt.pcap.
byte[] lsa = Convert.FromHexString(
    "000128010A0022030A0022038000000451CB0030130000020A0022040A002203010000010A002200FFFFFF0003000001");
lsa[16] = 0;
lsa[17] = 0;
Console.WriteLine($"OSPF LSA length: {lsa.Length}");
Console.WriteLine($"OSPF LSA checksum: {OspfLsaChecksum.Compute(lsa):X4}");
OspfLsaChecksum.Write(lsa);
Console.WriteLine($"OSPF LSA checksum field: {Convert.ToHexString(lsa.AsSpan(16, 2))}");
Console.WriteLine($"OSPF LSA valid: {OspfLsaChecksum.IsValid(lsa)}");

// The checksum excludes the two-byte LS age field.
lsa[0] = 0x00;
lsa[1] = 0x64;
Console.WriteLine($"OSPF LSA valid after age change: {OspfLsaChecksum.IsValid(lsa)}");
lsa[lsa.Length - 1] ^= 0x01;
Console.WriteLine($"OSPF LSA valid after body change: {OspfLsaChecksum.IsValid(lsa)}");
// Numeric checksum: 51CB. The checksum field contains network-order bytes 51 CB.
```
