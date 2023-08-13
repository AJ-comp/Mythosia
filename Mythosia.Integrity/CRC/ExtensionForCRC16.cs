using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.CRC
{
    public static class ExtensionForCRC16
    {

        /*******************************************/
        /// <summary>
        /// This function returns the CRC16 value.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type">This parameter means crc16 algorithm type.</param>
        /// <returns></returns>
        /*******************************************/
        public static ushort CRC16(this IEnumerable<byte> source, CRC16Type type = CRC16Type.Basic)
        {
            var crc = new CRC16(type).Compute(source).ToArray();
            if (crc.Length < 2) throw new InvalidOperationException();

            return BitConverter.ToUInt16(crc, 0);
        }

        /*******************************************/
        /// <summary>
        /// This function returns the byte array included crc16 value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means crc16 algorithm type.</param>
        /// <returns></returns>
        /*******************************************/
        public static IEnumerable<byte> WithCRC16(this IEnumerable<byte> data, CRC16Type type = CRC16Type.Basic)
        {
            return new CRC16(type).Encode(data);
        }
    }
}
