using System.Collections.Concurrent;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.Checksum;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public class IntegrityFrameTests
{
    public enum Algorithm
    {
        ChecksumXor,
        ChecksumNmea,
        ChecksumModulo256,
        ChecksumTwosComplement,
        Crc8Classic,
        Crc8Maxim,
        Crc16Classic,
        Crc16Modbus,
        Crc16XModem,
        Crc16DnpLegacy,
        Crc32Classic
    }

    public enum SourceKind
    {
        Array,
        List,
        YieldIterator,
        FilterIterator
    }

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (var algorithm in Enum.GetValues<Algorithm>())
            yield return new object[] { algorithm };
    }

    public static IEnumerable<object[]> PayloadsAndSourceKinds()
    {
        foreach (var algorithm in Enum.GetValues<Algorithm>())
        foreach (var (name, payload) in Payloads())
        foreach (var sourceKind in Enum.GetValues<SourceKind>())
            yield return new object[] { algorithm, name, payload, sourceKind };
    }

    public static IEnumerable<object[]> SingleBitPayloads()
    {
        foreach (var algorithm in Enum.GetValues<Algorithm>())
        {
            yield return new object[] { algorithm, "minimum", new byte[] { 0x80 } };
            yield return new object[] { algorithm, "check-string", Encoding.ASCII.GetBytes("123456789") };
            yield return new object[] { algorithm, "binary", BinaryPayload() };
        }
    }

    [Theory]
    [MemberData(nameof(PayloadsAndSourceKinds))]
    public void FrameOperations_MatchIndependentSuffixAndPreserveSource(
        Algorithm algorithm, string payloadName, byte[] payload, SourceKind sourceKind)
    {
        var detector = CreateDetector(algorithm);
        var source = CreateSource(payload, sourceKind);
        var sourceSnapshot = source.ToArray();
        var suffix = ReferenceSuffix(algorithm, payload);
        var expectedFrame = payload.Concat(suffix).ToArray();
        var context = $"{algorithm}, {payloadName}, {sourceKind}";

        AssertBytesEqual(suffix, detector.Compute(source), $"{context}: Compute");
        AssertBytesEqual(expectedFrame, detector.Encode(source), $"{context}: Encode");
        AssertBytesEqual(expectedFrame, EncodeViaExtension(algorithm, source), $"{context}: With extension");

        // Feed an independently assembled frame so an encoder/decoder bug cannot cancel itself out.
        var frameSource = CreateSource(expectedFrame, sourceKind);
        Assert.False(detector.IsError(frameSource));
        AssertBytesEqual(payload, detector.Decode(frameSource), $"{context}: Decode");
        AssertBytesEqual(sourceSnapshot, source, $"{context}: source changed");
        AssertBytesEqual(expectedFrame, frameSource, $"{context}: frame changed");
    }

    [Theory]
    [MemberData(nameof(SingleBitPayloads))]
    public void IsErrorAndDecode_RejectEverySingleBitFlipInPayloadAndSuffix(
        Algorithm algorithm, string payloadName, byte[] payload)
    {
        var detector = CreateDetector(algorithm);
        var frame = payload.Concat(ReferenceSuffix(algorithm, payload)).ToArray();

        for (var bitIndex = 0; bitIndex < frame.Length * 8; bitIndex++)
        {
            var corrupted = (byte[])frame.Clone();
            corrupted[bitIndex / 8] ^= (byte)(1 << (bitIndex % 8));
            var corruptedSnapshot = (byte[])corrupted.Clone();
            var location = bitIndex / 8 < payload.Length ? "payload" : "suffix";
            var context = $"{algorithm}, {payloadName}, {location} bit {bitIndex}";

            Assert.True(detector.IsError(corrupted), $"{context}: array corruption was accepted");
            Assert.True(detector.IsError(YieldBytes(corrupted)), $"{context}: iterator corruption was accepted");
            Assert.Empty(detector.Decode(corrupted));
            Assert.Empty(detector.Decode(YieldBytes(corrupted)));
            AssertBytesEqual(corruptedSnapshot, corrupted, $"{context}: corrupted frame changed");
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void MultiBitMutations_MatchIndependentRecalculationIncludingPossibleCollisions(Algorithm algorithm)
    {
        var detector = CreateDetector(algorithm);
        var payload = BinaryPayload();
        var frame = payload.Concat(ReferenceSuffix(algorithm, payload)).ToArray();
        var random = new Random(0x435243 + (int)algorithm);

        for (var mutation = 0; mutation < 64; mutation++)
        {
            var corrupted = (byte[])frame.Clone();
            var availableBits = Enumerable.Range(0, frame.Length * 8).ToList();
            var bitCount = 2 + mutation % 7;
            for (var index = 0; index < bitCount; index++)
            {
                var position = random.Next(availableBits.Count);
                var bitIndex = availableBits[position];
                availableBits.RemoveAt(position);
                corrupted[bitIndex / 8] ^= (byte)(1 << (bitIndex % 8));
            }

            AssertValidationMatchesReference(detector, algorithm, corrupted, $"mutation {mutation}, {bitCount} bits");
        }
    }

    [Theory]
    [InlineData(Algorithm.ChecksumXor)]
    [InlineData(Algorithm.ChecksumNmea)]
    [InlineData(Algorithm.ChecksumModulo256)]
    [InlineData(Algorithm.ChecksumTwosComplement)]
    public void ChecksumCollision_IsAcceptedWhenDifferentPayloadHasSameChecksum(Algorithm algorithm)
    {
        var original = new byte[] { 0x10, 0x20, 0x30 };
        var modified = (byte[])original.Clone();
        if (algorithm is Algorithm.ChecksumXor or Algorithm.ChecksumNmea)
        {
            modified[0] ^= 1;
            modified[1] ^= 1;
        }
        else
        {
            modified[0]++;
            modified[1]--;
        }

        var originalSuffix = ReferenceSuffix(algorithm, original);
        AssertBytesEqual(originalSuffix, ReferenceSuffix(algorithm, modified), $"{algorithm}: collision fixture");
        var frame = modified.Concat(originalSuffix).ToArray();
        var detector = CreateDetector(algorithm);

        Assert.False(detector.IsError(frame));
        AssertBytesEqual(modified, detector.Decode(frame), $"{algorithm}: colliding payload");
        Assert.False(original.SequenceEqual(modified));
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void TruncatedFrames_MatchIndependentValidationAndRejectAllLengthsAtMostSuffixWidth(Algorithm algorithm)
    {
        var detector = CreateDetector(algorithm);
        var payload = BinaryPayload();
        var frame = payload.Concat(ReferenceSuffix(algorithm, payload)).ToArray();

        for (var length = 0; length < frame.Length; length++)
        {
            var truncated = frame.Take(length).ToArray();
            AssertValidationMatchesReference(detector, algorithm, truncated, $"prefix length {length}");

            if (length <= SuffixWidth(algorithm))
            {
                Assert.True(detector.IsError(truncated), $"{algorithm}: length {length} must have a payload byte");
                Assert.Empty(detector.Decode(truncated));
            }
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void WarmedDetector_ConcurrentSameAndSeparateInstancesRemainDeterministic(Algorithm algorithm)
    {
        // This covers concurrent use of existing tables. First-use initialization
        // is verified separately with isolated assemblies in CrcInitializationTests.
        foreach (var type in Enum.GetValues<Algorithm>())
            CreateDetector(type).Compute(new byte[] { 0x01 }).ToArray();

        var sharedDetector = CreateDetector(algorithm);
        var payloads = Enumerable.Range(0, 32)
            .Select(index => Enumerable.Range(0, 1 + index)
                .Select(offset => (byte)(index * 29 + offset * 73)).ToArray())
            .ToArray();
        var suffixes = payloads.Select(payload => ReferenceSuffix(algorithm, payload)).ToArray();
        var frames = payloads.Select((payload, index) => payload.Concat(suffixes[index]).ToArray()).ToArray();
        var failures = new ConcurrentQueue<string>();

        Parallel.For(0, 128, iteration =>
        {
            var index = iteration % payloads.Length;
            var detector = iteration % 2 == 0 ? sharedDetector : CreateDetector(algorithm);
            if (!detector.Compute(payloads[index]).SequenceEqual(suffixes[index]))
                failures.Enqueue($"iteration {iteration}: Compute");
            if (!detector.Encode(payloads[index]).SequenceEqual(frames[index]))
                failures.Enqueue($"iteration {iteration}: Encode");
            if (detector.IsError(frames[index]))
                failures.Enqueue($"iteration {iteration}: valid frame rejected");
            if (!detector.Decode(frames[index]).SequenceEqual(payloads[index]))
                failures.Enqueue($"iteration {iteration}: Decode");

            var corrupted = (byte[])frames[index].Clone();
            corrupted[^1] ^= 0x80;
            if (!detector.IsError(corrupted) || detector.Decode(corrupted).Any())
                failures.Enqueue($"iteration {iteration}: damaged suffix accepted");
        });

        Assert.True(failures.IsEmpty, $"{algorithm}: {string.Join("; ", failures)}");
    }

    private static IEnumerable<(string Name, byte[] Payload)> Payloads()
    {
        yield return ("single-zero", new byte[] { 0x00 });
        yield return ("single-high-bit", new byte[] { 0x80 });
        yield return ("check-string", Encoding.ASCII.GetBytes("123456789"));
        yield return ("embedded-zero-and-high-bytes", BinaryPayload());
        yield return ("zero-run", new byte[17]);
        yield return ("maximum-byte-run", Enumerable.Repeat((byte)0xFF, 17).ToArray());
        yield return ("moderate-binary", Enumerable.Range(0, 64).Select(index => (byte)(index * 37)).ToArray());
    }

    private static byte[] BinaryPayload() =>
        new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF, 0x00, 0xA5, 0x5A, 0x10, 0x20, 0x30, 0x00, 0xF0, 0x0F, 0x81, 0x00 };

    private static IEnumerable<byte> CreateSource(byte[] values, SourceKind sourceKind) => sourceKind switch
    {
        SourceKind.Array => (byte[])values.Clone(),
        SourceKind.List => values.ToList(),
        SourceKind.YieldIterator => YieldBytes((byte[])values.Clone()),
        SourceKind.FilterIterator => ((byte[])values.Clone()).Where(_ => true),
        _ => throw new ArgumentOutOfRangeException(nameof(sourceKind))
    };

    private static IEnumerable<byte> YieldBytes(IEnumerable<byte> values)
    {
        foreach (var value in values)
            yield return value;
    }

    private static ErrorDetection CreateDetector(Algorithm algorithm) => algorithm switch
    {
        Algorithm.ChecksumXor => new Checksum8(Checksum8Type.Xor),
        Algorithm.ChecksumNmea => new Checksum8(Checksum8Type.NMEA),
        Algorithm.ChecksumModulo256 => new Checksum8(Checksum8Type.Modulo256),
        Algorithm.ChecksumTwosComplement => new Checksum8(Checksum8Type.TwosComplement),
        Algorithm.Crc8Classic => new CRC8(CRC8Type.Classic),
        Algorithm.Crc8Maxim => new CRC8(CRC8Type.Maxim),
        Algorithm.Crc16Classic => new CRC16(CRC16Type.Classic),
        Algorithm.Crc16Modbus => new CRC16(CRC16Type.Modbus),
        Algorithm.Crc16XModem => new CRC16(CRC16Type.CCITTxModem),
        Algorithm.Crc16DnpLegacy => new CRC16(CRC16Type.DNP),
        Algorithm.Crc32Classic => new CRC32(CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static IEnumerable<byte> EncodeViaExtension(Algorithm algorithm, IEnumerable<byte> source) => algorithm switch
    {
        Algorithm.ChecksumXor => source.WithChecksum8(Checksum8Type.Xor),
        Algorithm.ChecksumNmea => source.WithChecksum8(Checksum8Type.NMEA),
        Algorithm.ChecksumModulo256 => source.WithChecksum8(Checksum8Type.Modulo256),
        Algorithm.ChecksumTwosComplement => source.WithChecksum8(Checksum8Type.TwosComplement),
        Algorithm.Crc8Classic => source.WithCRC8(CRC8Type.Classic),
        Algorithm.Crc8Maxim => source.WithCRC8(CRC8Type.Maxim),
        Algorithm.Crc16Classic => source.WithCRC16(CRC16Type.Classic),
        Algorithm.Crc16Modbus => source.WithCRC16(CRC16Type.Modbus),
        Algorithm.Crc16XModem => source.WithCRC16(CRC16Type.CCITTxModem),
        Algorithm.Crc16DnpLegacy => source.WithCRC16(CRC16Type.DNP),
        Algorithm.Crc32Classic => source.WithCRC32(CRC32Type.Classic),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static byte[] ReferenceSuffix(Algorithm algorithm, byte[] payload) => algorithm switch
    {
        Algorithm.ChecksumXor or Algorithm.ChecksumNmea or
        Algorithm.ChecksumModulo256 or Algorithm.ChecksumTwosComplement =>
            new[] { ReferenceChecksum(algorithm, payload) },
        Algorithm.Crc8Classic => new[] { CrcReference.Compute8(payload, CRC8Type.Classic) },
        Algorithm.Crc8Maxim => new[] { CrcReference.Compute8(payload, CRC8Type.Maxim) },
        Algorithm.Crc16Classic => NativeBytes(CrcReference.Compute16(payload, CRC16Type.Classic), 2),
        Algorithm.Crc16Modbus => NativeBytes(CrcReference.Compute16(payload, CRC16Type.Modbus), 2),
        Algorithm.Crc16XModem => NativeBytes(CrcReference.Compute16(payload, CRC16Type.CCITTxModem), 2),
        // Compatibility fixture: current DNP swaps the standard CRC value before native serialization.
        // This preserves existing behavior and does not certify standard DNP3 wire interoperability.
        Algorithm.Crc16DnpLegacy => NativeBytes(
            CrcReference.ToLegacyDnpRepresentation(CrcReference.Compute16(payload, CRC16Type.DNP)), 2),
        Algorithm.Crc32Classic => NativeBytes(CrcReference.Compute32(payload, CRC32Type.Classic), 4),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static byte ReferenceChecksum(Algorithm algorithm, IEnumerable<byte> payload)
    {
        var sum = 0;
        var xor = 0;
        foreach (var value in payload)
        {
            sum = (sum + value) & 0xFF;
            xor ^= value;
        }

        return algorithm switch
        {
            Algorithm.ChecksumXor or Algorithm.ChecksumNmea => (byte)xor,
            Algorithm.ChecksumModulo256 => (byte)sum,
            Algorithm.ChecksumTwosComplement => (byte)((256 - sum) & 0xFF),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
        };
    }

    private static byte[] NativeBytes(uint value, int width)
    {
        var bytes = new byte[width];
        for (var index = 0; index < width; index++)
        {
            var shift = (BitConverter.IsLittleEndian ? index : width - index - 1) * 8;
            bytes[index] = (byte)(value >> shift);
        }
        return bytes;
    }

    private static int SuffixWidth(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc16Classic or Algorithm.Crc16Modbus or
        Algorithm.Crc16XModem or Algorithm.Crc16DnpLegacy => 2,
        Algorithm.Crc32Classic => 4,
        _ => 1
    };

    private static void AssertValidationMatchesReference(
        ErrorDetection detector, Algorithm algorithm, byte[] frame, string caseName)
    {
        var width = SuffixWidth(algorithm);
        var payloadLength = Math.Max(0, frame.Length - width);
        var payload = frame.Take(payloadLength).ToArray();
        var expectedError = frame.Length <= width ||
            !frame.Skip(payloadLength).SequenceEqual(ReferenceSuffix(algorithm, payload));
        var expectedDecoded = expectedError ? Array.Empty<byte>() : payload;
        var context = $"{algorithm}, {caseName}, frame {Convert.ToHexString(frame)}";
        var snapshot = (byte[])frame.Clone();

        Assert.True(detector.IsError(frame) == expectedError, $"{context}: expected IsError={expectedError}");
        Assert.True(detector.IsError(YieldBytes(frame)) == expectedError, $"{context}: iterator expected IsError={expectedError}");
        AssertBytesEqual(expectedDecoded, detector.Decode(frame), $"{context}: Decode");
        AssertBytesEqual(expectedDecoded, detector.Decode(YieldBytes(frame)), $"{context}: iterator Decode");
        AssertBytesEqual(snapshot, frame, $"{context}: frame changed");
    }

    private static void AssertBytesEqual(byte[] expected, IEnumerable<byte> actual, string context)
    {
        var actualBytes = actual.ToArray();
        Assert.True(expected.SequenceEqual(actualBytes),
            $"{context}: expected {Convert.ToHexString(expected)}, actual {Convert.ToHexString(actualBytes)}");
    }
}
