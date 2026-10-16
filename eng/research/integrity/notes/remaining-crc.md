> Historical research note, preserved on 2026-10-16. References to "unreleased"
> describe the original development snapshot, not the current NuGet release status.
> For current package behavior and validation, see [the research index](../README.md).

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
