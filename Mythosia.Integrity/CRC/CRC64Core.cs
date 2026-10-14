using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.CRC
{
    internal static class CRC64Core
    {
        private const int BitsPerByte = 8;
        private const int MostSignificantByteShift = 64 - BitsPerByte;
        private const ulong NvmeReflectedPolynomial = 0x9A6C9329AC4BC9B5UL;
        private const ulong EcmaPolynomial = 0x42F0E1EBA9EA3693UL;
        private const ulong XzReflectedPolynomial = 0xC96C5795D7870F42UL;

        // CLR initialization publishes only completed tables, which are never exposed or changed.
        private static readonly ulong[] NvmeTable = GenerateTable(NvmeReflectedPolynomial, reflected: true);
        private static readonly ulong[] EcmaTable = GenerateTable(EcmaPolynomial, reflected: false);
        private static readonly ulong[] XzTable = GenerateTable(XzReflectedPolynomial, reflected: true);

        internal static void ValidateType(CRC64Type type)
        {
            if (type != CRC64Type.NVME && type != CRC64Type.ECMA182 && type != CRC64Type.XZ)
                throw new ArgumentOutOfRangeException(nameof(type), type, "The CRC64 algorithm is not supported.");
        }

        internal static ulong InitialState(CRC64Type type) => type == CRC64Type.ECMA182 ? 0UL : ulong.MaxValue;

        internal static ulong Finalize(ulong state, CRC64Type type) => type == CRC64Type.ECMA182 ? state : ~state;

        internal static ulong Compute(IEnumerable<byte> source, CRC64Type type)
        {
            if (source is byte[] bytes) return Finalize(Append(InitialState(type), bytes, type), type);

            ulong state = InitialState(type);
            foreach (byte value in source)
                state = Update(state, value, type);
            return Finalize(state, type);
        }

        internal static ulong Append(ulong state, ReadOnlySpan<byte> source, CRC64Type type)
        {
            if (type == CRC64Type.ECMA182)
            {
                foreach (byte value in source)
                    state = (state << BitsPerByte) ^ EcmaTable[(state >> MostSignificantByteShift) ^ value];
                return state;
            }

            ulong[] table = type == CRC64Type.XZ ? XzTable : NvmeTable;
            foreach (byte value in source)
                state = (state >> BitsPerByte) ^ table[(state ^ value) & byte.MaxValue];
            return state;
        }

        internal static ulong Update(ulong state, byte value, CRC64Type type)
        {
            if (type == CRC64Type.ECMA182)
                return (state << BitsPerByte) ^ EcmaTable[(state >> MostSignificantByteShift) ^ value];
            ulong[] table = type == CRC64Type.XZ ? XzTable : NvmeTable;
            return (state >> BitsPerByte) ^ table[(state ^ value) & byte.MaxValue];
        }

        private static ulong[] GenerateTable(ulong polynomial, bool reflected)
        {
            var table = new ulong[1 << BitsPerByte];
            for (int index = 0; index < table.Length; index++)
            {
                ulong value = reflected ? (ulong)index : (ulong)index << MostSignificantByteShift;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    if (reflected)
                        value = (value & 1) != 0 ? (value >> 1) ^ polynomial : value >> 1;
                    else
                        value = (value & 0x8000000000000000UL) != 0 ? (value << 1) ^ polynomial : value << 1;
                }
                table[index] = value;
            }
            return table;
        }
    }
}
