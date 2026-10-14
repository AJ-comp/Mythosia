using Mythosia.Integrity.CRC;

namespace Mythosia.Test.Integrity;

/// <summary>
/// Independent bit-by-bit normal-polynomial CRC64 oracle. The XZ calculation
/// explicitly reflects bytes and the final register; neither path uses a table.
/// </summary>
internal static class Crc64VariantReference
{
    private const ulong NormalPolynomial = 0x42F0E1EBA9EA3693UL;

    internal static ulong Compute(ReadOnlySpan<byte> source, CRC64Type type)
    {
        if (type != CRC64Type.ECMA182 && type != CRC64Type.XZ)
            throw new ArgumentOutOfRangeException(nameof(type));

        bool reflected = type == CRC64Type.XZ;
        ulong register = reflected ? ulong.MaxValue : 0;
        foreach (byte value in source)
        {
            register ^= (reflected ? Reflect(value, 8) : value) << 56;
            for (int bit = 0; bit < 8; bit++)
            {
                bool outgoingBit = (register & 0x8000000000000000UL) != 0;
                register <<= 1;
                if (outgoingBit) register ^= NormalPolynomial;
            }
        }
        return reflected ? Reflect(register, 64) ^ ulong.MaxValue : register;
    }

    private static ulong Reflect(ulong value, int width)
    {
        ulong result = 0;
        for (int bit = 0; bit < width; bit++)
        {
            result = (result << 1) | (value & 1);
            value >>= 1;
        }
        return result;
    }
}
