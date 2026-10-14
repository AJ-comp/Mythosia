using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Integrity.CRC
{
    /// <summary>Calculates CRC values from readable streams without buffering the whole input.</summary>
    public static class StreamCRCExtensions
    {
        private const int ReadBufferSize = 64 * 1024;
        private delegate void AppendBytes(ReadOnlySpan<byte> source);

        /// <summary>Reads from the current position to the end of the stream and calculates CRC8.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the stream open.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC8 algorithm, including the supported Basic alias.</param>
        /// <returns>The CRC8 value for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        /// <exception cref="InvalidOperationException">The stream contains no remaining bytes.</exception>
        public static byte ComputeCRC8(this Stream source, CRC8Type type = CRC8Type.Classic)
        {
            ValidateSource(source);
            var accumulator = new CRC8Accumulator(type);
            return ReadStream(source, accumulator.Append, accumulator.GetCurrentValue);
        }

        /// <summary>Reads from the current position to the end of the stream and calculates CRC16.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the stream open. DNP preserves the legacy numeric byte swap.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC16 algorithm, including the supported Basic alias.</param>
        /// <returns>The CRC16 value for all bytes read. Empty input returns 0xFFFF for Modbus and zero for other algorithms.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public static ushort ComputeCRC16(this Stream source, CRC16Type type = CRC16Type.Classic)
        {
            ValidateSource(source);
            var accumulator = new CRC16Accumulator(type);
            return ReadStream(source, accumulator.Append, accumulator.GetCurrentValue);
        }

        /// <summary>Reads from the current position to the end of the stream and calculates CRC32.</summary>
        /// <remarks>Reuses one 64 KiB buffer and leaves the stream open.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC32 algorithm, including the supported Basic alias.</param>
        /// <returns>The CRC32 value for all bytes read, or zero for an empty stream.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        public static uint ComputeCRC32(this Stream source, CRC32Type type = CRC32Type.Classic)
        {
            ValidateSource(source);
            var accumulator = new CRC32Accumulator(type);
            return ReadStream(source, accumulator.Append, accumulator.GetCurrentValue);
        }

        /// <summary>Asynchronously reads to the end of the stream and calculates CRC8.</summary>
        /// <remarks>Starts at the current position, reuses one 64 KiB buffer and leaves the stream open, including on cancellation or failure.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC8 algorithm, including the supported Basic alias.</param>
        /// <param name="cancellationToken">The token used to cancel stream reads and calculation between reads.</param>
        /// <returns>The CRC8 value for all bytes read.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        /// <exception cref="InvalidOperationException">The stream contains no remaining bytes.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<byte> ComputeCRC8Async(this Stream source, CRC8Type type = CRC8Type.Classic,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            var accumulator = new CRC8Accumulator(type);
            return ReadStreamAsync(source, accumulator.Append, accumulator.GetCurrentValue, cancellationToken);
        }

        /// <summary>Asynchronously reads to the end of the stream and calculates CRC16.</summary>
        /// <remarks>Starts at the current position, reuses one 64 KiB buffer and leaves the stream open, including on cancellation or failure. DNP preserves the legacy numeric byte swap.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC16 algorithm, including the supported Basic alias.</param>
        /// <param name="cancellationToken">The token used to cancel stream reads and calculation between reads.</param>
        /// <returns>The CRC16 value for all bytes read. Empty input returns 0xFFFF for Modbus and zero for other algorithms.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<ushort> ComputeCRC16Async(this Stream source, CRC16Type type = CRC16Type.Classic,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            var accumulator = new CRC16Accumulator(type);
            return ReadStreamAsync(source, accumulator.Append, accumulator.GetCurrentValue, cancellationToken);
        }

        /// <summary>Asynchronously reads to the end of the stream and calculates CRC32.</summary>
        /// <remarks>Starts at the current position, reuses one 64 KiB buffer and leaves the stream open, including on cancellation or failure.</remarks>
        /// <param name="source">The readable stream to consume.</param>
        /// <param name="type">The CRC32 algorithm, including the supported Basic alias.</param>
        /// <param name="cancellationToken">The token used to cancel stream reads and calculation between reads.</param>
        /// <returns>The CRC32 value for all bytes read, or zero for an empty stream.</returns>
        /// <exception cref="ArgumentNullException">The stream is null.</exception>
        /// <exception cref="ArgumentException">The stream is not readable.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The algorithm is not supported.</exception>
        /// <exception cref="OperationCanceledException">Cancellation was requested.</exception>
        public static Task<uint> ComputeCRC32Async(this Stream source, CRC32Type type = CRC32Type.Classic,
            CancellationToken cancellationToken = default)
        {
            ValidateSource(source);
            var accumulator = new CRC32Accumulator(type);
            return ReadStreamAsync(source, accumulator.Append, accumulator.GetCurrentValue, cancellationToken);
        }

        private static void ValidateSource(Stream source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (!source.CanRead) throw new ArgumentException("The stream must be readable.", nameof(source));
        }

        private static T ReadStream<T>(Stream source, AppendBytes append, Func<T> getCurrentValue)
        {
            var buffer = new byte[ReadBufferSize];
            int count;
            while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
                append(buffer.AsSpan(0, count));
            return getCurrentValue();
        }

        private static async Task<T> ReadStreamAsync<T>(Stream source, AppendBytes append,
            Func<T> getCurrentValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var buffer = new byte[ReadBufferSize];
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                int count = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
                if (count == 0) return getCurrentValue();
                append(buffer.AsSpan(0, count));
            }
        }
    }
}
