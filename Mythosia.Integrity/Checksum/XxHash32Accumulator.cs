using System;
using System.Buffers.Binary;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates XXH32 incrementally with a fixed 16-byte holdback buffer.</summary>
    /// <remarks>Snapshots do not change the state. An instance is not thread-safe.</remarks>
    public sealed class XxHash32Accumulator
    {
        private const uint Prime1 = 2654435761u;
        private const uint Prime2 = 2246822519u;
        private const uint Prime3 = 3266489917u;
        private const uint Prime4 = 668265263u;
        private const uint Prime5 = 374761393u;
        private const int StripeLength = 16;
        private readonly byte[] _tail = new byte[StripeLength];
        private uint _v1, _v2, _v3, _v4;
        private ulong _length;
        private int _tailLength;
        private bool _hasStripes;

        /// <summary>Creates an empty XXH32 accumulator using the supplied seed.</summary>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        public XxHash32Accumulator(uint seed = 0)
        {
            Seed = seed;
            Reset();
        }

        /// <summary>Gets the seed retained when the accumulator is reset.</summary>
        public uint Seed { get; }

        /// <summary>Consumes the next bytes immediately without retaining the source.</summary>
        /// <param name="source">The next bytes. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {
            if (source.IsEmpty) return;
            _length = unchecked(_length + (ulong)source.Length);
            if (_tailLength != 0)
            {
                int count = Math.Min(StripeLength - _tailLength, source.Length);
                source.Slice(0, count).CopyTo(_tail.AsSpan(_tailLength));
                _tailLength += count;
                source = source.Slice(count);
                if (_tailLength < StripeLength) return;
                ProcessStripe(_tail);
                _tailLength = 0;
            }
            while (source.Length >= StripeLength)
            {
                ProcessStripe(source);
                source = source.Slice(StripeLength);
            }
            source.CopyTo(_tail);
            _tailLength = source.Length;
        }

        /// <summary>Gets the numeric XXH32 value without changing the calculation.</summary>
        /// <returns>The hash of every byte appended so far using the selected seed.</returns>
        public uint GetCurrentValue()
        {
            unchecked
            {
                uint result = _hasStripes
                    ? Rotate(_v1, 1) + Rotate(_v2, 7) + Rotate(_v3, 12) + Rotate(_v4, 18)
                    : Seed + Prime5;
                result += (uint)_length;
                int offset = 0;
                while (offset + sizeof(uint) <= _tailLength)
                {
                    result = Rotate(result + BinaryPrimitives.ReadUInt32LittleEndian(_tail.AsSpan(offset)) * Prime3, 17) * Prime4;
                    offset += sizeof(uint);
                }
                while (offset < _tailLength)
                    result = Rotate(result + _tail[offset++] * Prime5, 11) * Prime1;
                result ^= result >> 15;
                result *= Prime2;
                result ^= result >> 13;
                result *= Prime3;
                return result ^ (result >> 16);
            }
        }

        /// <summary>Restores the empty state using the original seed.</summary>
        public void Reset()
        {
            unchecked
            {
                _v1 = Seed + Prime1 + Prime2;
                _v2 = Seed + Prime2;
                _v3 = Seed;
                _v4 = Seed - Prime1;
            }
            _length = 0;
            _tailLength = 0;
            _hasStripes = false;
        }

        internal void AppendByte(byte value)
        {
            _length = unchecked(_length + 1);
            _tail[_tailLength++] = value;
            if (_tailLength != StripeLength) return;
            ProcessStripe(_tail);
            _tailLength = 0;
        }

        private void ProcessStripe(ReadOnlySpan<byte> stripe)
        {
            _v1 = Round(_v1, BinaryPrimitives.ReadUInt32LittleEndian(stripe));
            _v2 = Round(_v2, BinaryPrimitives.ReadUInt32LittleEndian(stripe.Slice(4)));
            _v3 = Round(_v3, BinaryPrimitives.ReadUInt32LittleEndian(stripe.Slice(8)));
            _v4 = Round(_v4, BinaryPrimitives.ReadUInt32LittleEndian(stripe.Slice(12)));
            _hasStripes = true;
        }

        private static uint Round(uint lane, uint word) =>
            unchecked(Rotate(lane + word * Prime2, 13) * Prime1);

        private static uint Rotate(uint value, int count) => (value << count) | (value >> (32 - count));
    }
}
