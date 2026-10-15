using System;
using System.Buffers.Binary;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates XXH64 incrementally with a fixed 32-byte holdback buffer.</summary>
    /// <remarks>Snapshots do not change the state. An instance is not thread-safe.</remarks>
    public sealed class XxHash64Accumulator
    {
        private const ulong Prime1 = 11400714785074694791ul;
        private const ulong Prime2 = 14029467366897019727ul;
        private const ulong Prime3 = 1609587929392839161ul;
        private const ulong Prime4 = 9650029242287828579ul;
        private const ulong Prime5 = 2870177450012600261ul;
        private const int StripeLength = 32;
        private readonly byte[] _tail = new byte[StripeLength];
        private ulong _v1, _v2, _v3, _v4;
        private ulong _length;
        private int _tailLength;
        private bool _hasStripes;

        /// <summary>Creates an empty XXH64 accumulator using the supplied seed.</summary>
        /// <param name="seed">The XXH64 seed, zero by default.</param>
        public XxHash64Accumulator(ulong seed = 0)
        {
            Seed = seed;
            Reset();
        }

        /// <summary>Gets the seed retained when the accumulator is reset.</summary>
        public ulong Seed { get; }

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

        /// <summary>Gets the numeric XXH64 value without changing the calculation.</summary>
        /// <returns>The hash of every byte appended so far using the selected seed.</returns>
        public ulong GetCurrentValue()
        {
            unchecked
            {
                ulong result;
                if (_hasStripes)
                {
                    result = Rotate(_v1, 1) + Rotate(_v2, 7) + Rotate(_v3, 12) + Rotate(_v4, 18);
                    result = Merge(result, _v1);
                    result = Merge(result, _v2);
                    result = Merge(result, _v3);
                    result = Merge(result, _v4);
                }
                else result = Seed + Prime5;
                result += _length;
                int offset = 0;
                while (offset + sizeof(ulong) <= _tailLength)
                {
                    result ^= Round(0, BinaryPrimitives.ReadUInt64LittleEndian(_tail.AsSpan(offset)));
                    result = Rotate(result, 27) * Prime1 + Prime4;
                    offset += sizeof(ulong);
                }
                if (offset + sizeof(uint) <= _tailLength)
                {
                    result ^= BinaryPrimitives.ReadUInt32LittleEndian(_tail.AsSpan(offset)) * Prime1;
                    result = Rotate(result, 23) * Prime2 + Prime3;
                    offset += sizeof(uint);
                }
                while (offset < _tailLength)
                {
                    result ^= _tail[offset++] * Prime5;
                    result = Rotate(result, 11) * Prime1;
                }
                result ^= result >> 33;
                result *= Prime2;
                result ^= result >> 29;
                result *= Prime3;
                return result ^ (result >> 32);
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
            _v1 = Round(_v1, BinaryPrimitives.ReadUInt64LittleEndian(stripe));
            _v2 = Round(_v2, BinaryPrimitives.ReadUInt64LittleEndian(stripe.Slice(8)));
            _v3 = Round(_v3, BinaryPrimitives.ReadUInt64LittleEndian(stripe.Slice(16)));
            _v4 = Round(_v4, BinaryPrimitives.ReadUInt64LittleEndian(stripe.Slice(24)));
            _hasStripes = true;
        }

        private static ulong Round(ulong lane, ulong word) =>
            unchecked(Rotate(lane + word * Prime2, 31) * Prime1);

        private static ulong Rotate(ulong value, int count) => (value << count) | (value >> (64 - count));

        private static ulong Merge(ulong accumulator, ulong lane) =>
            unchecked((accumulator ^ Round(0, lane)) * Prime1 + Prime4);
    }
}
