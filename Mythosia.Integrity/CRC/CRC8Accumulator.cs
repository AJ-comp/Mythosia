using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>
    /// Computes a CRC8 value incrementally without retaining the appended data.
    /// </summary>
    /// <remarks>
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// No CRC8 value is available until a nonempty span has been appended.
    /// </remarks>
    public sealed class CRC8Accumulator
    {
        private const int BitsPerByte = 8;
        private const int TableLength = 1 << BitsPerByte;
        private const byte MostSignificantBit = 0x80;
        private const byte ClassicPolynomial = 0x07;
        private const byte MaximReflectedPolynomial = 0x8C;
        private const byte SaeJ1850Polynomial = 0x1D;
        private const byte AutosarH2FPolynomial = 0x2F;
        private const byte SensirionPolynomial = 0x31;

        // Tables are generated once, fully initialized, and never modified or exposed.
        private static readonly byte[] ClassicTable = GenerateUnreflectedTable(ClassicPolynomial);
        private static readonly byte[] MaximTable = GenerateMaximTable();
        private static readonly byte[] SaeJ1850Table = GenerateUnreflectedTable(SaeJ1850Polynomial);
        private static readonly byte[] AutosarH2FTable = GenerateUnreflectedTable(AutosarH2FPolynomial);
        private static readonly byte[] SensirionTable = GenerateUnreflectedTable(SensirionPolynomial);

        private readonly byte[] _table;
        private readonly byte _initialState;
        private readonly byte _finalXor;
        private byte _state;
        private bool _hasData;

        /// <summary>
        /// Gets the CRC8 algorithm selected when this accumulator was created.
        /// </summary>
        public CRC8Type Type { get; }

        /// <summary>
        /// Creates an empty accumulator for the selected CRC8 algorithm.
        /// </summary>
        /// <param name="type">The algorithm to use. The default is <see cref="CRC8Type.Classic"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not a defined CRC8 type.</exception>
        public CRC8Accumulator(CRC8Type type = CRC8Type.Classic)
        {
#pragma warning disable CS0618 // Continue supporting the legacy Basic algorithm name.
            _table = type switch
            {
                CRC8Type.Basic => ClassicTable,
                CRC8Type.Classic => ClassicTable,
                CRC8Type.Maxim => MaximTable,
                CRC8Type.SaeJ1850 => SaeJ1850Table,
                CRC8Type.AutosarH2F => AutosarH2FTable,
                CRC8Type.SensirionSht4x => SensirionTable,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CRC8 algorithm.")
            };
#pragma warning restore CS0618
            Type = type;
            _initialState = type == CRC8Type.SaeJ1850 || type == CRC8Type.AutosarH2F || type == CRC8Type.SensirionSht4x
                ? byte.MaxValue
                : (byte)0;
            _finalXor = type == CRC8Type.SaeJ1850 || type == CRC8Type.AutosarH2F
                ? byte.MaxValue
                : (byte)0;
            _state = _initialState;
        }

        /// <summary>
        /// Appends bytes to the current calculation without retaining the source.
        /// </summary>
        /// <param name="source">The next bytes in the sequence. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;

            byte state = _state;
            foreach (byte value in source)
                state = _table[state ^ value];

            _state = state;
            _hasData = true;
        }

        /// <summary>
        /// Gets the CRC8 value for all appended bytes without changing the calculation state.
        /// </summary>
        /// <returns>
        /// The current CRC8 value. SAE-J1850 and AUTOSAR H2F apply a final XOR of 0xFF
        /// without changing the calculation state. More bytes may be appended after reading it.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// No nonempty span has been appended since construction or the last reset.
        /// </exception>
        public byte GetCurrentValue()
        {
            if (!_hasData)
                throw new InvalidOperationException("No data has been appended.");

            return (byte)(_state ^ _finalXor);
        }

        /// <summary>
        /// Restores the empty initial state while keeping the selected algorithm.
        /// </summary>
        public void Reset()
        {
            _state = _initialState;
            _hasData = false;
        }

        private static byte[] GenerateUnreflectedTable(byte polynomial)
        {
            byte[] table = new byte[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                byte value = (byte)index;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = unchecked((byte)((value & MostSignificantBit) != 0
                        ? (value << 1) ^ polynomial
                        : value << 1));
                }

                table[index] = value;
            }

            return table;
        }

        private static byte[] GenerateMaximTable()
        {
            byte[] table = new byte[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                byte value = (byte)index;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = (byte)((value & 1) != 0
                        ? (value >> 1) ^ MaximReflectedPolynomial
                        : value >> 1);
                }

                table[index] = value;
            }

            return table;
        }
    }
}
