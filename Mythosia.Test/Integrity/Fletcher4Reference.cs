using System.Numerics;
using Mythosia.Integrity.Checksum;

namespace Mythosia.Test.Integrity;

internal static class Fletcher4Reference
{
    // Each word's closed-form contribution to each sum, calculated with unbounded integers.
    // This is independent of the product's four sequential wrapping additions.
    internal static Fletcher4Value Compute(ReadOnlySpan<byte> data, Fletcher4ByteOrder byteOrder)
    {
        if (data.Length % 4 != 0) throw new ArgumentException("Unaligned input.", nameof(data));
        BigInteger a = 0, b = 0, c = 0, d = 0;
        int count = data.Length / 4;
        for (int index = 0; index < count; index++)
        {
            BigInteger word = 0;
            for (int part = 0; part < 4; part++)
            {
                int position = byteOrder == Fletcher4ByteOrder.LittleEndian ? 3 - part : part;
                word = word * 256 + data[index * 4 + position];
            }
            BigInteger remaining = count - index;
            a += word;
            b += word * remaining;
            c += word * remaining * (remaining + 1) / 2;
            d += word * remaining * (remaining + 1) * (remaining + 2) / 6;
        }
        BigInteger mask = (BigInteger.One << 64) - 1;
        return new Fletcher4Value((ulong)(a & mask), (ulong)(b & mask), (ulong)(c & mask), (ulong)(d & mask));
    }
}
