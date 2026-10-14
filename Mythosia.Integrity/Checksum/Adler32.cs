using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates Adler-32 checksums and validates frames containing a trailing checksum.</summary>
    /// <remarks>
    /// Uses the Adler-32 algorithm defined in RFC 1950, with an initial value of one and modulus 65521.
    /// Digest bytes use the platform's native byte order, matching the library's existing four-byte framing convention.
    /// The zlib format specifies big-endian checksum bytes; serialize the numeric value explicitly when writing that format.
    /// </remarks>
    public sealed class Adler32 : ErrorDetection
    {
        private const int DigestLength = sizeof(uint);

        /// <summary>Creates an Adler-32 calculator.</summary>
        public Adler32()
        {
        }

        /// <summary>Calculates the four checksum bytes for a byte sequence.</summary>
        /// <param name="source">The bytes to consume once.</param>
        /// <returns>The Adler-32 value in native byte order. Empty input produces the value one.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes(Adler32Core.Compute(source));
        }

        /// <summary>Copies a payload and appends its four Adler-32 checksum bytes in native byte order.</summary>
        /// <param name="source">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its Adler-32 checksum.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<byte>();
            uint state = Adler32Core.InitialState;
            foreach (byte value in source)
            {
                result.Add(value);
                state = Adler32Core.Update(state, value);
            }
            result.AddRange(BitConverter.GetBytes(state));
            return result;
        }

        /// <summary>Validates a frame and returns its payload without the trailing Adler-32 checksum.</summary>
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

        /// <summary>Reports whether a frame has an invalid Adler-32 checksum or lacks a nonempty payload.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>True if the frame has at most four bytes or its trailing checksum does not match the payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidChecksum(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the algorithm name.</summary>
        /// <returns>The string Adler32.</returns>
        public override string GetDetectionType() => "Adler32";

        private static bool HasValidChecksum(byte[] frame)
        {
            if (frame.Length <= DigestLength) return false;

            int payloadLength = frame.Length - DigestLength;
            uint expected = Adler32Core.Append(Adler32Core.InitialState, frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt32(frame, payloadLength) == expected;
        }
    }
}
