using System;
using System.Collections.Generic;

namespace Mythosia.Integrity
{
    public static class ExtensionForCRC16
    {
        public enum CRC16Type
        {
            Basic,
            CCITTxModem,
            DNP,
        }

        /*******************************************/
        /// <summary>
        /// This function returns the CRC16 value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means crc16 algorithm type.</param>
        /// <returns></returns>
        /*******************************************/
        public static ushort CRC16(this IEnumerable<byte> data, CRC16Type type = CRC16Type.Basic)
        {
            ushort result = 0;

            if (type == CRC16Type.Basic) result = CRC.ComputeCRC16(data);
            else if (type == CRC16Type.CCITTxModem) result = CRC.ComputeCCITTxModem(data);
            else if (type == CRC16Type.DNP) result = CRC.ComputeDNP(data);

            return result;
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
            if (type == CRC16Type.Basic) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeCRC16(data)));
            else if (type == CRC16Type.CCITTxModem) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeCCITTxModem(data)));
            else if(type == CRC16Type.DNP) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeDNP(data)));

            return new List<byte>();
        }

        /*******************************************/
        /// <summary>
        /// This function returns the CRC CCITT (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************/
        [Obsolete("This method is deprecated, please use the extension method .CRC16 included the parameter instead.")]
        public static ushort CCITTxModem(this IEnumerable<byte> data) => CRC.ComputeCCITTxModem(data);

        /*******************************************/
        /// <summary>
        /// This function returns the byte array included crc-ccitt (x modem) value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************/
        [Obsolete("This method is deprecated, please use the extension method .WithCRC16 included the parameter instead.")]
        public static IEnumerable<byte> WithCCITTxModem(this IEnumerable<byte> data)
            => data.AppendRange(BitConverter.GetBytes(data.CCITTxModem()));

        /*******************************************/
        /// <summary>
        /// This function returns the CRC DNP (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************/
        [Obsolete("This method is deprecated, please use the extension method .CRC16 included the parameter instead.")]
        public static ushort DNP(this IEnumerable<byte> data) => CRC.ComputeDNP(data);

        /*******************************************/
        /// <summary>
        /// This function returns the byte array included crc-dnp value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************/
        [Obsolete("This method is deprecated, please use the extension method .WithCRC16 included the parameter instead.")]
        public static IEnumerable<byte> WithDNP(this IEnumerable<byte> data)
            => data.AppendRange(BitConverter.GetBytes(data.DNP()));
    }
}
