using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Integrity.Checksum
{
    public static partial class StreamChecksumExtensions
    {
        /// <summary>Reads to EOF and calculates OpenZFS Fletcher-4 using the selected 32-bit input word byte order.</summary>
        /// <remarks>Starts at the current position, reuses one 64 KiB buffer and leaves the stream open on all paths.</remarks>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The byte order is unsupported.</exception>
        /// <exception cref="InvalidDataException">The number of bytes read is not divisible by four.</exception>
        public static Fletcher4Value ComputeFletcher4(this Stream source,
            Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian)
        {
            ValidateSource(source);
            var accumulator = new Fletcher4Accumulator(byteOrder);
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                accumulator.Append(buffer.AsSpan(0, count));
            return FletcherStreamValue(accumulator);
        }

        /// <summary>Asynchronously reads to EOF and calculates OpenZFS Fletcher-4 using the selected input word byte order.</summary>
        /// <remarks>Preserves partial words across reads, reuses one 64 KiB buffer and leaves the stream open on all paths.</remarks>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The byte order is unsupported.</exception>
        /// <exception cref="InvalidDataException">The number of bytes read is not divisible by four.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<Fletcher4Value> ComputeFletcher4Async(this Stream source,
            Fletcher4ByteOrder byteOrder = Fletcher4ByteOrder.LittleEndian, CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            var accumulator = new Fletcher4Accumulator(byteOrder);
            return FletcherReadStreamAsync(source, accumulator, cancellationToken);
        }

        private static async Task<Fletcher4Value> FletcherReadStreamAsync(Stream source,
            Fletcher4Accumulator accumulator, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = new byte[ReadBufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0) return FletcherStreamValue(accumulator);
                accumulator.Append(buffer.AsSpan(0, count));
            }
        }

        private static Fletcher4Value FletcherStreamValue(Fletcher4Accumulator accumulator)
        {
            try { return accumulator.GetCurrentValue(); }
            catch (InvalidOperationException exception)
            {
                throw new InvalidDataException("Fletcher-4 stream length must be divisible by four bytes.", exception);
            }
        }
    }
}
