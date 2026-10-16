"""Reproduce the historical xxHash prototype without replacing maintained sources."""

from pathlib import Path

REPOSITORY_ROOT = Path(__file__).resolve().parents[4]
base = REPOSITORY_ROOT / 'TestResults/Research/integrity/xxhash/product'
base.mkdir(parents=True, exist_ok=True)
for bits, typ, block, word, primes in [(32,'uint',16,4,[2654435761,2246822519,3266489917,668265263,374761393]),(64,'ulong',32,8,[11400714785074694791,14029467366897019727,1609587929392839161,9650029242287828579,2870177450012600261])]:
 n=f'XxHash{bits}'; suffix='u' if bits==32 else 'ul'
 constants='\n'.join(f'        private const {typ} Prime{i+1} = {p}{suffix};' for i,p in enumerate(primes))
 if bits==32:
  final='''                uint result = _hasStripes
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
                return result ^ (result >> 16);'''
 else:
  final='''                ulong result;
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
                return result ^ (result >> 32);'''
 merge='''
        private static ulong Merge(ulong accumulator, ulong lane) =>
            unchecked((accumulator ^ Round(0, lane)) * Prime1 + Prime4);
''' if bits==64 else ''
 acc=f'''using System;
using System.Buffers.Binary;

namespace Mythosia.Integrity.Checksum
{{
    /// <summary>Calculates XXH{bits} incrementally with a fixed {block}-byte holdback buffer.</summary>
    /// <remarks>Snapshots do not change the state. An instance is not thread-safe.</remarks>
    public sealed class {n}Accumulator
    {{
{constants}
        private const int StripeLength = {block};
        private readonly byte[] _tail = new byte[StripeLength];
        private {typ} _v1, _v2, _v3, _v4;
        private ulong _length;
        private int _tailLength;
        private bool _hasStripes;

        /// <summary>Creates an empty XXH{bits} accumulator using the supplied seed.</summary>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        public {n}Accumulator({typ} seed = 0)
        {{
            Seed = seed;
            Reset();
        }}

        /// <summary>Gets the seed retained when the accumulator is reset.</summary>
        public {typ} Seed {{ get; }}

        /// <summary>Consumes the next bytes immediately without retaining the source.</summary>
        /// <param name="source">The next bytes. An empty span leaves the state unchanged.</param>
        public void Append(ReadOnlySpan<byte> source)
        {{
            if (source.IsEmpty) return;
            _length = unchecked(_length + (ulong)source.Length);
            if (_tailLength != 0)
            {{
                int count = Math.Min(StripeLength - _tailLength, source.Length);
                source.Slice(0, count).CopyTo(_tail.AsSpan(_tailLength));
                _tailLength += count;
                source = source.Slice(count);
                if (_tailLength < StripeLength) return;
                ProcessStripe(_tail);
                _tailLength = 0;
            }}
            while (source.Length >= StripeLength)
            {{
                ProcessStripe(source);
                source = source.Slice(StripeLength);
            }}
            source.CopyTo(_tail);
            _tailLength = source.Length;
        }}

        /// <summary>Gets the numeric XXH{bits} value without changing the calculation.</summary>
        /// <returns>The hash of every byte appended so far using the selected seed.</returns>
        public {typ} GetCurrentValue()
        {{
            unchecked
            {{
{final}
            }}
        }}

        /// <summary>Restores the empty state using the original seed.</summary>
        public void Reset()
        {{
            unchecked
            {{
                _v1 = Seed + Prime1 + Prime2;
                _v2 = Seed + Prime2;
                _v3 = Seed;
                _v4 = Seed - Prime1;
            }}
            _length = 0;
            _tailLength = 0;
            _hasStripes = false;
        }}

        internal void AppendByte(byte value)
        {{
            _length = unchecked(_length + 1);
            _tail[_tailLength++] = value;
            if (_tailLength != StripeLength) return;
            ProcessStripe(_tail);
            _tailLength = 0;
        }}

        private void ProcessStripe(ReadOnlySpan<byte> stripe)
        {{
            _v1 = Round(_v1, BinaryPrimitives.ReadUInt{bits}LittleEndian(stripe));
            _v2 = Round(_v2, BinaryPrimitives.ReadUInt{bits}LittleEndian(stripe.Slice({word})));
            _v3 = Round(_v3, BinaryPrimitives.ReadUInt{bits}LittleEndian(stripe.Slice({word*2})));
            _v4 = Round(_v4, BinaryPrimitives.ReadUInt{bits}LittleEndian(stripe.Slice({word*3})));
            _hasStripes = true;
        }}

        private static {typ} Round({typ} lane, {typ} word) =>
            unchecked(Rotate(lane + word * Prime2, {13 if bits==32 else 31}) * Prime1);

        private static {typ} Rotate({typ} value, int count) => (value << count) | (value >> ({bits} - count));
{merge}    }}
}}
'''
 (base/f'{n}Accumulator.cs').write_text(acc, encoding='utf-8', newline='\n')
 core=f'''using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{{
    internal static class {n}Core
    {{
        internal static {typ} Compute(IEnumerable<byte> source, {typ} seed)
        {{
            var accumulator = new {n}Accumulator(seed);
            if (source is byte[] bytes) accumulator.Append(bytes);
            else foreach (byte value in source) accumulator.AppendByte(value);
            return accumulator.GetCurrentValue();
        }}
    }}
}}
'''
 (base/f'{n}Core.cs').write_text(core, encoding='utf-8', newline='\n')
 detector=f'''using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{{
    /// <summary>Calculates XXH{bits} hashes and validates payloads with a trailing hash.</summary>
    /// <remarks>
    /// This is the original XXH{bits} algorithm. Digest and frame bytes use native byte order,
    /// following this library's framing convention. The xxHash canonical representation is big-endian;
    /// serialize the numeric value explicitly when a protocol requires canonical bytes.
    /// </remarks>
    public sealed class {n} : ErrorDetection
    {{
        private const int DigestLength = sizeof({typ});

        /// <summary>Creates an XXH{bits} calculator.</summary>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        public {n}({typ} seed = 0) => Seed = seed;

        /// <summary>Gets the seed used for computation and frame validation.</summary>
        public {typ} Seed {{ get; }}

        /// <summary>Calculates hash bytes for a sequence consumed once.</summary>
        /// <param name="source">The input bytes, including an empty sequence.</param>
        /// <returns>The XXH{bits} hash in native byte order.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {{
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes({n}Core.Compute(source, Seed));
        }}

        /// <summary>Copies a payload and appends its XXH{bits} hash in native byte order.</summary>
        /// <param name="source">The payload consumed once.</param>
        /// <returns>The payload followed by its hash.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {{
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new List<byte>();
            var accumulator = new {n}Accumulator(Seed);
            foreach (byte value in source)
            {{
                result.Add(value);
                accumulator.AppendByte(value);
            }}
            result.AddRange(BitConverter.GetBytes(accumulator.GetCurrentValue()));
            return result;
        }}

        /// <summary>Validates a frame and removes its trailing hash.</summary>
        /// <param name="sourceWithCRC">The frame consumed once.</param>
        /// <returns>The payload, or an empty sequence for invalid frames. A valid frame requires a nonempty payload.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {{
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            byte[] frame = sourceWithCRC.ToArray();
            if (!HasValidHash(frame)) return Array.Empty<byte>();
            var payload = new byte[frame.Length - DigestLength];
            Array.Copy(frame, payload, payload.Length);
            return payload;
        }}

        /// <summary>Reports an invalid hash or missing payload.</summary>
        /// <param name="sourceWithCRC">The frame consumed once.</param>
        /// <returns>True for a mismatched hash or a frame with at most {word} bytes.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {{
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidHash(sourceWithCRC.ToArray());
        }}

        /// <summary>Gets the algorithm name.</summary>
        /// <returns>The string {n}.</returns>
        public override string GetDetectionType() => "{n}";

        private bool HasValidHash(byte[] frame)
        {{
            if (frame.Length <= DigestLength) return false;
            int payloadLength = frame.Length - DigestLength;
            var accumulator = new {n}Accumulator(Seed);
            accumulator.Append(frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt{bits}(frame, payloadLength) == accumulator.GetCurrentValue();
        }}
    }}
}}
'''
 (base/f'{n}.cs').write_text(detector, encoding='utf-8', newline='\n')
 ext=f'''using System;
using System.Collections.Generic;

namespace Mythosia.Integrity.Checksum
{{
    /// <summary>Provides numeric XXH{bits} hashes and native-byte-order frames.</summary>
    public static class ExtensionFor{n}
    {{
        /// <summary>Calculates the numeric XXH{bits} hash for a sequence.</summary>
        /// <param name="data">The input consumed once.</param>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        /// <returns>The numeric hash.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static {typ} {n}(this IEnumerable<byte> data, {typ} seed = 0)
        {{
            if (data == null) throw new ArgumentNullException(nameof(data));
            return {n}Core.Compute(data, seed);
        }}

        /// <summary>Appends an XXH{bits} hash in native byte order.</summary>
        /// <remarks>The xxHash canonical digest is big-endian. This method follows the library's native framing convention.</remarks>
        /// <param name="data">The payload consumed once.</param>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        /// <returns>The payload and trailing hash bytes.</returns>
        /// <exception cref="ArgumentNullException">The data is null.</exception>
        public static IEnumerable<byte> With{n}(this IEnumerable<byte> data, {typ} seed = 0)
        {{
            if (data == null) throw new ArgumentNullException(nameof(data));
            return new {n}(seed).Encode(data);
        }}
    }}
}}
'''
 (base/f'ExtensionFor{n}.cs').write_text(ext, encoding='utf-8', newline='\n')
stream='''using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Integrity.Checksum
{
    public static partial class StreamChecksumExtensions
    {
'''
for bits,typ in [(32,'uint'),(64,'ulong')]:
 n=f'XxHash{bits}'
 stream+=f'''        /// <summary>Reads from the current position to EOF and calculates numeric XXH{bits}.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        public static {typ} Compute{n}(this Stream source, {typ} seed = 0)
        {{
            ValidateSource(source);
            var accumulator = new {n}Accumulator(seed);
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                accumulator.Append(buffer.AsSpan(0, count));
            return accumulator.GetCurrentValue();
        }}

        /// <summary>Asynchronously reads from the current position to EOF and calculates numeric XXH{bits}.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on cancellation and failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH{bits} seed, zero by default.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to every read.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<{typ}> Compute{n}Async(this Stream source, {typ} seed = 0,
            CancellationToken cancellationToken = default)
        {{
            ValidateSource(source);
            return Read{n}Async(source, seed, cancellationToken);
        }}

        private static async Task<{typ}> Read{n}Async(Stream source, {typ} seed,
            CancellationToken cancellationToken)
        {{
            cancellationToken.ThrowIfCancellationRequested();
            var accumulator = new {n}Accumulator(seed);
            var buffer = new byte[ReadBufferSize];
            while (true)
            {{
                cancellationToken.ThrowIfCancellationRequested();
                int count = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0) return accumulator.GetCurrentValue();
                accumulator.Append(buffer.AsSpan(0, count));
            }}
        }}

'''
stream+='    }\n}\n'
(base/'StreamXxHashExtensions.cs').write_text(stream, encoding='utf-8', newline='\n')
print(f'Wrote historical prototype copies to {base}')
