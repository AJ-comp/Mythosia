using System.Numerics;

namespace Mythosia.Test.Integrity;

// Independent, whole-message specification oracle. BigInteger arithmetic and explicit masking
// model modular words; it has no holdback buffer, incremental state, or production helpers.
internal static class XxHashReference
{
    internal static ulong Compute(ReadOnlySpan<byte> input, int width, ulong seed = 0)
    {
        BigInteger mask = (BigInteger.One << width) - 1;
        BigInteger[] primes = width == 32
            ? new BigInteger[] { 2654435761, 2246822519, 3266489917, 668265263, 374761393 }
            : new BigInteger[] { 11400714785074694791ul, 14029467366897019727ul, 1609587929392839161ul, 9650029242287828579ul, 2870177450012600261ul };
        BigInteger p1 = primes[0], p2 = primes[1], p3 = primes[2], p4 = primes[3], p5 = primes[4];
        BigInteger Rotate(BigInteger value, int amount)
        {
            value &= mask;
            return ((value << amount) | (value >> (width - amount))) & mask;
        }
        BigInteger Round(BigInteger lane, BigInteger word) =>
            (Rotate(lane + word * p2, width == 32 ? 13 : 31) * p1) & mask;
        BigInteger Merge(BigInteger result, BigInteger lane) => ((result ^ Round(0, lane)) * p1 + p4) & mask;

        int wordBytes = width / 8;
        int stripeBytes = 4 * wordBytes;
        int position = 0;
        BigInteger value;
        if (input.Length >= stripeBytes)
        {
            BigInteger[] lanes = { (seed + p1 + p2) & mask, (seed + p2) & mask, seed, (seed - p1) & mask };
            int stripes = input.Length / stripeBytes;
            for (int stripe = 0; stripe < stripes; stripe++)
            for (int lane = 0; lane < 4; lane++)
            {
                lanes[lane] = Round(lanes[lane], ReadWord(input.Slice(position, wordBytes)));
                position += wordBytes;
            }
            value = (Rotate(lanes[0], 1) + Rotate(lanes[1], 7) + Rotate(lanes[2], 12) + Rotate(lanes[3], 18)) & mask;
            if (width == 64)
                foreach (BigInteger lane in lanes) value = Merge(value, lane);
        }
        else value = (seed + p5) & mask;
        value = (value + input.Length) & mask;
        if (width == 64)
        {
            while (position + 8 <= input.Length)
            {
                value ^= Round(0, ReadWord(input.Slice(position, 8)));
                value = (Rotate(value, 27) * p1 + p4) & mask;
                position += 8;
            }
            if (position + 4 <= input.Length)
            {
                value ^= (ReadWord(input.Slice(position, 4)) * p1) & mask;
                value = (Rotate(value, 23) * p2 + p3) & mask;
                position += 4;
            }
        }
        else
        {
            while (position + 4 <= input.Length)
            {
                value = (Rotate(value + ReadWord(input.Slice(position, 4)) * p3, 17) * p4) & mask;
                position += 4;
            }
        }
        while (position < input.Length)
        {
            value = width == 32
                ? (Rotate(value + input[position] * p5, 11) * p1) & mask
                : (Rotate(value ^ ((input[position] * p5) & mask), 11) * p1) & mask;
            position++;
        }
        value ^= value >> (width == 32 ? 15 : 33);
        value = (value * p2) & mask;
        value ^= value >> (width == 32 ? 13 : 29);
        value = (value * p3) & mask;
        value ^= value >> (width == 32 ? 16 : 32);
        return (ulong)value;
    }

    private static BigInteger ReadWord(ReadOnlySpan<byte> bytes)
    {
        BigInteger result = 0;
        for (int i = bytes.Length - 1; i >= 0; i--) result = (result << 8) | bytes[i];
        return result;
    }
}
