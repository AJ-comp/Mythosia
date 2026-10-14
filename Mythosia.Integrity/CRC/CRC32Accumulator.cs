using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>
    /// Computes a CRC32 value incrementally without retaining the appended data.
    /// </summary>
    /// <remarks>
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// The value for an empty sequence is zero.
    /// </remarks>
    public sealed class CRC32Accumulator
    {
        private const int BitsPerByte = 8;
        private const int TableLength = 1 << BitsPerByte;
        private const uint ReflectedPolynomial = 0xEDB88320;
        private const uint InitialState = uint.MaxValue;

        // The table is generated once, fully initialized, and never modified or exposed.
        private static readonly uint[] Table = GenerateTable();

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
            if (type != CRC32Type.Basic && type != CRC32Type.Classic)
                throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CRC32 algorithm.");
#pragma warning restore CS0618
            Type = type;
        }

        /// <summary>
        /// Appends bytes to the current calculation without retaining the source.
        /// </summary>
        /// <param name="source">The next bytes in the sequence. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;

            uint state = _state;
            foreach (byte value in source)
                state = (state >> BitsPerByte) ^ Table[(state ^ value) & byte.MaxValue];

            _state = state;
        }

        /// <summary>
        /// Gets the CRC32 value for all appended bytes without changing the calculation state.
        /// </summary>
        /// <returns>The current CRC32 value, or zero for an empty sequence. More bytes may be appended afterward.</returns>
        public uint GetCurrentValue() => ~_state;

        /// <summary>
        /// Restores the empty initial state while keeping the selected algorithm.
        /// </summary>
        public void Reset() => _state = InitialState;

        private static uint[] GenerateTable()
        {
            uint[] table = new uint[TableLength];
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
