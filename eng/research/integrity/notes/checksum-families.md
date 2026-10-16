> Historical research note, preserved on 2026-10-16. References to "unreleased"
> describe the original development snapshot, not the current NuGet release status.
> For current package behavior and validation, see [the research index](../README.md).

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

This source-build feature follows the [OpenZFS Fletcher-4 algorithm](https://github.com/openzfs/zfs/blob/master/module/zcommon/zfs_fletcher.c).
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
lsa[0] = 0x12;
lsa[1] = 0x34;
Console.WriteLine($"OSPF LSA valid after age change: {OspfLsaChecksum.IsValid(lsa)}");
lsa[lsa.Length - 1] ^= 0x01;
Console.WriteLine($"OSPF LSA valid after body change: {OspfLsaChecksum.IsValid(lsa)}");
// Numeric checksum: 51CB. The checksum field contains network-order bytes 51 CB.
```
