namespace Mythosia.Test.Integrity;

/// <summary>
/// Independent RFC 1071 oracle: sum the two byte lanes separately with 64-bit
/// arithmetic, then apply their positional weights and end-around carry once.
/// It has no pending-byte state and no production-style per-word reduction.
/// </summary>
internal static class InternetChecksumReference
{
    public static ushort Compute(ReadOnlySpan<byte> source)
    {
        ulong high = 0;
        ulong low = 0;
        for (int index = 0; index < source.Length; index++)
        {
            if ((index & 1) == 0) high += source[index];
            else low += source[index];
        }

        ulong sum = (high << 8) + low;
        while ((sum >> 16) != 0) sum = (sum & 0xFFFF) + (sum >> 16);
        return (ushort)(sum ^ 0xFFFF);
    }
}
