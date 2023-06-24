using System;
using System.Collections.Generic;

namespace Mythosia.Integrity
{
    public static class ExtensionForCRC32
    {
        public enum CRC32Type
        {
            Basic,
        }

        /*******************************************/
        /// <summary>
        /// This function returns the CRC32 value
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means crc32 algorithm type.</param>
        /// <returns></returns>
        /*******************************************/
        public static uint CRC32(this IEnumerable<byte> data, CRC32Type type = CRC32Type.Basic)
        {
            uint result = 0;

            if (type == CRC32Type.Basic) result = CRC.ComputeCRC32(data);

            return result;
        }

        /*******************************************/
        /// <summary>
        /// This function returns the byte array included crc32 value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means crc32 algorithm type.</param>
        /// <returns></returns>
        /*******************************************/
        public static IEnumerable<byte> WithCRC32(this IEnumerable<byte> data, CRC32Type type = CRC32Type.Basic)
        {
            List<byte> result = new List<byte>();
            result.AddRange(data);

            if (type == CRC32Type.Basic) result.AddRange(data.CRC32(type).ToByteArray());

            return result;
        }
    }
}
