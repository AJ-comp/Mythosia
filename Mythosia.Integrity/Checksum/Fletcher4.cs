using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates OpenZFS Fletcher-4 and supports the library's trailing-digest frame convention.</summary>
    /// <remarks>
    /// Input consists of complete 32-bit words. The selected byte order controls input word interpretation.
    /// Digest bytes contain A, B, C and D, each serialized as a native-endian 64-bit integer (32 bytes total).
    /// This generic trailing digest is not an OpenZFS block-pointer or send-stream record format.
    /// </remarks>
    public sealed class Fletcher4 : ErrorDetection
    {
        private const int DigestLength = 4 * sizeof(ulong);

        /// <summary>Creates a calculator with the selected input word byte order.</summary>
        /// <exception cref="ArgumentOutOfRangeException">The byte order is not supported.</exception>
        public Fletcher4(Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian)
        {
            Fletcher4Accumulator.ValidateByteOrder(byteOrder);
            ByteOrder = byteOrder;
        }

        /// <summary>Gets the byte order of the 32-bit input words.</summary>
        public Fletcher4ByteOrder ByteOrder { get; }

        /// <summary>Calculates the 32 digest bytes, in A/B/C/D order with native-endian 64-bit sums.</summary>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The input length is not divisible by four.</exception>
        public override IEnumerable<byte> Compute(IEnumerable<byte> source) => Digest(ComputeValue(source, ByteOrder));

        /// <summary>Copies the payload and appends its 32 digest bytes.</summary>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The input length is not divisible by four.</exception>
        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var frame = new List<byte>();
            var accumulator = new Fletcher4Accumulator(ByteOrder);
            foreach (byte value in source)
            {
                frame.Add(value);
                accumulator.AppendByte(value);
            }
            if (frame.Count % sizeof(uint) != 0)
                throw new ArgumentException("Fletcher-4 input length must be divisible by four bytes.", nameof(source));
            frame.AddRange(Digest(accumulator.GetCurrentValue()));
            return frame;
        }

        /// <summary>Returns the payload of a valid frame, or an empty sequence for an invalid frame.</summary>
        /// <remarks>A valid frame requires a nonempty payload whose length is divisible by four.</remarks>
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

        /// <summary>Reports whether a frame lacks an aligned, nonempty payload or has an incorrect digest.</summary>
        /// <exception cref="ArgumentNullException">The frame is null.</exception>
        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC == null) throw new ArgumentNullException(nameof(sourceWithCRC));
            return !HasValidChecksum(sourceWithCRC.ToArray());
        }

        /// <summary>Gets the algorithm name.</summary>
        public override string GetDetectionType() => "Fletcher4";

        internal static Fletcher4Value ComputeValue(IEnumerable<byte> source, Fletcher4ByteOrder byteOrder)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            var accumulator = new Fletcher4Accumulator(byteOrder);
            foreach (byte value in source) accumulator.AppendByte(value);
            try { return accumulator.GetCurrentValue(); }
            catch (InvalidOperationException exception)
            {
                throw new ArgumentException("Fletcher-4 input length must be divisible by four bytes.", nameof(source), exception);
            }
        }

        private bool HasValidChecksum(byte[] frame)
        {
            int payloadLength = frame.Length - DigestLength;
            if (payloadLength <= 0 || payloadLength % sizeof(uint) != 0) return false;
            var accumulator = new Fletcher4Accumulator(ByteOrder);
            accumulator.Append(frame.AsSpan(0, payloadLength));
            Fletcher4Value expected = accumulator.GetCurrentValue();
            return BitConverter.ToUInt64(frame, payloadLength) == expected.A
                && BitConverter.ToUInt64(frame, payloadLength + 8) == expected.B
                && BitConverter.ToUInt64(frame, payloadLength + 16) == expected.C
                && BitConverter.ToUInt64(frame, payloadLength + 24) == expected.D;
        }

        private static byte[] Digest(Fletcher4Value value)
        {
            var result = new byte[DigestLength];
            BitConverter.GetBytes(value.A).CopyTo(result, 0);
            BitConverter.GetBytes(value.B).CopyTo(result, 8);
            BitConverter.GetBytes(value.C).CopyTo(result, 16);
            BitConverter.GetBytes(value.D).CopyTo(result, 24);
            return result;
        }
    }
}
