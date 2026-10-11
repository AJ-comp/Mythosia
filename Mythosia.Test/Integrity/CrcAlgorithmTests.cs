using System.Text;
using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

// These aliases are intentionally tested as supported compatibility names.
#pragma warning disable CS0618

[Collection(IntegrityTestCollection.Name)]
public class CrcAlgorithmTests
{
    private static readonly byte[] PublishedCheckInput = Encoding.ASCII.GetBytes("123456789");

    public static IEnumerable<object[]> Algorithms()
    {
        foreach (CRC8Type type in Enum.GetValues<CRC8Type>())
            yield return new object[] { 8, (int)type };
        foreach (CRC16Type type in Enum.GetValues<CRC16Type>())
            yield return new object[] { 16, (int)type };
        foreach (CRC32Type type in Enum.GetValues<CRC32Type>())
            yield return new object[] { 32, (int)type };
    }

    // Independently published checks calibrate the oracle before it is used
    // for exhaustive inputs. See CrcReference.cs for the catalogue entries.
    [Theory]
    [InlineData(CRC8Type.Basic, 0xF4)]
    [InlineData(CRC8Type.Classic, 0xF4)]
    [InlineData(CRC8Type.Maxim, 0xA1)]
    public void Crc8MatchesPublishedCheck(CRC8Type type, byte expected)
    {
        Assert.Equal(expected, CrcReference.Compute8(PublishedCheckInput, type));
        AssertValueAndBytes(PublishedCheckInput, 8, (int)type, new CRC8(type), expected);
    }

    [Theory]
    [InlineData(CRC16Type.Basic, 0xBB3D)]
    [InlineData(CRC16Type.Classic, 0xBB3D)]
    [InlineData(CRC16Type.Modbus, 0x4B37)]
    [InlineData(CRC16Type.CCITTxModem, 0x31C3)]
    [InlineData(CRC16Type.DNP, 0xEA82)]
    public void Crc16MatchesPublishedCheckWithExplicitDnpCompatibility(CRC16Type type, ushort canonical)
    {
        Assert.Equal(canonical, CrcReference.Compute16(PublishedCheckInput, type));
        ushort expected = type == CRC16Type.DNP
            ? CrcReference.ToLegacyDnpRepresentation(canonical)
            : canonical;
        AssertValueAndBytes(PublishedCheckInput, 16, (int)type, new CRC16(type), expected);
    }

    [Theory]
    [InlineData(CRC32Type.Basic, 0xCBF43926u)]
    [InlineData(CRC32Type.Classic, 0xCBF43926u)]
    public void Crc32MatchesPublishedCheck(CRC32Type type, uint expected)
    {
        Assert.Equal(expected, CrcReference.Compute32(PublishedCheckInput, type));
        AssertValueAndBytes(PublishedCheckInput, 32, (int)type, new CRC32(type), expected);
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EverySingleByteMatchesIndependentBitwiseReference(int width, int variant)
    {
        ErrorDetection detector = CreateDetector(width, variant);
        var input = new byte[1];
        for (int value = 0; value <= byte.MaxValue; value++)
        {
            input[0] = (byte)value;
            AssertMatchesReference(input, width, variant, detector);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void EveryTwoByteCombinationMatchesIndependentBitwiseReference(int width, int variant)
    {
        ErrorDetection detector = CreateDetector(width, variant);
        var input = new byte[2];
        for (int value = 0; value <= ushort.MaxValue; value++)
        {
            input[0] = (byte)(value >> 8);
            input[1] = (byte)value;
            AssertMatchesReference(input, width, variant, detector);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void BinaryPatternsAtLengthBoundariesMatchIndependentReference(int width, int variant)
    {
        int[] lengths =
        {
            1, 2, 3, 4, 7, 8, 15, 16, 17, 31, 32, 33, 63, 64, 65,
            127, 128, 129, 255, 256, 257, 511, 512, 513, 1023, 1024,
            1025, 4095, 4096, 4097, 65535, 65536, 65537
        };
        ErrorDetection detector = CreateDetector(width, variant);
        foreach (int length in lengths)
        {
            foreach (byte repeated in new byte[] { 0x00, 0xFF, 0x55, 0xAA })
                AssertMatchesReference(Enumerable.Repeat(repeated, length).ToArray(), width, variant, detector);

            AssertMatchesReference(Enumerable.Range(0, length).Select(i => (byte)i).ToArray(), width, variant, detector);
            AssertMatchesReference(Enumerable.Range(0, length).Select(i => (byte)(255 - i)).ToArray(), width, variant, detector);
            AssertMatchesReference(Enumerable.Range(0, length).Select(i => (byte)(1 << (i % 8))).ToArray(), width, variant, detector);
            AssertMatchesReference(DeterministicBytes(length, (uint)length), width, variant, detector);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void DeterministicRandomCorpusMatchesIndependentReference(int width, int variant)
    {
        ErrorDetection detector = CreateDetector(width, variant);
        // Explicit xorshift32 keeps the corpus stable across .NET versions.
        for (uint sample = 1; sample <= 256; sample++)
        {
            int length = 1 + (int)((sample * 7919) % 8192);
            AssertMatchesReference(DeterministicBytes(length, sample * 0x9E3779B9), width, variant, detector);
        }
    }

    [Theory]
    [MemberData(nameof(Algorithms))]
    public void OneMebibyteBinaryPayloadMatchesIndependentReference(int width, int variant)
    {
        byte[] input = DeterministicBytes(1024 * 1024, 0xC0FFEE01);
        AssertMatchesReference(input, width, variant, CreateDetector(width, variant));
    }

    [Fact]
    public void Crc8ClassicMatchesAnalogDevicesWorkedExample()
    {
        // ADI's worked PEC example: 65 43 21 -> 86.
        // https://www.analog.com/en/resources/analog-dialogue/articles/cyclic-redundancy-and-correct-data-communications.html
        byte[] input = { 0x65, 0x43, 0x21 };
        Assert.Equal((byte)0x86, CrcReference.Compute8(input, CRC8Type.Classic));
        AssertValueAndBytes(input, 8, (int)CRC8Type.Classic, new CRC8(), 0x86);
    }

    [Fact]
    public void Crc8MaximMatchesAnalogDevicesRomExample()
    {
        // ADI Application Note 27, Example 2 and Table 1; bytes in read order.
        // https://www.analog.com/en/resources/technical-articles/understanding-and-using-cyclic-redundancy-checks-with-maxim-1wire-and-ibutton-products.html
        byte[] input = { 0x02, 0x1C, 0xB8, 0x01, 0x00, 0x00, 0x00 };
        Assert.Equal((byte)0xA2, CrcReference.Compute8(input, CRC8Type.Maxim));
        AssertValueAndBytes(input, 8, (int)CRC8Type.Maxim, new CRC8(CRC8Type.Maxim), 0xA2);
    }

    [Fact]
    public void DnpCanonicalValueAndLegacyRepresentationMatchOpendnp3Example()
    {
        // opendnp3's independently published link-header vector.
        // https://github.com/dnp3/opendnp3/blob/release/cpp/tests/unit/TestCRC.cpp
        byte[] input = { 0x05, 0x64, 0x05, 0xC0, 0x01, 0x00, 0x00, 0x04 };
        Assert.Equal((ushort)0x21E9, CrcReference.Compute16(input, CRC16Type.DNP));
        Assert.Equal((ushort)0xE921, CrcReference.ToLegacyDnpRepresentation(0x21E9));
        // This preserves Mythosia's legacy byte swap; it does not claim that
        // Mythosia's native-packed suffix is the standard DNP3 wire encoding.
        AssertValueAndBytes(input, 16, (int)CRC16Type.DNP, new CRC16(CRC16Type.DNP), 0xE921);
    }

    private static void AssertMatchesReference(byte[] input, int width, int variant, ErrorDetection detector)
    {
        uint expected = width switch
        {
            8 => CrcReference.Compute8(input, (CRC8Type)variant),
            16 => Reference16WithLegacyDnpSwap(input, (CRC16Type)variant),
            32 => CrcReference.Compute32(input, (CRC32Type)variant),
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
        AssertValueAndBytes(input, width, variant, detector, expected);
    }

    private static ushort Reference16WithLegacyDnpSwap(byte[] input, CRC16Type type)
    {
        ushort canonical = CrcReference.Compute16(input, type);
        return type == CRC16Type.DNP ? CrcReference.ToLegacyDnpRepresentation(canonical) : canonical;
    }

    private static void AssertValueAndBytes(byte[] input, int width, int variant, ErrorDetection detector, uint expected)
    {
        uint actual = width switch
        {
            8 => input.CRC8((CRC8Type)variant),
            16 => input.CRC16((CRC16Type)variant),
            32 => input.CRC32((CRC32Type)variant),
            _ => throw new ArgumentOutOfRangeException(nameof(width))
        };
        if (actual != expected)
            Assert.Fail($"{Describe(width, variant)}, {DescribeInput(input)}: expected 0x{expected:X}, actual 0x{actual:X}.");

        // Manual native-endian packing is independent of Marshal/ToByteArray.
        byte[] computed = detector.Compute(input).ToArray();
        int byteCount = width / 8;
        if (computed.Length != byteCount)
            Assert.Fail($"{Describe(width, variant)} returned {computed.Length} bytes; expected {byteCount}.");

        for (int index = 0; index < byteCount; index++)
        {
            int shift = (BitConverter.IsLittleEndian ? index : byteCount - 1 - index) * 8;
            byte expectedByte = (byte)(expected >> shift);
            if (computed[index] != expectedByte)
                Assert.Fail($"{Describe(width, variant)}, {DescribeInput(input)}, CRC byte {index}: expected 0x{expectedByte:X2}, actual 0x{computed[index]:X2}.");
        }
    }

    private static ErrorDetection CreateDetector(int width, int variant) => width switch
    {
        8 => new CRC8((CRC8Type)variant),
        16 => new CRC16((CRC16Type)variant),
        32 => new CRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static string Describe(int width, int variant) => width switch
    {
        8 => $"CRC8/{(CRC8Type)variant}",
        16 => $"CRC16/{(CRC16Type)variant}",
        32 => $"CRC32/{(CRC32Type)variant}",
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static string DescribeInput(byte[] input)
        => $"length {input.Length}, prefix {Convert.ToHexString(input.AsSpan(0, Math.Min(input.Length, 32)))}";

    private static byte[] DeterministicBytes(int length, uint seed)
    {
        var result = new byte[length];
        uint state = seed == 0 ? 1u : seed;
        for (int index = 0; index < result.Length; index++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            result[index] = (byte)state;
        }
        return result;
    }
}

#pragma warning restore CS0618
