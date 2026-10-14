using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class Crc16VariantTests
{
    public static IEnumerable<object[]> PublishedVectors()
    {
        // AUTOSAR Classic Platform R24-11, CRC Routines, SWS_Crc_00078:
        // complete hexadecimal input and numerical CRC-16/CCITT-FALSE output.
        // https://www.autosar.org/fileadmin/standards/R24-11/CP/AUTOSAR_CP_SWS_CRCLibrary.pdf
        yield return new object[] { CRC16Type.CCITTFalse, "00000000", (ushort)0x84C0 };
        yield return new object[] { CRC16Type.CCITTFalse, "F20183", (ushort)0xD374 };
        yield return new object[] { CRC16Type.CCITTFalse, "0FAA0055", (ushort)0x2023 };
        yield return new object[] { CRC16Type.CCITTFalse, "00FF5511", (ushort)0xB8F9 };
        yield return new object[] { CRC16Type.CCITTFalse, "332255AABBCCDDEEFF", (ushort)0xF53F };
        yield return new object[] { CRC16Type.CCITTFalse, "926B55", (ushort)0x0745 };
        yield return new object[] { CRC16Type.CCITTFalse, "FFFFFFFF", (ushort)0x1D0F };

        // ITU-T X.25 (10/1996), Appendix I, printed page 145. Address/control
        // octets exclude HDLC flags and inserted zero bits. The printed FCS
        // octets are least-significant first; these are the numerical answers.
        // https://www.itu.int/rec/dologin_pub.asp?id=T-REC-X.25-199610-I!!PDF-E&lang=e&type=items
        yield return new object[] { CRC16Type.X25, "033F", (ushort)0xEC5B };
        yield return new object[] { CRC16Type.X25, "0173", (ushort)0x5783 };
        yield return new object[] { CRC16Type.X25, "013F", (ushort)0xDFEB };
        yield return new object[] { CRC16Type.X25, "0373", (ushort)0x6433 };

        // ITSO TS 1000-2:2025-03, Annex A.1.1, printed page 57: CRC_B examples.
        // Annex A.1.2 provides the ISO/IEC 14443-3 reference calculation:
        // initial FFFF, reflected 1021 polynomial, final complement, LSB-first FCS.
        // These are the same CRC parameters as CRC-16/X-25.
        // https://www.itso.org.uk/download/42151.2/part-2-customer-media-data-and-customer-media-architecture/
        yield return new object[] { CRC16Type.X25, "000000", (ushort)0xC6CC };
        yield return new object[] { CRC16Type.X25, "0FAAFF", (ushort)0xD1FC };
        yield return new object[] { CRC16Type.X25, "0A123456", (ushort)0xF62C };

        // CRC RevEng catalogue check values for the ASCII octets "123456789".
        // https://reveng.sourceforge.io/crc-catalogue/16.htm#crc.cat.crc-16-ibm-3740
        // https://reveng.sourceforge.io/crc-catalogue/16.htm#crc.cat.crc-16-ibm-sdlc
        yield return new object[] { CRC16Type.CCITTFalse, "313233343536373839", (ushort)0x29B1 };
        yield return new object[] { CRC16Type.X25, "313233343536373839", (ushort)0x906E };
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public async Task PublishedFixedAnswersMatchOracleOneShotEverySplitAndStreams(
        CRC16Type type, string hex, ushort expected)
    {
        byte[] input = Convert.FromHexString(hex);
        Assert.Equal(expected, CrcReference.Compute16(input, type));
        Assert.Equal(expected, input.CRC16(type));
        Assert.Equal(NativeBytes(expected), new CRC16(type).Compute(input));

        var accumulator = new CRC16Accumulator(type);
        for (int split = 0; split <= input.Length; split++)
        {
            accumulator.Reset();
            accumulator.Append(input.AsSpan(0, split));
            accumulator.Append(ReadOnlySpan<byte>.Empty);
            accumulator.Append(input.AsSpan(split));
            Assert.Equal(expected, accumulator.GetCurrentValue());
        }

        using var synchronous = new MemoryStream(input, writable: false);
        using var asynchronous = new MemoryStream(input, writable: false);
        Assert.Equal(expected, synchronous.ComputeCRC16(type));
        Assert.Equal(expected, await asynchronous.ComputeCRC16Async(type));
    }

    [Theory]
    [MemberData(nameof(PublishedVectors))]
    public void PublishedAnswersDefineNativeEndianFramesAndRejectEverySingleBitMutation(
        CRC16Type type, string hex, ushort expected)
    {
        byte[] input = Convert.FromHexString(hex);
        byte[] frame = input.Concat(NativeBytes(expected)).ToArray();
        var detector = new CRC16(type);
        Assert.Equal(frame, detector.Encode(input));
        Assert.Equal(frame, input.WithCRC16(type));
        Assert.Equal(input, detector.Decode(frame));
        Assert.False(detector.IsError(frame));

        for (int bit = 0; bit < frame.Length * 8; bit++)
        {
            byte[] damaged = frame.ToArray();
            damaged[bit / 8] ^= (byte)(1 << (bit % 8));
            Assert.True(detector.IsError(damaged));
            Assert.Empty(detector.Decode(damaged));
        }
    }

    [Fact]
    public void X25ProtocolFramesHavePublishedRawResidueWithLittleEndianFcs()
    {
        // RFC 1662, Appendix C.2: PPPGOODFCS16 is the raw, uncomplemented
        // receive register 0xF0B8. The final numerical CRC is its complement.
        // HDLC uses LSB-first FCS octets regardless of machine endianness;
        // Mythosia's native-endian Encode contract is tested separately above.
        // https://www.rfc-editor.org/rfc/rfc1662.html#appendix-C.2
        const ushort rawResidue = 0xF0B8;
        const ushort finalCrcOfFrame = unchecked((ushort)~rawResidue);
        foreach (object[] vector in PublishedVectors())
        {
            if ((CRC16Type)vector[0] != CRC16Type.X25)
                continue;

            byte[] input = Convert.FromHexString((string)vector[1]);
            ushort fcs = (ushort)vector[2];
            byte[] protocolFrame = input.Concat(new[] { (byte)fcs, (byte)(fcs >> 8) }).ToArray();
            Assert.Equal(finalCrcOfFrame, CrcReference.Compute16(protocolFrame, CRC16Type.X25));
            Assert.Equal(finalCrcOfFrame, protocolFrame.CRC16(CRC16Type.X25));
            var accumulator = new CRC16Accumulator(CRC16Type.X25);
            accumulator.Append(protocolFrame);
            Assert.Equal(rawResidue, unchecked((ushort)~accumulator.GetCurrentValue()));
        }
    }

    [Theory]
    [InlineData(CRC16Type.CCITTFalse, 0xFFFF)]
    [InlineData(CRC16Type.X25, 0x0000)]
    public async Task EmptyStateResetAndDigestOnlyFramesHaveExplicitContracts(CRC16Type type, int expectedValue)
    {
        ushort expected = (ushort)expectedValue;
        byte[] empty = Array.Empty<byte>();
        var detector = new CRC16(type);
        Assert.Equal(type, detector.Type);
        Assert.Equal(type.ToString(), detector.GetDetectionType());
        Assert.Equal(expected, CrcReference.Compute16(empty, type));
        Assert.Equal(expected, empty.CRC16(type));
        Assert.Equal(NativeBytes(expected), detector.Compute(empty));
        Assert.Equal(NativeBytes(expected), detector.Encode(empty));

        var accumulator = new CRC16Accumulator(type);
        Assert.Equal(expected, accumulator.GetCurrentValue());
        accumulator.Append(new byte[] { 0x80, 0x00, 0xFF });
        accumulator.Reset();
        accumulator.Append(ReadOnlySpan<byte>.Empty);
        Assert.Equal(expected, accumulator.GetCurrentValue());

        using var source = new MemoryStream();
        Assert.Equal(expected, source.ComputeCRC16(type));
        Assert.Equal(expected, await source.ComputeCRC16Async(type));
        Assert.True(source.CanRead);
        for (int length = 0; length <= sizeof(ushort); length++)
        {
            Assert.True(detector.IsError(NativeBytes(expected).Take(length)));
            Assert.Empty(detector.Decode(NativeBytes(expected).Take(length)));
        }
    }

    [Theory]
    [InlineData(CRC16Type.CCITTFalse, 0x29B1)]
    [InlineData(CRC16Type.X25, 0x906E)]
    public void NewComputePathsConsumeAForwardOnlyEnumerableOnceAndDisposeIt(CRC16Type type, int expectedValue)
    {
        byte[] input = Convert.FromHexString("313233343536373839");
        AssertSinglePass(source => BitConverter.ToUInt16(new CRC16(type).Compute(source).ToArray(), 0));
        AssertSinglePass(source => source.CRC16(type));

        void AssertSinglePass(Func<IEnumerable<byte>, ushort> compute)
        {
            int enumerations = 0;
            int disposals = 0;
            Assert.Equal((ushort)expectedValue, compute(Enumerate()));
            Assert.Equal(1, enumerations);
            Assert.Equal(1, disposals);

            IEnumerable<byte> Enumerate()
            {
                if (++enumerations != 1)
                    throw new InvalidOperationException("The input cannot be enumerated twice.");
                try
                {
                    foreach (byte value in input)
                        yield return value;
                }
                finally
                {
                    disposals++;
                }
            }
        }
    }

    private static byte[] NativeBytes(ushort value)
        => BitConverter.IsLittleEndian
            ? new[] { (byte)value, (byte)(value >> 8) }
            : new[] { (byte)(value >> 8), (byte)value };
}
