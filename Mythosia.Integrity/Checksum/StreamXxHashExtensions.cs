using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Integrity.Checksum
{
    public static partial class StreamChecksumExtensions
    {
        /// <summary>Reads from the current position to EOF and calculates numeric XXH32.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        public static uint ComputeXxHash32(this Stream source, uint seed = 0)
        {
            ValidateSource(source);
            var accumulator = new XxHash32Accumulator(seed);
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                accumulator.Append(buffer.AsSpan(0, count));
            return accumulator.GetCurrentValue();
        }

        /// <summary>Asynchronously reads from the current position to EOF and calculates numeric XXH32.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on cancellation and failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH32 seed, zero by default.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to every read.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<uint> ComputeXxHash32Async(this Stream source, uint seed = 0,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            return ReadXxHash32Async(source, seed, cancellationToken);
        }

        private static async Task<uint> ReadXxHash32Async(Stream source, uint seed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var accumulator = new XxHash32Accumulator(seed);
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

        /// <summary>Reads from the current position to EOF and calculates numeric XXH64.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH64 seed, zero by default.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        public static ulong ComputeXxHash64(this Stream source, ulong seed = 0)
        {
            ValidateSource(source);
            var accumulator = new XxHash64Accumulator(seed);
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                accumulator.Append(buffer.AsSpan(0, count));
            return accumulator.GetCurrentValue();
        }

        /// <summary>Asynchronously reads from the current position to EOF and calculates numeric XXH64.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the source open, including on cancellation and failure.</remarks>
        /// <param name="source">The readable stream.</param>
        /// <param name="seed">The XXH64 seed, zero by default.</param>
        /// <param name="cancellationToken">The cancellation token forwarded to every read.</param>
        /// <returns>The numeric hash for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The source is null.</exception>
        /// <exception cref="ArgumentException">The source is not readable.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<ulong> ComputeXxHash64Async(this Stream source, ulong seed = 0,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            return ReadXxHash64Async(source, seed, cancellationToken);
        }

        private static async Task<ulong> ReadXxHash64Async(Stream source, ulong seed,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var accumulator = new XxHash64Accumulator(seed);
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
