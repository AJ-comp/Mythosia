namespace Mythosia.Test.Integrity;

/// <summary>
/// Independent CRC-32/ISCSI (CRC32C) oracle using a normal-form polynomial,
/// an MSB-first register, and explicit input/output reflection. No production
/// lookup table, accumulator, numeric extension, or byte packing is reused.
/// </summary>
internal static class Crc32CReference
{
    // width=32, poly=1EDC6F41, init=FFFFFFFF, refin/refout=true,
    // xorout=FFFFFFFF, check=E3069283, residue=B798B438.
    // https://reveng.sourceforge.io/crc-catalogue/17plus.htm#crc.cat.crc-32-iscsi
    public const uint InitialRegister = uint.MaxValue;
    private const uint NormalPolynomial = 0x1EDC6F41;

    public static uint Compute(ReadOnlySpan<byte> source)
        => Finish(Update(InitialRegister, source));

    /// <summary>Updates the unfinalized, normal-form register.</summary>
    public static uint Update(uint register, ReadOnlySpan<byte> source)
    {
        foreach (byte value in source)
        {
            register ^= Reflect(value, 8) << 24;
            for (int bit = 0; bit < 8; bit++)
            {
                bool outgoingBit = (register & 0x80000000u) != 0;
                register = unchecked(register << 1);
                if (outgoingBit)
                    register ^= NormalPolynomial;
            }
        }
        return register;
    }

    /// <summary>Finalization does not change the caller's unfinalized state.</summary>
    public static uint Finish(uint register)
        => Reflect(register, 32) ^ uint.MaxValue;

    private static uint Reflect(uint value, int width)
    {
        uint reflected = 0;
        for (int bit = 0; bit < width; bit++)
        {
            reflected = (reflected << 1) | (value & 1);
            value >>= 1;
        }
        return reflected;
    }
}
