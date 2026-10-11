using System.Collections;
using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcBytePackingTests
{
    public enum Algorithm
    {
        Crc16Basic,
        Crc16Classic,
        Crc16Modbus,
        Crc16Xmodem,
        Crc16DnpLegacy,
        Crc32Basic,
        Crc32Classic
    }

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (Algorithm algorithm in Enum.GetValues<Algorithm>())
            yield return new object[] { algorithm };
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_ReturnsIndependentMutableMaterializedLists(Algorithm algorithm)
    {
        byte[] payload = Encoding.ASCII.GetBytes("123456789");
        byte[] originalPayload = (byte[])payload.Clone();
        byte[] expected = ExpectedDigest(algorithm, originalPayload);
        ErrorDetection detector = CreateDetector(algorithm);

        // A direct byte writer must retain the existing concrete result and
        // calculate eagerly rather than return a shared buffer or an iterator.
        var first = Assert.IsType<List<byte>>(detector.Compute(payload));
        var second = Assert.IsType<List<byte>>(detector.Compute(payload));
        Assert.NotSame(first, second);
        Array.Fill(payload, (byte)0xFF);
        Assert.Equal(expected, first.ToArray());
        Assert.Equal(expected, second.ToArray());

        first[0] ^= 0xFF;
        first.Add(0xA5);
        Assert.Equal(expected, second.ToArray());
        Assert.Equal(expected, detector.Compute(originalPayload).ToArray());
        Assert.Equal(expected, second.ToArray());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_NativePackingPreservesListWidthAcrossBinaryLengthBoundaries(Algorithm algorithm)
    {
        // Full numerical coverage already lives in CrcAlgorithmTests. This
        // smaller corpus verifies materialization and native packing together.
        int[] lengths = { 0, 1, 2, 15, 16, 17, 255, 256, 257, 1023, 1024, 1025, 4095, 4096, 4097 };
        ErrorDetection detector = CreateDetector(algorithm);
        foreach (int length in lengths)
        {
            byte[] payload = DeterministicPayload(length);
            byte[] snapshot = (byte[])payload.Clone();
            byte[] expected = ExpectedDigest(algorithm, snapshot);

            var arrayDigest = Assert.IsType<List<byte>>(detector.Compute(payload));
            var listDigest = Assert.IsType<List<byte>>(detector.Compute(payload.ToList()));
            var iteratorDigest = Assert.IsType<List<byte>>(detector.Compute(YieldBytes(payload)));
            string context = $"{algorithm}, length {length}";

            AssertDigest(expected, arrayDigest, context + ", array");
            AssertDigest(expected, listDigest, context + ", list");
            AssertDigest(expected, iteratorDigest, context + ", iterator");
            Assert.Equal(snapshot, payload);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_EmptyInputRetainsLegacyValueAndMutableList(Algorithm algorithm)
    {
        byte[] empty = Array.Empty<byte>();
        ErrorDetection detector = CreateDetector(algorithm);
        byte[] expected = NativeBytes(LegacyEmptyValue(algorithm), Width(algorithm));

        var digest = Assert.IsType<List<byte>>(detector.Compute(empty));
        var nextDigest = Assert.IsType<List<byte>>(detector.Compute(empty));
        Assert.NotSame(digest, nextDigest);
        Assert.Equal(expected, digest.ToArray());
        Assert.Equal(expected, nextDigest.ToArray());
        Assert.Equal(LegacyEmptyValue(algorithm), NumericDigest(algorithm, empty));

        digest.Clear();
        digest.Add(0xA5);
        Assert.Equal(expected, nextDigest.ToArray());
        Assert.Equal(expected, detector.Compute(empty).ToArray());
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void Compute_NullInputRetainsEagerExceptionBeforeAnyDigestCanBeReturned(Algorithm algorithm)
    {
        ErrorDetection detector = CreateDetector(algorithm);
        // There is deliberately no ToArray here: Compute itself must fail.
        if (algorithm == Algorithm.Crc16Modbus)
            Assert.Throws<NullReferenceException>(() => detector.Compute(null!));
        else
        {
            var failure = Assert.Throws<ArgumentNullException>(() => detector.Compute(null!));
            Assert.Equal("source", failure.ParamName);
        }
    }

    [Theory]
    [InlineData(16, -1)]
    [InlineData(16, int.MaxValue)]
    [InlineData(32, -1)]
    [InlineData(32, int.MaxValue)]
    public void Compute_InvalidTypeRetainsFreshEmptyListAndNeverReadsSource(int bits, int variant)
    {
        ErrorDetection detector = bits == 16
            ? new CRC16((CRC16Type)variant)
            : new CRC32((CRC32Type)variant);
        var source = new ForbiddenSource();

        // An unsupported algorithm has always bypassed calculation, even for
        // null. The packing optimization must not introduce source validation.
        var first = Assert.IsType<List<byte>>(detector.Compute(source));
        var second = Assert.IsType<List<byte>>(detector.Compute(null!));
        Assert.Empty(first);
        Assert.Empty(second);
        Assert.NotSame(first, second);
        first.Add(0xA5);
        Assert.Empty(second);
        Assert.Equal(0, source.EnumeratorRequests);

        if (bits == 16)
        {
            Assert.Throws<InvalidOperationException>(() => source.CRC16((CRC16Type)variant));
            Assert.Throws<InvalidOperationException>(() => ((IEnumerable<byte>)null!).CRC16((CRC16Type)variant));
        }
        else
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => source.CRC32((CRC32Type)variant));
            Assert.Throws<ArgumentOutOfRangeException>(() => ((IEnumerable<byte>)null!).CRC32((CRC32Type)variant));
        }
        Assert.Equal(0, source.EnumeratorRequests);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void NativeDigestPackingFlowsThroughNumericExtensionsAndMaterializedFrames(Algorithm algorithm)
    {
        byte[] payload = DeterministicPayload(257);
        byte[] snapshot = (byte[])payload.Clone();
        uint expectedValue = ReferenceValue(algorithm, snapshot);
        byte[] suffix = NativeBytes(expectedValue, Width(algorithm));
        byte[] expectedFrame = snapshot.Concat(suffix).ToArray();
        ErrorDetection detector = CreateDetector(algorithm);

        Assert.Equal(expectedValue, NumericDigest(algorithm, payload));
        var encoded = Assert.IsType<List<byte>>(detector.Encode(payload));
        var extensionEncoded = Assert.IsType<List<byte>>(EncodeExtension(algorithm, payload));
        Assert.NotSame(encoded, extensionEncoded);

        // Changing the source after the call must not change packed frames.
        Array.Fill(payload, (byte)0);
        Assert.Equal(expectedFrame, encoded.ToArray());
        Assert.Equal(expectedFrame, extensionEncoded.ToArray());
        Assert.False(detector.IsError(expectedFrame));
        Assert.Equal(snapshot, detector.Decode(expectedFrame).ToArray());

        // Check each trailer position, including the most significant byte.
        // This catches truncation and wrong byte shifts in the direct writer.
        for (int trailerIndex = 0; trailerIndex < suffix.Length; trailerIndex++)
        {
            byte[] corrupted = (byte[])expectedFrame.Clone();
            corrupted[snapshot.Length + trailerIndex] ^= 0x80;
            Assert.True(detector.IsError(corrupted));
            Assert.Empty(detector.Decode(corrupted));
        }

        encoded[^1] ^= 0x01;
        Assert.Equal(expectedFrame, extensionEncoded.ToArray());
    }

    private static int Width(Algorithm algorithm)
        => algorithm is Algorithm.Crc32Basic or Algorithm.Crc32Classic ? 4 : 2;

    private static uint LegacyEmptyValue(Algorithm algorithm)
        => algorithm == Algorithm.Crc16Modbus ? 0xFFFFu : 0;

    private static byte[] ExpectedDigest(Algorithm algorithm, byte[] payload)
        => NativeBytes(ReferenceValue(algorithm, payload), Width(algorithm));

    private static uint ReferenceValue(Algorithm algorithm, byte[] payload)
    {
        if (payload.Length == 0)
        {
            // Legacy DNP returns zero before applying its final XOR. Preserve
            // that exception to the canonical oracle, not a new DNP standard.
            return LegacyEmptyValue(algorithm);
        }
        if (Width(algorithm) == 4)
            return CrcReference.Compute32(payload, Crc32Type(algorithm));

        ushort canonical = CrcReference.Compute16(payload, Crc16Type(algorithm));
        return algorithm == Algorithm.Crc16DnpLegacy
            ? CrcReference.ToLegacyDnpRepresentation(canonical)
            : canonical;
    }

    // BitConverter is a second implementation of native serialization, without
    // Mythosia's old Marshal path or the new direct writer's shift operations.
    private static byte[] NativeBytes(uint value, int width)
        => width == 2 ? BitConverter.GetBytes((ushort)value) : BitConverter.GetBytes(value);

    private static void AssertDigest(byte[] expected, List<byte> actual, string context)
    {
        Assert.True(expected.SequenceEqual(actual),
            $"{context}: expected {Convert.ToHexString(expected)}, actual {Convert.ToHexString(actual.ToArray())}.");
    }

    private static ErrorDetection CreateDetector(Algorithm algorithm)
        => Width(algorithm) == 2
            ? new CRC16(Crc16Type(algorithm))
            : new CRC32(Crc32Type(algorithm));

    private static uint NumericDigest(Algorithm algorithm, IEnumerable<byte> source)
        => Width(algorithm) == 2 ? source.CRC16(Crc16Type(algorithm)) : source.CRC32(Crc32Type(algorithm));

    private static IEnumerable<byte> EncodeExtension(Algorithm algorithm, IEnumerable<byte> source)
        => Width(algorithm) == 2 ? source.WithCRC16(Crc16Type(algorithm)) : source.WithCRC32(Crc32Type(algorithm));

#pragma warning disable CS0618 // Basic aliases remain supported public algorithms.
    private static CRC16Type Crc16Type(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc16Basic => CRC16Type.Basic,
        Algorithm.Crc16Classic => CRC16Type.Classic,
        Algorithm.Crc16Modbus => CRC16Type.Modbus,
        Algorithm.Crc16Xmodem => CRC16Type.CCITTxModem,
        Algorithm.Crc16DnpLegacy => CRC16Type.DNP,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };

    private static CRC32Type Crc32Type(Algorithm algorithm) => algorithm switch
    {
        Algorithm.Crc32Basic => CRC32Type.Basic,
        Algorithm.Crc32Classic => CRC32Type.Classic,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm))
    };
#pragma warning restore CS0618

    private static byte[] DeterministicPayload(int length)
    {
        var result = new byte[length];
        uint state = 0xB17E5EED;
        for (int index = 0; index < result.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            result[index] = (byte)state;
        }
        return result;
    }

    private static IEnumerable<byte> YieldBytes(IEnumerable<byte> source)
    {
        foreach (byte value in source)
            yield return value;
    }

    private sealed class ForbiddenSource : IEnumerable<byte>
    {
        public int EnumeratorRequests { get; private set; }

        public IEnumerator<byte> GetEnumerator()
        {
            EnumeratorRequests++;
            throw new InvalidOperationException("An unsupported algorithm must not read the source.");
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
