# Mythosia.Integrity

CRC and 8-bit checksum calculation, frame encoding and error detection for byte
sequences. Hash algorithms are provided by `Mythosia.Security.Cryptography`.

Version `1.2.1` improves initialization safety and calculation efficiency while
preserving the public API and existing results. See the
[release notes](https://github.com/AJ-comp/Mythosia/blob/master/Mythosia.Integrity/RELEASE_NOTES.md).

The streaming CRC section describes unreleased source changes. Use a source
build until a package containing these APIs is published.

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

## Streaming CRC (unreleased)

The following APIs are additions for the next release. Existing CRC methods and
their results remain unchanged. Both streaming forms return the same numerical
value as the corresponding existing numeric extension for non-empty input,
when the same algorithm processes the same bytes in the same order. Chunk sizes
and split positions do not affect the final value. CRC16 DNP keeps its legacy
representation.

| CRC | Stream methods | Incremental object | Numeric result |
| --- | --- | --- | --- |
| CRC8 | `ComputeCRC8` / `ComputeCRC8Async` | `CRC8Accumulator` | `byte` |
| CRC16 | `ComputeCRC16` / `ComputeCRC16Async` | `CRC16Accumulator` | `ushort` |
| CRC32 | `ComputeCRC32` / `ComputeCRC32Async` | `CRC32Accumulator` | `uint` |

All new methods and constructors default to `Classic`. They support the existing
algorithm enums and `Basic` aliases. Async methods return `Task<byte>`,
`Task<ushort>` or `Task<uint>`. All APIs are in `Mythosia.Integrity.CRC`.

### Read a stream

`ComputeCRC8`, `ComputeCRC16` and `ComputeCRC32` read from the stream's current
position to its end. Their `Async` counterparts support a named
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
`FFFF` for Modbus and `0000` for the other variants, including legacy DNP; empty
CRC32 returns `00000000`. Unsupported algorithm values throw
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
