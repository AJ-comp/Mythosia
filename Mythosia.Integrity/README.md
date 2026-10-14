# Mythosia.Integrity

CRC and 8-bit checksum calculation, frame encoding and error detection for byte
sequences. Hash algorithms are provided by `Mythosia.Security.Cryptography`.

Version `1.2.1` improves initialization safety and calculation efficiency while
preserving the public API and existing results. See the
[release notes](https://github.com/AJ-comp/Mythosia/blob/master/Mythosia.Integrity/RELEASE_NOTES.md).

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

## Input and byte order

Use a non-empty payload. Empty-input behavior differs by algorithm: CRC8 and
Checksum8 return an empty digest, and their numeric extensions and `Encode`
throw for empty inputs. CRC16 and CRC32 can produce digest-only frames, but
`IsError` rejects those frames and `Decode` returns an empty sequence.

Inputs must support repeated enumeration. Materialize a stream or one-shot
iterator into a byte array before calling these APIs.

CRC16 and CRC32 digest bytes use the current machine's native byte order.
For example, `new CRC16(CRC16Type.Classic).Compute(data)` produces `3D BB` on a
little-endian machine and `BB 3D` on a big-endian machine. A protocol that specifies
a fixed byte order needs explicit serialization for that protocol.
