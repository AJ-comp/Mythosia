using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.IO
{
    public static class StreamExtension
    {
        private static string ReadString(this Stream stream, int timeOut)
        {
            int sentBytes = ReadInt32(stream, timeOut);
            if (sentBytes == 0) return string.Empty;

            byte[] buf = new byte[sentBytes];

            stream.Read(buf, 0, buf.Length);
            return Encoding.UTF8.GetString(buf, 0, buf.Length);
        }


        private static void WriteString(this Stream stream, string data)
        {
            byte[] buf = Encoding.UTF8.GetBytes(data);

            stream.WriteInt32(buf.Length);
            stream.Write(buf, 0, buf.Length);
        }



        private static int ReadInt32(this Stream stream, int timeOut)
        {
            byte[] intBytes = new byte[4];
            stream.ReadTimeout = timeOut;
            stream.Read(intBytes, 0, 4);

            return BitConverter.ToInt32(intBytes, 0);
        }


        private static async Task<int> ReadInt32Async(this Stream stream, int timeOut)
        {
            byte[] intBytes = new byte[4];
            await stream.ReadExAsync(intBytes, timeOut);

            return BitConverter.ToInt32(intBytes, 0);
        }


        private static void WriteInt32(this Stream stream, int value)
        {
            byte[] buf = value.ToByteArray();
            stream.Write(buf, 0, buf.Length);
        }


        private static Task WriteInt32Async(this Stream stream, int value)
        {
            byte[] buf = value.ToByteArray();
            return stream.WriteAsync(buf, 0, buf.Length);
        }


        /// <summary>
        /// Writes data to a stream asynchronously with a specified timeout.
        /// </summary>
        /// <param name="stream">The target stream to which the data will be written.</param>
        /// <param name="buffer">The data to write to the stream.</param>
        /// <param name="timeout">The timeout duration in milliseconds for the write operation. 
        /// If set to -2, the default stream's WriteTimeout value will be used.</param>
        /// <exception cref="TimeoutException">Thrown when the write operation exceeds the specified timeout duration.</exception>
        public static async ValueTask WriteExAsync(this Stream stream, ReadOnlyMemory<byte> buffer, int timeout = -2)
        {
            if (timeout <= -2) timeout = stream.WriteTimeout;

            using var cts = new CancellationTokenSource(timeout);

            try
            {
                await stream.WriteAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The write operation has timed out.");
            }
        }


        /// <summary>
        /// Reads data from a stream asynchronously with a specified timeout.
        /// </summary>
        /// <param name="stream">The source stream from which the data will be read.</param>
        /// <param name="buffer">The buffer where the read data will be stored.</param>
        /// <param name="timeout">The timeout duration in milliseconds for the read operation. 
        /// If set to -2, the default stream's ReadTimeout value will be used.</param>
        /// <returns>The number of bytes read into the buffer.</returns>
        /// <exception cref="TimeoutException">Thrown when the read operation exceeds the specified timeout duration.</exception>
        public static async ValueTask<int> ReadExAsync(this Stream stream, Memory<byte> buffer, int timeout = -2)
        {
            if (timeout <= -2) timeout = stream.ReadTimeout;

            using var cts = new CancellationTokenSource(timeout);

            try
            {
                return await stream.ReadAsync(buffer, cts.Token);
            }
            catch (OperationCanceledException)
            {
                throw new TimeoutException("The read operation has timed out.");
            }
        }
    }
}
