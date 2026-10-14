using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.CRC
{
    internal static class CRC32CCore
    {
        internal const uint InitialState = uint.MaxValue;

        private const int BitsPerByte = 8;
        private const uint ReflectedPolynomial = 0x82F63B78;

        // CLR initialization publishes only the completed table, which is never exposed or changed.
        private static readonly uint[] Table = GenerateTable();

        internal static uint Compute(IEnumerable<byte> source)
        {
            if (source is byte[] bytes) return ~Append(InitialState, bytes);

            uint state = InitialState;
            foreach (byte value in source)
                state = Update(state, value);
            return ~state;
        }

        internal static uint Append(uint state, ReadOnlySpan<byte> source)
        {
            foreach (byte value in source)
                state = Update(state, value);
            return state;
        }

        internal static uint Update(uint state, byte value) =>
            (state >> BitsPerByte) ^ Table[(state ^ value) & byte.MaxValue];

        private static uint[] GenerateTable()
        {
            var table = new uint[1 << BitsPerByte];
            for (int index = 0; index < table.Length; index++)
            {
                uint value = (uint)index;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = (value & 1) != 0
                        ? (value >> 1) ^ ReflectedPolynomial
                        : value >> 1;
                }
                table[index] = value;
            }
            return table;
        }
    }
}
