using System;

namespace Mythosia.Integrity.CRC
{
    /// <summary>
    /// Computes a CRC16 value incrementally without retaining the appended data.
    /// </summary>
    /// <remarks>
    /// Separate instances can be used independently. An instance is not thread-safe.
    /// DNP results preserve the complement and byte swap used by the existing CRC16 API.
    /// </remarks>
    public sealed class CRC16Accumulator
    {
        private const int BitsPerByte = 8;
        private const int TableLength = 1 << BitsPerByte;
        private const ushort MostSignificantBit = 0x8000;
        private const ushort ClassicReflectedPolynomial = 0xA001;
        private const ushort XModemPolynomial = 0x1021;
        private const ushort DnpReflectedPolynomial = 0xA6BC;
        private const ushort X25ReflectedPolynomial = 0x8408;

        // Classic and Modbus share a polynomial; only their initial states differ.
        // Tables are generated once, fully initialized, and never modified or exposed.
        private static readonly ushort[] ClassicTable = GenerateReflectedTable(ClassicReflectedPolynomial);
        private static readonly ushort[] XModemTable = GenerateXModemTable();
        private static readonly ushort[] DnpTable = GenerateReflectedTable(DnpReflectedPolynomial);
        private static readonly ushort[] X25Table = GenerateReflectedTable(X25ReflectedPolynomial);

        private readonly ushort[] _table;
        private readonly ushort _initialState;
        private ushort _state;
        private bool _hasData;

        /// <summary>
        /// Gets the CRC16 algorithm selected when this accumulator was created.
        /// </summary>
        public CRC16Type Type { get; }

        /// <summary>
        /// Creates an empty accumulator for the selected CRC16 algorithm.
        /// </summary>
        /// <param name="type">The algorithm to use. The default is <see cref="CRC16Type.Classic"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not a defined CRC16 type.</exception>
        public CRC16Accumulator(CRC16Type type = CRC16Type.Classic)
        {
#pragma warning disable CS0618 // Continue supporting the legacy Basic algorithm name.
            _table = type switch
            {
                CRC16Type.Basic => ClassicTable,
                CRC16Type.Classic => ClassicTable,
                CRC16Type.Modbus => ClassicTable,
                CRC16Type.CCITTxModem => XModemTable,
                CRC16Type.DNP => DnpTable,
                CRC16Type.CCITTFalse => XModemTable,
                CRC16Type.X25 => X25Table,
                CRC16Type.MCRF4XX => X25Table,
                CRC16Type.USB => ClassicTable,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unknown CRC16 algorithm.")
            };
#pragma warning restore CS0618
            Type = type;
            _initialState = type == CRC16Type.Modbus || type == CRC16Type.CCITTFalse || type == CRC16Type.X25
                || type == CRC16Type.MCRF4XX || type == CRC16Type.USB
                ? ushort.MaxValue
                : (ushort)0;
            _state = _initialState;
        }

        /// <summary>
        /// Appends bytes to the current calculation without retaining the source.
        /// </summary>
        /// <param name="source">The next bytes in the sequence. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;

            ushort state = _state;
            if (Type == CRC16Type.CCITTxModem || Type == CRC16Type.CCITTFalse)
            {
                foreach (byte value in source)
                    state = unchecked((ushort)((state << BitsPerByte) ^ _table[(state >> BitsPerByte) ^ value]));
            }
            else
            {
                foreach (byte value in source)
                    state = (ushort)((state >> BitsPerByte) ^ _table[(state ^ value) & byte.MaxValue]);
            }

            _state = state;
            _hasData = true;
        }

        /// <summary>
        /// Gets the CRC16 value for all appended bytes without changing the calculation state.
        /// </summary>
        /// <returns>
        /// The current CRC16 value. With no appended bytes, Modbus, CCITTFalse and MCRF4XX return 0xFFFF
        /// and other algorithms return zero. X25 and USB apply a final XOR of 0xFFFF without changing
        /// the calculation state. Nonempty DNP results use the legacy complement and byte swap.
        /// More bytes may be appended after reading the value.
        /// </returns>
        public ushort GetCurrentValue()
        {
            if (Type == CRC16Type.X25 || Type == CRC16Type.USB) return unchecked((ushort)~_state);
            if (Type != CRC16Type.DNP || !_hasData) return _state;

            ushort complemented = unchecked((ushort)~_state);
            return unchecked((ushort)((complemented << BitsPerByte) | (complemented >> BitsPerByte)));
        }

        /// <summary>
        /// Restores the empty initial state while keeping the selected algorithm.
        /// </summary>
        public void Reset()
        {
            _state = _initialState;
            _hasData = false;
        }

        private static ushort[] GenerateReflectedTable(ushort polynomial)
        {
            ushort[] table = new ushort[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                ushort value = (ushort)index;
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = (ushort)((value & 1) != 0
                        ? (value >> 1) ^ polynomial
                        : value >> 1);
                }

                table[index] = value;
            }

            return table;
        }

        private static ushort[] GenerateXModemTable()
        {
            ushort[] table = new ushort[TableLength];
            for (int index = 0; index < table.Length; index++)
            {
                ushort value = (ushort)(index << BitsPerByte);
                for (int bit = 0; bit < BitsPerByte; bit++)
                {
                    value = unchecked((ushort)((value & MostSignificantBit) != 0
                        ? (value << 1) ^ XModemPolynomial
                        : value << 1));
                }

                table[index] = value;
            }

            return table;
        }
    }
}
