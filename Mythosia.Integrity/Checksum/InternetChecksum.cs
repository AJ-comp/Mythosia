using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates Internet checksums and validates frames containing a trailing checksum.</summary>
    /// <remarks>
    /// Uses the one's-complement sum of big-endian 16-bit words defined in RFC 1071.
    /// An odd trailing byte is padded with a zero low byte for calculation only.
    /// Digest bytes use the platform's native byte order, matching the library's existing two-byte framing convention.
    /// Network protocols require explicit big-endian serialization of the numeric checksum.
    /// Supply the exact bytes required by the protocol: this calculator does not construct pseudo-headers,
    /// clear checksum fields, or replace a zero checksum with 0xFFFF for UDP.
    /// </remarks>
    public sealed class InternetChecksum : ErrorDetection
    {
        private const int DigestLength = sizeof(ushort);

        /// <summary>Creates an Internet checksum calculator.</summary>
        public InternetChecksum()
        {
        }

        /// <summary>Calculates the two checksum bytes for a byte sequence.</summary>
        /// <param name="source">The bytes to consume once.</param>
        /// <returns>The Internet checksum in native byte order. Empty input produces the value 0xFFFF.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes(InternetChecksumCore.Compute(source));
        }

        /// <summary>Copies a payload and appends its two Internet checksum bytes in native byte order.</summary>
        /// <remarks>This library frame is distinct from a network protocol's checksum field and byte order.</remarks>
        /// <param name="source">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its Internet checksum.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<byte>();
            var state = new InternetChecksumCore();
            foreach (byte value in source)
            {
                result.Add(value);
                state.Update(value);
            }
            result.AddRange(BitConverter.GetBytes(state.GetCurrentValue()));
            return result;
        }

        /// <summary>Validates a frame and returns its payload without the trailing Internet checksum.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>The payload, or an empty sequence if validation fails. Frames must contain a nonempty payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));

            byte[] frame = sourceWithCRC.ToArray();
            if (!HasValidChecksum(frame)) return Array.Empty<byte>();

            var payload = new byte[frame.Length - DigestLength];
            Array.Copy(frame, payload, payload.Length);
            return payload;
        }

        /// <summary>Reports whether a frame has an invalid Internet checksum or lacks a nonempty payload.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>True if the frame has at most two bytes or its trailing checksum does not match the payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidChecksum(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the algorithm name.</summary>
        /// <returns>The string InternetChecksum.</returns>
        public override string GetDetectionType() => "InternetChecksum";

        private static bool HasValidChecksum(byte[] frame)
        {
            if (frame.Length <= DigestLength) return false;

            int payloadLength = frame.Length - DigestLength;
            var state = new InternetChecksumCore();
            state.Append(frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt16(frame, payloadLength) == state.GetCurrentValue();
        }
    }
}
