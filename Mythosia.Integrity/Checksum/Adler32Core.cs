using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    internal static class Adler32Core
    {
        internal const uint InitialState = 1;

        private const uint Modulus = 65521;
        private const int HalfWidth = 16;
        private const uint HalfMask = ushort.MaxValue;

        // With both sums reduced, 5,552 bytes of 0xFF cannot overflow the second uint sum.
        private const int MaximumBlockLength = 5552;

        internal static uint Compute(IEnumerable<byte> source)
        {
            if (source is byte[] bytes) return Append(InitialState, bytes);

            uint state = InitialState;
            foreach (byte value in source)
                state = Update(state, value);
            return state;
        }

        internal static uint Append(uint state, ReadOnlySpan<byte> source)
        {
            uint first = state & HalfMask;
            uint second = state >> HalfWidth;
            while (!source.IsEmpty)
            {
                int blockLength = Math.Min(source.Length, MaximumBlockLength);
                foreach (byte value in source.Slice(0, blockLength))
                {
                    first += value;
                    second += first;
                }
                first %= Modulus;
                second %= Modulus;
                source = source.Slice(blockLength);
            }
            return (second << HalfWidth) | first;
        }

        internal static uint Update(uint state, byte value)
        {
            uint first = (state & HalfMask) + value;
            if (first >= Modulus) first -= Modulus;

            uint second = (state >> HalfWidth) + first;
            if (second >= Modulus) second -= Modulus;
            return (second << HalfWidth) | first;
        }
    }
}
