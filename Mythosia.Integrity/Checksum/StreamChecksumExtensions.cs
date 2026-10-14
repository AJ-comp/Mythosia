using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Integrity.Checksum
{
    /// <summary>Calculates checksums from readable streams without buffering the whole input.</summary>
    public static class StreamChecksumExtensions
    {
        private const int ReadBufferSize = 64 * 1024;

        /// <summary>Reads from the current position to the end of the stream and calculates Adler-32.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the stream open, including on failure.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <returns>The numeric Adler-32 value for all bytes read, or one for an empty stream.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        public static uint ComputeAdler32(this Stream source)
        {
            ValidateSource(source);

            var accumulator = new Adler32Accumulator();
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                accumulator.Append(buffer.AsSpan(0, count));
            return accumulator.GetCurrentValue();
        }

        /// <summary>Asynchronously reads to the end of the stream and calculates Adler-32.</summary>
        /// <remarks>Starts at the current position, reuses one 64 KiB buffer and leaves the stream open, including on cancellation or failure.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="cancellationToken">The token used to cancel stream reads and calculation between reads.</param>
        /// <returns>The numeric Adler-32 value for all bytes read, or one for an empty stream.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<uint> ComputeAdler32Async(this Stream source,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            return ReadStreamAsync(source, cancellationToken);
        }

        private static void ValidateSource(Stream source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("The stream must be readable.", nameof(source));
        }

        private static async Task<uint> ReadStreamAsync(Stream source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var accumulator = new Adler32Accumulator();
            var buffer = new byte[ReadBufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = await source.ReadAsync(buffer.AsMemory(), cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0) return accumulator.GetCurrentValue();
                accumulator.Append(buffer.AsSpan(0, count));
            }
        }
    }
}
