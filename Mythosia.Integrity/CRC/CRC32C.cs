using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.CRC
{
    /// <summary>
    /// Calculates CRC32C (Castagnoli) digests and validates frames containing a trailing digest.
    /// </summary>
    /// <remarks>
    /// Uses polynomial 0x1EDC6F41, reflected input and output, and an initial state and final XOR of 0xFFFFFFFF.
    /// Digest bytes use the platform's native byte order, matching the existing CRC32 framing convention.
    /// CRC32C is a different algorithm from CRC32 Classic.
    /// </remarks>
    public sealed class CRC32C : ErrorDetection
    {
        private const int DigestLength = sizeof(uint);

        /// <summary>Creates a CRC32C calculator.</summary>
        public CRC32C()
        {
        }

        /// <summary>Calculates the four digest bytes for a byte sequence.</summary>
        /// <param name="source">The bytes to consume once.</param>
        /// <returns>The CRC32C value in native byte order. Empty input produces four zero bytes.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes(CRC32CCore.Compute(source));
        }

        /// <summary>Copies a payload and appends its four CRC32C digest bytes in native byte order.</summary>
        /// <param name="source">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its CRC32C digest.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<byte>();
            uint state = CRC32CCore.InitialState;
            foreach (byte value in source)
            {
                result.Add(value);
                state = CRC32CCore.Update(state, value);
            }
            result.AddRange(BitConverter.GetBytes(~state));
            return result;
        }

        /// <summary>Validates a frame and returns its payload without the trailing CRC32C digest.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>The payload, or an empty sequence if validation fails. Frames must contain a nonempty payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));

            byte[] frame = sourceWithCRC.ToArray();
            if (!HasValidDigest(frame)) return Array.Empty<byte>();

            var payload = new byte[frame.Length - DigestLength];
            Array.Copy(frame, payload, payload.Length);
            return payload;
        }

        /// <summary>Reports whether a frame has an invalid CRC32C digest or lacks a nonempty payload.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>True if the frame has at most four bytes or its trailing digest does not match the payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidDigest(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the algorithm name.</summary>
        /// <returns>The string CRC32C.</returns>
        public override string GetDetectionType() => "CRC32C";

        private static bool HasValidDigest(byte[] frame)
        {
            if (frame.Length <= DigestLength) return false;

            int payloadLength = frame.Length - DigestLength;
            uint expected = ~CRC32CCore.Append(CRC32CCore.InitialState, frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt32(frame, payloadLength) == expected;
        }
    }
}
