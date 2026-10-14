using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Identifies the supported CRC64 algorithms.</summary>
    public enum CRC64Type
    {
        /// <summary>The reflected CRC64/NVME algorithm.</summary>
        NVME = 0,
    }

    /// <summary>Calculates CRC64 digests and validates frames containing a trailing digest.</summary>
    /// <remarks>
    /// NVME uses polynomial 0xAD93D23594C93659, reflected input and output, and an initial state and final XOR of 0xFFFFFFFFFFFFFFFF.
    /// Digest bytes use the platform's native byte order, matching the existing CRC framing convention.
    /// </remarks>
    public sealed class CRC64 : ErrorDetection
    {
        private const int DigestLength = sizeof(ulong);

        /// <summary>Gets the selected CRC64 algorithm.</summary>
        public CRC64Type Type { get; }

        /// <summary>Creates a CRC64 calculator for the selected algorithm.</summary>
        /// <param name="type">The CRC64 algorithm. Only NVME is supported.</param>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public CRC64(CRC64Type type = CRC64Type.NVME)
        {
            CRC64Core.ValidateType(type);
            Type = type;
        }

        /// <summary>Calculates the eight digest bytes for a byte sequence.</summary>
        /// <param name="source">The bytes to consume once.</param>
        /// <returns>The CRC64 value in native byte order. Empty input produces eight zero bytes.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            return BitConverter.GetBytes(CRC64Core.Compute(source));
        }

        /// <summary>Copies a payload and appends its eight CRC64 digest bytes in native byte order.</summary>
        /// <param name="source">The payload bytes to consume once.</param>
        /// <returns>The payload followed by its CRC64 digest.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var result = new List<byte>();
            ulong state = CRC64Core.InitialState;
            foreach (byte value in source)
            {
                result.Add(value);
                state = CRC64Core.Update(state, value);
            }
            result.AddRange(BitConverter.GetBytes(~state));
            return result;
        }

        /// <summary>Validates a frame and returns its payload without the trailing CRC64 digest.</summary>
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

        /// <summary>Reports whether a frame has an invalid CRC64 digest or lacks a nonempty payload.</summary>
        /// <param name="sourceWithCRC">The frame bytes to consume once.</param>
        /// <returns>True if the frame has at most eight bytes or its trailing digest does not match the payload.</returns>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidDigest(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the selected algorithm name.</summary>
        /// <returns>The string NVME.</returns>
        public override string GetDetectionType() => Type.ToString();

        private static bool HasValidDigest(byte[] frame)
        {
            if (frame.Length <= DigestLength) return false;

            int payloadLength = frame.Length - DigestLength;
            ulong expected = ~CRC64Core.Append(CRC64Core.InitialState, frame.AsSpan(0, payloadLength));
            return BitConverter.ToUInt64(frame, payloadLength) == expected;
        }
    }
}
