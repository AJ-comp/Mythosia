using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.CRC
{
    internal static class CRC64Core
    {
        internal const ulong InitialState = ulong.MaxValue;

        private const int BitsPerByte = 8;
        private const ulong ReflectedPolynomial = 0x9A6C9329AC4BC9B5UL;

        // CLR initialization publishes only the completed table, which is never exposed or changed.
        private static readonly ulong[] Table = GenerateTable();

        internal static void ValidateType(CRC64Type type)
        {
            if (type != CRC64Type.NVME)
                throw new ArgumentOutOfRangeException(nameof(type), type, "The CRC64 algorithm is not supported.");
        }

        internal static ulong Compute(IEnumerable<byte> source)
        {
            if (source is byte[] bytes) return ~Append(InitialState, bytes);

            ulong state = InitialState;
            foreach (byte value in source)
                state = Update(state, value);
            return ~state;
        }

        internal static ulong Append(ulong state, ReadOnlySpan<byte> source)
        {
            foreach (byte value in source)
                state = Update(state, value);
            return state;
        }

        internal static ulong Update(ulong state, byte value) =>
            (state >> BitsPerByte) ^ Table[(state ^ value) & byte.MaxValue];

        private static ulong[] GenerateTable()
        {
            var table = new ulong[1 << BitsPerByte];
            for (int index = 0; index < table.Length; index++)
            {
                ulong value = (ulong)index;
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
