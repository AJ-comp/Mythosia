using System;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates OpenZFS Fletcher-4 over 32-bit input words using four wrapping 64-bit sums.</summary>
    /// <remarks>
    /// Keeps at most three pending bytes across appends. A result requires a total byte count divisible by four;
    /// incomplete words are never padded. A failed result query preserves the state for a subsequent append.
    /// Instances are independent and are not thread-safe.
    /// </remarks>
    public sealed class Fletcher4Accumulator
    {
        private ulong _a, _b, _c, _d;
        private uint _pendingWord;
        private int _pendingBytes;

        /// <summary>Creates an accumulator with an explicit input-word byte order.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The byte order is not supported.</exception>
        public Fletcher4Accumulator(Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian)
        {
            ValidateByteOrder(byteOrder);
            ByteOrder = byteOrder;
        }

        /// <summary>Gets the byte order used for the input's 32-bit words.</summary>
        public Fletcher4ByteOrder ByteOrder { get; }

        /// <summary>Consumes the next bytes without retaining the source; partial words survive append boundaries.</summary>
        public void Append(ReadOnlySpan<byte> source)
        {
            foreach (byte value in source) AppendByte(value);
        }

        /// <summary>Gets the four sums without modifying the state; empty input produces four zeros.</summary>
        /// <exception cref="InvalidOperationException">The total input length is not divisible by four.</exception>
        public Fletcher4Value GetCurrentValue()
        {
            if (_pendingBytes != 0)
                throw new InvalidOperationException("Fletcher-4 requires a total input length divisible by four bytes.");
            return new Fletcher4Value(_a, _b, _c, _d);
        }

        /// <summary>Clears all sums and pending bytes while retaining the selected byte order.</summary>
        public void Reset()
        {
            _a = _b = _c = _d = 0;
            _pendingWord = 0;
            _pendingBytes = 0;
        }

        internal void AppendByte(byte value)
        {
            int shift = ByteOrder == Fletcher4ByteOrder.LittleEndian ? _pendingBytes * 8 : (3 - _pendingBytes) * 8;
            _pendingWord |= (uint)value << shift;
            if (++_pendingBytes != 4) return;

            unchecked
            {
                _a += _pendingWord;
                _b += _a;
                _c += _b;
                _d += _c;
            }
            _pendingWord = 0;
            _pendingBytes = 0;
        }

        internal static void ValidateByteOrder(Fletcher4ByteOrder byteOrder)
        {
            if (byteOrder != Fletcher4ByteOrder.LittleEndian && byteOrder != Fletcher4ByteOrder.BigEndian)
                throw new ArgumentOutOfRangeException(nameof(byteOrder), byteOrder, "Unsupported Fletcher-4 input word byte order.");
        }
    }
}
