namespace Mythosia.Test.Integrity;

/// <summary>
/// Independent Adler-32 oracle using a weighted sum of the input bytes rather
/// than the production running pair of sums or its block reduction strategy.
/// </summary>
internal static class Adler32Reference
{
    // RFC 1950 section 2.2 defines s1 = 1 + sum(bytes) and s2 as the sum
    // of successive s1 values, modulo 65521. Expanding the latter gives
    // s2 = length + sum((length - index) * bytes[index]).
    // https://www.rfc-editor.org/rfc/rfc1950.html#section-2.2
    public static uint Compute(ReadOnlySpan<byte> source)
    {
        const ulong modulus = 65521;
        ulong length = (ulong)source.Length;
        ulong first = 1;
        ulong second = length % modulus;

        for (int index = 0; index < source.Length; index++)
        {
            first = (first + source[index]) % modulus;
            second = (second + (length - (ulong)index) * source[index]) % modulus;
        }

        return (uint)((second << 16) | first);
    }
}
