using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.CRC
{
    public static class ExtensionForCRC32
    {
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
            return BitConverter.ToUInt32(new CRC32(type).Compute(data).ToArray(), 0);
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
            return new CRC32(type).Encode(data);
        }
    }
}
