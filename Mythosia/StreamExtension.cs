using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Mythosia
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

        private static void WriteInt32(this Stream stream, int value)
        {
            byte[] buf = BitConverter.GetBytes(value);
            stream.Write(buf, 0, buf.Length);
        }

    }
}
