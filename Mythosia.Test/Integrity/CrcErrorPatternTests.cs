using Mythosia.Integrity;
using Mythosia.Integrity.CRC;
using Xunit;

namespace Mythosia.Test.Integrity;

[Collection(IntegrityTestCollection.Name)]
public sealed class CrcErrorPatternTests
{
    [Theory]
    [InlineData(8, (int)CRC8Type.Classic, false)]
    [InlineData(8, (int)CRC8Type.Maxim, true)]
    [InlineData(16, (int)CRC16Type.Classic, true)]
    [InlineData(16, (int)CRC16Type.Modbus, true)]
    [InlineData(16, (int)CRC16Type.CCITTxModem, false)]
    [InlineData(16, (int)CRC16Type.DNP, true)]
    [InlineData(32, (int)CRC32Type.Classic, true)]
    public void PayloadBurstPatternsUpToCrcWidth_AreDetectedAtEveryPosition(
        int width, int variant, bool reflectedInput)
    {
        byte[] payload = Enumerable.Range(0, 17).Select(i => (byte)(i * 37)).ToArray();
        byte[] frame = payload.Concat(ReferenceSuffix(payload, width, variant)).ToArray();
        ErrorDetection detector = CreateDetector(width, variant);

        // A burst has changed first/last bits and can contain unchanged bits
        // between them. Test endpoints-only, full, alternating, and mixed
        // interiors; every start position and length through the CRC width.
        // Bits follow each CRC's input order, including byte-boundary crossings.
        for (int length = 1; length <= width; length++)
        for (int start = 0; start <= payload.Length * 8 - length; start++)
        for (int pattern = 0; pattern < 4; pattern++)
        {
            byte[] damaged = (byte[])frame.Clone();
            FlipBurst(damaged, start, length, pattern, reflectedInput);
            AssertRejected(detector, damaged,
                $"CRC{width}, variant {variant}, start {start}, length {length}, pattern {pattern}");
        }
    }

    [Theory]
    [InlineData(8, (int)CRC8Type.Classic)]
    [InlineData(8, (int)CRC8Type.Maxim)]
    [InlineData(16, (int)CRC16Type.Classic)]
    [InlineData(16, (int)CRC16Type.Modbus)]
    [InlineData(16, (int)CRC16Type.CCITTxModem)]
    [InlineData(16, (int)CRC16Type.DNP)]
    [InlineData(32, (int)CRC32Type.Classic)]
    public void EveryPairOfChangedPayloadBits_InAnEightByteMessage_IsDetected(int width, int variant)
    {
        // Keep the message short: CRCs do not promise detection of every
        // two-bit error at arbitrary separations in arbitrarily long messages.
        byte[] payload = { 0x00, 0xFF, 0x80, 0x7F, 0xA5, 0x5A, 0x01, 0xFE };
        byte[] frame = payload.Concat(ReferenceSuffix(payload, width, variant)).ToArray();
        ErrorDetection detector = CreateDetector(width, variant);

        for (int first = 0; first < payload.Length * 8; first++)
        for (int second = first + 1; second < payload.Length * 8; second++)
        {
            byte[] damaged = (byte[])frame.Clone();
            damaged[first / 8] ^= (byte)(1 << (first % 8));
            damaged[second / 8] ^= (byte)(1 << (second % 8));
            AssertRejected(detector, damaged,
                $"CRC{width}, variant {variant}, payload bits {first} and {second}");
        }
    }

    private static void FlipBurst(byte[] frame, int start, int length, int pattern, bool reflectedInput)
    {
        for (int offset = 0; offset < length; offset++)
        {
            bool endpoint = offset == 0 || offset == length - 1;
            bool interior = pattern switch
            {
                0 => false,
                1 => true,
                2 => offset % 2 == 0,
                3 => (offset * 13 + start * 7 + length) % 5 < 2,
                _ => throw new ArgumentOutOfRangeException(nameof(pattern))
            };
            if (!endpoint && !interior) continue;

            int bit = start + offset;
            int shift = reflectedInput ? bit % 8 : 7 - bit % 8;
            frame[bit / 8] ^= (byte)(1 << shift);
        }
    }

    private static void AssertRejected(ErrorDetection detector, byte[] damaged, string context)
    {
        Assert.True(detector.IsError(damaged), context);
        Assert.Empty(detector.Decode(damaged));
    }

    private static byte[] ReferenceSuffix(byte[] payload, int width, int variant) => width switch
    {
        8 => new[] { CrcReference.Compute8(payload, (CRC8Type)variant) },
        16 => BitConverter.GetBytes(Reference16(payload, (CRC16Type)variant)),
        32 => BitConverter.GetBytes(CrcReference.Compute32(payload, (CRC32Type)variant)),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };

    private static ushort Reference16(byte[] payload, CRC16Type type)
    {
        ushort canonical = CrcReference.Compute16(payload, type);
        return type == CRC16Type.DNP
            ? CrcReference.ToLegacyDnpRepresentation(canonical)
            : canonical;
    }

    private static ErrorDetection CreateDetector(int width, int variant) => width switch
    {
        8 => new CRC8((CRC8Type)variant),
        16 => new CRC16((CRC16Type)variant),
        32 => new CRC32((CRC32Type)variant),
        _ => throw new ArgumentOutOfRangeException(nameof(width))
    };
}
