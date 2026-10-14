using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{
    internal struct InternetChecksumCore
    {
        private uint _sum;
        private byte _pendingHighByte;
        private bool _hasPendingHighByte;

        internal static ushort Compute(IEnumerable<byte> source)
        {
            var state = new InternetChecksumCore();
            if (source is byte[] bytes)
                state.Append(bytes);
            else
                foreach (byte value in source)
                    state.Update(value);
            return state.GetCurrentValue();
        }

        internal void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;

            int offset = 0;
            if (_hasPendingHighByte)
            {
                AddWord((uint)(_pendingHighByte << 8) | source[0]);
                _hasPendingHighByte = false;
                offset = 1;
            }

            while (source.Length - offset >= sizeof(ushort))
            {
                AddWord((uint)(source[offset] << 8) | source[offset + 1]);
                offset += sizeof(ushort);
            }

            if (offset < source.Length)
            {
                _pendingHighByte = source[offset];
                _hasPendingHighByte = true;
            }
        }

        internal void Update(byte value)
        {
            if (!_hasPendingHighByte)
            {
                _pendingHighByte = value;
                _hasPendingHighByte = true;
                return;
            }

            AddWord((uint)(_pendingHighByte << 8) | value);
            _hasPendingHighByte = false;
        }

        internal ushort GetCurrentValue()
        {
            uint sum = _sum;
            if (_hasPendingHighByte)
                sum += (uint)(_pendingHighByte << 8);
            sum = (sum & ushort.MaxValue) + (sum >> 16);
            return (ushort)~sum;
        }

        private void AddWord(uint value)
        {
            // Folding every word bounds the sum at 0xFFFF for inputs of any length.
            uint sum = _sum + value;
            _sum = (sum & ushort.MaxValue) + (sum >> 16);
        }
    }
}
