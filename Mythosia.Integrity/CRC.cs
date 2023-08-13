using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity
{
    internal class CRCAlgorithm
    {
        private static readonly ushort P_KERMIT = 0x8408;
        private static readonly ushort P_SICK = 0x8005;

        private static ushort[] crc16SickTable;
        private static ushort[] crc_tabkermit;



        /*******************************************************************/
        /// <summary>
        /// *   The function  UpdateCRC16Sick  calculates  a  new  CRC16 (Sick)     *
        /// *   value  based  on the previous value of the CRC and the next     *
        /// *   byte of the data to be checked.                                 *
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="c"></param>
        /// <param name="prev_byte"></param>
        /// <returns></returns>
        /*******************************************************************/
        private static ushort UpdateCRC16Sick(ushort crc, byte c, byte prev_byte)
        {
            var short_c = (ushort)(0x00ff & c);
            var short_p = (ushort)(0x00ff & prev_byte) << 8;

            if ((crc & 0x8000) != 0) crc = (ushort)((crc << 1) ^ P_SICK);
            else crc = (ushort)(crc << 1);

            crc &= 0xffff;
            crc ^= (ushort)(short_c | short_p);

            return crc;
        }

        /*******************************************************************/
        /// <summary>
        /// *   The function InitKermitTable() is used to fill the array     *
        /// *   for calculation of the CRC Kermit with values.                  *
        /// </summary>
        /*******************************************************************/
        private static void InitKermitTable()
        {
            crc_tabkermit = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x0001) != 0) crc = (ushort)((crc >> 1) ^ P_KERMIT);
                    else crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }
                crc_tabkermit[i] = crc;
            }
        }
    }
}
