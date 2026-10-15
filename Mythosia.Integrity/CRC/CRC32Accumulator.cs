using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>
    /// Computes a CRC32 value incrementally without retaining the appended data.
    /// </summary>
    /// <remarks>
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// The value for an empty sequence is 0xFFFFFFFF for MPEG-2 and zero for other algorithms.
    /// </remarks>
    public sealed class CRC32Accumulator
    {
        private const int BitsPerByte = 8;
        private const int TableLength = 1 << BitsPerByte;
        private const uint ReflectedPolynomial = 0xEDB88320;
        private const uint InitialState = uint.MaxValue;
        private const uint UnreflectedPolynomial = 0x04C11DB7;
        private const uint AutosarReflectedPolynomial = 0xC8DF352F;

        // The table is generated once, fully initialized, and never modified or exposed.
        private static readonly uint[] Table = GenerateReflectedTable(ReflectedPolynomial);
        private static readonly uint[] UnreflectedTable = GenerateUnreflectedTable();
        private static readonly uint[] AutosarTable = GenerateReflectedTable(AutosarReflectedPolynomial);

        private readonly uint[] _table;
        private readonly bool _reflected;
        private readonly uint _finalXor;

        private uint _state = InitialState;

        /// <summary>
        /// Gets the CRC32 algorithm selected when this accumulator was created.
        /// </summary>
        public CRC32Type Type { get; }

        /// <summary>
        /// Creates an empty accumulator for the selected CRC32 algorithm.
        /// </summary>
        /// <param name="type">The algorithm to use. The default is <see cref="CRC32Type.Classic"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not a defined CRC32 type.</exception>
        public CRC32Accumulator(CRC32Type type = CRC32Type.Classic)
        {
#pragma warning disable CS0618 // Continue supporting the legacy Basic algorithm name.
            _table = type switch
            {
                CRC32Type.Basic => Table,
                CRC32Type.Classic => Table,
                CRC32Type.Mpeg2 => UnreflectedTable,
                CRC32Type.Bzip2 => UnreflectedTable,
                CRC32Type.AutosarP4 => AutosarTable,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CRC32 algorithm.")
            };
#pragma warning restore CS0618
            Type = type;
            _reflected = type != CRC32Type.Mpeg2 && type != CRC32Type.Bzip2;
            _finalXor = type == CRC32Type.Mpeg2 ? 0 : uint.MaxValue;
        }

        /// <summary>
        /// Appends bytes to the current calculation without retaining the source.
        /// </summary>
        /// <param name="source">The next bytes in the sequence. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;

            uint state = _state;
            if (_reflected)
            {
                foreach (byte value in source)
                    state = (state >> BitsPerByte) ^ _table[(state ^ value) & byte.MaxValue];
            }
            else
            {
                foreach (byte value in source)
                    state = (state << BitsPerByte) ^ _table[(state >> 24) ^ value];
            }

            _state = state;
        }

        /// <summary>
        /// Gets the CRC32 value for all appended bytes without changing the calculation state.
        /// </summary>
        /// <returns>The current CRC32 value; an empty sequence returns 0xFFFFFFFF for MPEG-2 and zero otherwise. More bytes may be appended afterward.</returns>
        public uint GetCurrentValue() => _state ^ _finalXor;

        /// <summary>
        /// Restores the empty initial state while keeping the selected algorithm.
        /// </summary>
        public void Reset() => _state = InitialState;

        private static uint[] GenerateReflectedTable(uint polynomial)
        {
            uint[] table = new uint[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                uint value = (uint)index;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = (value & 1) != 0
                        ? (value >> 1) ^ polynomial
                        : value >> 1;
                }

                table[index] = value;
            }

            return table;
        }

        private static uint[] GenerateUnreflectedTable()
        {
            uint[] table = new uint[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                uint value = (uint)index << 24;
                for (int bit = 0; bit < BitsPerByte; bit++)
                    value = (value & 0x80000000u) != 0
                        ? (value << 1) ^ UnreflectedPolynomial
                        : value << 1;

                table[index] = value;
            }

            return table;
        }
    }
}
