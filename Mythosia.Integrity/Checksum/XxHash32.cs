using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates XXH32 hashes and validates payloads with a trailing hash.</summary>
    /// <remarks>
    /// This is the original XXH32 algorithm. Digest and frame bytes use native byte order,
    /// following this library's framing convention. The xxHash canonical representation is big-endian;
    /// serialize the numeric value explicitly when a protocol requires canonical bytes.
    /// </remarks>
    public sealed class XxHash32 : ErrorDetection
    {
        private const int DigestLength = sizeof(uint);

        /// <summary>Creates an XXH32 calculator.</summary>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        public XxHash32(uint seed = 0) => Seed = seed;

        /// <summary>Gets the seed used for computation and frame validation.</summary>
        public uint Seed { get; }

        /// <summary>Calculates hash bytes for a sequence consumed once.</summary>
        /// <param name="source">The input bytes, including an empty sequence.</param>
        /// <returns>The XXH32 hash in native byte order.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes(XxHash32Core.Compute(source, Seed));
        }

        /// <summary>Copies a payload and appends its XXH32 hash in native byte order.</summary>
        /// <param name="source">The payload consumed once.</param>
        /// <returns>The payload followed by its hash.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var result = new List<byte>();
            var accumulator = new XxHash32Accumulator(Seed);
            foreach (byte value in source)
            {
                result.Add(value);
                accumulator.AppendByte(value);
            }
            result.AddRange(BitConverter.GetBytes(accumulator.GetCurrentValue()));
            return result;
        }

        /// <summary>Validates a frame and removes its trailing hash.</summary>
        /// <param name="sourceWithCRC">The frame consumed once.</param>
        /// <returns>The payload, or an empty sequence for invalid frames. A valid frame requires a nonempty payload.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            byte[] frame = sourceWithCRC.ToArray();
            if (!HasValidHash(frame)) return Array.Empty<byte>();
            var payload = new byte[frame.Length - DigestLength];
            Array.Copy(frame, payload, payload.Length);
            return payload;
        }

        /// <summary>Reports an invalid hash or missing payload.</summary>
        /// <param name="sourceWithCRC">The frame consumed once.</param>
        /// <returns>True for a mismatched hash or a frame with at most 4 bytes.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidHash(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the algorithm name.</summary>
        /// <returns>The string XxHash32.</returns>
        public override string GetDetectionType() => "XxHash32";

        private bool HasValidHash(byte[] frame)
        {
            if (frame.Length <= DigestLength) return false;
            int payloadLength = frame.Length - DigestLength;
            var accumulator = new XxHash32Accumulator(Seed);
            accumulator.Append(frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt32(frame, payloadLength) == accumulator.GetCurrentValue();
        }
    }
}
