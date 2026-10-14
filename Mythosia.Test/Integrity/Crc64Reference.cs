namespace Mythosia.Test.Integrity;

/// <summary>
/// Independent CRC64/NVME oracle: a normal-form polynomial, left-shifting
/// register and explicit reflection, rather than the production lookup table.
/// </summary>
internal static class Crc64Reference
{
    // Equivalent normal form of AWS's reflected 0x9A6C9329AC4BC9B5 polynomial.
    // Both the oracle and production paths are calibrated against AWS's fixed answers.
    // https://github.com/awslabs/aws-checksums/blob/12cd22e81543b8547132762023076ab6afa25559/tests/crc64_test.c
    private const ulong NormalPolynomial = 0xAD93D23594C93659;

    public static ulong Compute(ReadOnlySpan<byte> source)
    {
        ulong register = ulong.MaxValue;
        foreach (byte value in source)
        {
            register ^= Reflect(value, 8) << 56;
            for (int bit = 0; bit < 8; bit++)
            {
                bool outgoingBit = (register & 0x8000000000000000) != 0;
                register <<= 1;
                if (outgoingBit)
                    register ^= NormalPolynomial;
            }
        }
        return Reflect(register, 64) ^ ulong.MaxValue;
    }

    private static ulong Reflect(ulong value, int width)
    {
        ulong reflected = 0;
        for (int bit = 0; bit < width; bit++)
        {
            reflected = (reflected << 1) | (value & 1);
            value >>= 1;
        }
        return reflected;
    }
}
