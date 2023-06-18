using System;
using System.Collections.Generic;

namespace Mythosia.Integrity
{
    public static class ExtensionForCRC16
    {
        public enum CRC16Type
        {
            Basic,
            Modbus,
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
            else if (type == CRC16Type.Modbus) result = CRC.ComputeCRC16Modbus(data);
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
            else if (type == CRC16Type.Modbus) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeCRC16Modbus(data)));
            else if (type == CRC16Type.CCITTxModem) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeCCITTxModem(data)));
            else if (type == CRC16Type.DNP) return data.AppendRange(BitConverter.GetBytes(CRC.ComputeDNP(data)));

            return new List<byte>();
        }
    }
}
