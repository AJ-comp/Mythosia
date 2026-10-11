using System.Text;
using Mythosia.Integrity.Checksum;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public class ChecksumAlgorithmTests
{
    private static readonly Checksum8Type[] Algorithms = Enum.GetValues<Checksum8Type>();

    public static IEnumerable<object[]> FixedVectors()
    {
        // The expectations are hand-calculated, not copied from library output.
        // Columns are input bytes, XOR, sum modulo 256, and additive inverse modulo 256.
        (string Hex, byte Xor, byte Sum, byte Inverse)[] vectors =
        {
            ("00", 0x00, 0x00, 0x00),
            ("01", 0x01, 0x01, 0xFF),
            ("7F", 0x7F, 0x7F, 0x81),
            ("80", 0x80, 0x80, 0x80),
            ("FF", 0xFF, 0xFF, 0x01),
            ("FF01", 0xFE, 0x00, 0x00),
            ("FFFF", 0x00, 0xFE, 0x02),
            ("01020304", 0x04, 0x0A, 0xF6),
            ("000102037F80FEFF", 0xFE, 0x02, 0xFE),
            ("5555", 0x00, 0xAA, 0x56),
            ("AA55", 0xFF, 0xFF, 0x01),
            ("313233343536373839", 0x31, 0xDD, 0x23),
            ("41", 0x41, 0x41, 0xBF),
            ("0102030405060708090A0B0C0D0E0F10", 0x10, 0x88, 0x78),
        };

        foreach (var type in Algorithms)
        {
            foreach (var vector in vectors)
            {
                byte expected = type switch
                {
                    Checksum8Type.Xor or Checksum8Type.NMEA => vector.Xor,
                    Checksum8Type.Modulo256 => vector.Sum,
                    Checksum8Type.TwosComplement => vector.Inverse,
                    _ => throw new ArgumentOutOfRangeException(nameof(type)),
                };

                yield return new object[] { type, vector.Hex, expected };
            }
        }
    }

    [Theory]
    [MemberData(nameof(FixedVectors))]
    public void FixedVectorsMatchBothPublicCalculationApis(
        Checksum8Type type, string hex, byte expected)
    {
        byte[] source = Convert.FromHexString(hex);
        var detector = new Checksum8(type);

        // Anchor the independent oracle before using it for the exhaustive corpus.
        Assert.Equal(expected, MathematicalChecksum(type, source));
        AssertCalculation(detector, source, expected);
        Assert.Equal(source.Concat(new[] { expected }), source.WithChecksum8(type));
    }

    // Trimble publishes these complete NMEA sentences with literal checksums:
    // https://receiverhelp.trimble.com/alloy-gnss/en-us/nmea0183-messages-common-message-elements.html
    // https://receiverhelp.trimble.com/oem-gnss/nmea0183-messages-gll.html
    // Only the bytes between '$' and '*' belong to an NMEA checksum calculation.
    [Theory]
    [InlineData("GPGGA,172814.0,3723.46587704,N,12202.26957864,W,2,6,1.2,18.893,M,-25.669,M,2.0,0031", 0x4F)]
    [InlineData("GPGLL,3953.88008971,N,10506.75318910,W,034138.00,A,D", 0x7A)]
    public void NmeaMatchesPublishedReceiverExamples(string body, byte expected)
    {
        byte[] source = Encoding.ASCII.GetBytes(body);

        Assert.Equal(expected, MathematicalChecksum(Checksum8Type.NMEA, source));
        AssertCalculation(new Checksum8(Checksum8Type.NMEA), source, expected);
        AssertCalculation(new Checksum8(Checksum8Type.Xor), source, expected);
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void EverySingleByteMatchesIndependentMathematicalOracle(Checksum8Type type)
    {
        var detector = new Checksum8(type);
        byte[] source = new byte[1];

        for (int value = 0; value <= byte.MaxValue; value++)
        {
            source[0] = (byte)value;
            AssertCalculation(detector, source, MathematicalChecksum(type, source));
        }
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void EveryOrderedBytePairMatchesIndependentMathematicalOracle(Checksum8Type type)
    {
        var detector = new Checksum8(type);
        byte[] source = new byte[2];

        for (int first = 0; first <= byte.MaxValue; first++)
        {
            source[0] = (byte)first;
            for (int second = 0; second <= byte.MaxValue; second++)
            {
                source[1] = (byte)second;
                AssertCalculation(detector, source, MathematicalChecksum(type, source));
            }
        }
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void SeededBinaryPayloadsAcrossLengthBoundariesMatchMathematicalOracle(Checksum8Type type)
    {
        int[] lengths =
        {
            1, 2, 3, 7, 8, 15, 16, 31, 32, 63, 64,
            127, 128, 255, 256, 257, 511, 512, 513, 1023, 1024, 4096,
        };
        var random = new Random(0x5EED);
        var detector = new Checksum8(type);

        foreach (int length in lengths)
        {
            for (int sample = 0; sample < 4; sample++)
            {
                byte[] source = new byte[length];
                random.NextBytes(source);
                AssertCalculation(detector, source, MathematicalChecksum(type, source));
            }
        }
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void EveryByteValueTogetherHasKnownChecksum(Checksum8Type type)
    {
        byte[] source = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        // 0 XOR ... XOR 255 = 0; 0 + ... + 255 = 32640 = 127 * 256 + 128.
        byte expected = type is Checksum8Type.Xor or Checksum8Type.NMEA ? (byte)0 : (byte)0x80;

        AssertCalculation(new Checksum8(type), source, expected);
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void LongConstantPayloadsPreserveRepeatedModuloWraparound(Checksum8Type type)
    {
        var detector = new Checksum8(type);

        foreach (int length in new[] { 255, 256, 257, 65535, 65536, 65537 })
        {
            foreach (byte value in new byte[] { 0x00, 0x01, 0x7F, 0x80, 0xFF })
            {
                byte[] source = Enumerable.Repeat(value, length).ToArray();
                // Use the closed form as well as the iterative oracle: this exercises
                // many wraps beyond an 8-bit or 16-bit accumulator's capacity.
                long sum = (long)value * length;
                byte expected = type switch
                {
                    Checksum8Type.Xor or Checksum8Type.NMEA => length % 2 == 0 ? (byte)0 : value,
                    Checksum8Type.Modulo256 => (byte)(sum % 256),
                    Checksum8Type.TwosComplement => (byte)((256 - sum % 256) % 256),
                    _ => throw new ArgumentOutOfRangeException(nameof(type)),
                };

                Assert.Equal(expected, MathematicalChecksum(type, source));
                AssertCalculation(detector, source, expected);
            }
        }
    }

    [Fact]
    public void NmeaAndXorAreEquivalentForRawBinaryInputsIncludingSentenceDelimiters()
    {
        var random = new Random(0x4E4D4541);

        for (int length = 1; length <= 512; length++)
        {
            byte[] source = new byte[length];
            random.NextBytes(source);

            Assert.Equal(source.Checksum8(Checksum8Type.Xor), source.Checksum8(Checksum8Type.NMEA));
        }

        // This API takes raw bytes; it does not parse or remove NMEA delimiters.
        byte[] delimiters = Encoding.ASCII.GetBytes("$*\r\n");
        AssertCalculation(new Checksum8(Checksum8Type.NMEA), delimiters, 0x09);
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void PermutationsHaveTheSameChecksumByDesign(Checksum8Type type)
    {
        byte[] original = { 0x00, 0x01, 0x03, 0x10, 0x55, 0x7F, 0x80, 0xA5, 0xFE, 0xFF };
        byte expected = MathematicalChecksum(type, original);
        var detector = new Checksum8(type);

        for (int offset = 0; offset < original.Length; offset++)
        {
            byte[] rotated = original.Skip(offset).Concat(original.Take(offset)).ToArray();
            AssertCalculation(detector, rotated, expected);
            AssertCalculation(detector, rotated.Reverse().ToArray(), expected);
        }
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void CompensatingChangesAreKnownChecksumCollisions(Checksum8Type type)
    {
        byte[] original = { 0x10, 0x20 };
        byte[] modified = type is Checksum8Type.Xor or Checksum8Type.NMEA
            ? new byte[] { 0x11, 0x21 } // Flip the same bit twice: XOR is unchanged.
            : new byte[] { 0x11, 0x1F }; // Add one and subtract one: the sum is unchanged.
        byte expected = type == Checksum8Type.TwosComplement ? (byte)0xD0 : (byte)0x30;
        var detector = new Checksum8(type);

        AssertCalculation(detector, original, expected);
        AssertCalculation(detector, modified, expected);
        byte[] collisionFrame = modified.Concat(new[] { expected }).ToArray();
        Assert.False(detector.IsError(collisionFrame));
        Assert.Equal(modified, detector.Decode(collisionFrame));
    }

    [Fact]
    public void TwosComplementMakesTheSumOfPayloadAndChecksumZeroModulo256()
    {
        var random = new Random(0x256);

        for (int length = 1; length <= 512; length++)
        {
            byte[] source = new byte[length];
            random.NextBytes(source);
            int sum = source.Sum(value => (int)value);
            byte complement = source.Checksum8(Checksum8Type.TwosComplement);

            Assert.Equal(0, (sum + complement) % 256);
            Assert.Equal((byte)(sum % 256), source.Checksum8(Checksum8Type.Modulo256));
        }
    }

    [Theory]
    [InlineData(Checksum8Type.Xor)]
    [InlineData(Checksum8Type.NMEA)]
    [InlineData(Checksum8Type.Modulo256)]
    [InlineData(Checksum8Type.TwosComplement)]
    public void ObsoleteAliasesStillMatchMathematicalChecksumAndFrame(Checksum8Type type)
    {
        byte[] source = { 0x00, 0x01, 0x7F, 0x80, 0xFE, 0xFF };
        byte expected = MathematicalChecksum(type, source);

#pragma warning disable CS0618 // The supported legacy aliases are intentionally exercised.
        CheckSum8Type legacyType = type switch
        {
            Checksum8Type.Xor => CheckSum8Type.Xor,
            Checksum8Type.NMEA => CheckSum8Type.NMEA,
            Checksum8Type.Modulo256 => CheckSum8Type.Modulo256,
            Checksum8Type.TwosComplement => CheckSum8Type.TwosComplement,
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
        Assert.Equal(expected, source.CheckSum8(legacyType));
        Assert.Equal(source.Concat(new[] { expected }), source.WithCheckSum8(legacyType));
#pragma warning restore CS0618
    }

    private static byte MathematicalChecksum(Checksum8Type type, IEnumerable<byte> source)
    {
        // Independent mathematical reference: no library calls, tables, indexed
        // access, production conversion helpers, or output byte-order assumptions.
        long sum = 0;
        int xor = 0;
        foreach (byte value in source)
        {
            sum += value;
            xor ^= value;
        }

        return type switch
        {
            Checksum8Type.Xor or Checksum8Type.NMEA => (byte)xor,
            Checksum8Type.Modulo256 => (byte)(sum % 256),
            Checksum8Type.TwosComplement => (byte)((256 - sum % 256) % 256),
            _ => throw new ArgumentOutOfRangeException(nameof(type)),
        };
    }

    private static void AssertCalculation(Checksum8 detector, byte[] source, byte expected)
    {
        byte actual = source.Checksum8(detector.Type);
        if (actual != expected)
        {
            Assert.Fail($"{detector.Type} extension: expected {expected:X2}, actual {actual:X2}; {Describe(source)}");
        }

        byte[] computed = detector.Compute(source).ToArray();
        if (computed.Length != 1 || computed[0] != expected)
        {
            Assert.Fail($"{detector.Type} Compute: expected one byte {expected:X2}, actual {Convert.ToHexString(computed)}; {Describe(source)}");
        }
    }

    private static string Describe(byte[] source)
        => $"length={source.Length}, input prefix={Convert.ToHexString(source.AsSpan(0, Math.Min(source.Length, 32)))}";
}
