using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity
{
    internal class CRC
    {
        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC16 value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        internal static ushort ComputeCRC16(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            if (!crc_tab16_init) InitCRC16Table();

            foreach (var d in data)
                result = UpdateCRC16(result, d);

            return result;
        }


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC CCITT (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        internal static ushort ComputeCCITTxModem(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            if (!crc_tabccitt_init) InitCCITTTable();

            foreach (var d in data)
                result = UpdateCCITTxModem(result, d);

            return result;
        }


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC DNP (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        internal static ushort ComputeDNP(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            if (!crc_tabdnp_init) InitDNPTable();

            for (int i = 0; i < data.Count(); i++)
            {
                byte index = (byte)(result ^ data.ElementAt(i));
                result = (ushort)((result >> 8) ^ crc_tabdnp[index]);
            }

            result = (ushort)(result ^ 0xffff);
            return (ushort)((ushort)((result & 0xff) << 8) | ((result >> 8) & 0xff));
        }


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC32 value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        internal static uint ComputeCRC32(IEnumerable<byte> data)
        {
            long result = 0;
            if (data.Count() <= 0) return (uint)result;

            if (!crc_tab32_init) InitCRC32Table();

            uint crc = 0xffffffff;
            for (int i = 0; i < data.Count(); i++)
            {
                var c = data.ElementAt(i);
                crc = (crc >> 8) ^ crc_tab32[(crc ^ c) & 0xFF];
            }

            return ~crc; //(crc ^ (-1)) >> 0;
        }




        private static ushort P_16 = 0xA001;
        private static uint P_32 = 0xEDB88320;
        private static ushort P_CCITT = 0x1021;
        private static ushort P_DNP = 0xA6BC;
        private static ushort P_KERMIT = 0x8408;
        private static ushort P_SICK = 0x8005;


        /*******************************************************************\
        *                                                                   *
        *   The algorithms use tables with precalculated  values.  This     *
        *   speeds  up  the calculation dramaticaly. The first time the     *
        *   CRC function is called, the table for that specific  calcu-     *
        *   lation  is set up. The ...init variables are used to deter-     *
        *   mine if the initialization has taken place. The  calculated     *
        *   values are stored in the crc_tab... arrays.                     *
        *                                                                   *
        *   The variables are declared static. This makes them  invisi-     *
        *   ble for other modules of the program.                           *
        *                                                                   *
        \*******************************************************************/
        private static ushort[] crc_tab16 = new ushort[256];
        private static uint[] crc_tab32 = new uint[256]; // ulong?
        private static ushort[] crc_tabccitt = new ushort[256];
        private static ushort[] crc_tabdnp = new ushort[256];
        private static ushort[] crc_tabkermit = new ushort[256];


        private static bool crc_tab16_init = false;
        private static bool crc_tab32_init = false;
        private static bool crc_tabccitt_init = false;
        private static bool crc_tabdnp_init = false;
        private static bool crc_tabkermit_init = false;


        /*******************************************************************/
        /// <summary>
        /// *   The function UpdateCRC16 calculates a  new  CRC-16  value     *
        /// *   based  on  the  previous value of the CRC and the next byte     *
        /// *   of the data to be checked.                                      *
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        /*******************************************************************/
        private static ushort UpdateCRC16(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc ^ short_c);
            return (ushort)((crc >> 8) ^ crc_tab16[tmp & 0xff]);
        }


        /*******************************************************************/
        /// <summary>
        ///     The function UpdateCCITTxModem calculates  a  new  CRC-CCITT (xModem)    *
        ///     value  based  on the previous value of the CRC and the next     *
        ///     byte of the data to be checked.                                 *
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        /*******************************************************************/
        private static ushort UpdateCCITTxModem(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc >> 8) ^ short_c;
            return (ushort)((crc << 8) ^ crc_tabccitt[tmp]);
        }


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
        /// *   The function UpdateCRC32 calculates a  new  CRC-32  value     *
        /// *   based  on  the  previous value of the CRC and the next byte     *
        /// *   of the data to be checked.                                      *
        /// </summary>
        /// <param name="crc"></param>
        /// <param name="c"></param>
        /// <returns></returns>
        /*******************************************************************/
        private static long UpdateCRC32(long crc, byte c)
        {
            long long_c = (0x000000ffL & c);

            long tmp = (crc ^ long_c);
            crc = ((crc >> 8) ^ crc_tab32[tmp & 0xff]);

            return crc;
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitCRC16Table() is used  to  fill  the  array     *
        /// *   for calculation of the CRC-16 with values.                      *
        /// </summary>
        /*******************************************************************/
        private static void InitCRC16Table()
        {
            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x0001) != 0) crc = (ushort)((crc >> 1) ^ P_16);
                    else crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }

                crc_tab16[i] = crc;
            }
            crc_tab16_init = true;
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitCCITTTable() is used to fill the  array     *
        /// *   for calculation of the CRC-CCITT with values.                   *
        /// </summary>
        /*******************************************************************/
        private static void InitCCITTTable()
        {
            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)(i << 8);

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x8000) != 0) crc = (ushort)((crc << 1) ^ P_CCITT);
                    else crc = (ushort)(crc << 1);

                    c = (ushort)(c << 1);
                }
                crc_tabccitt[i] = crc;
            }
            crc_tabccitt_init = true;
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitDNPTable() is used  to  fill  the  array    *
        /// *   for calculation of the CRC-DNP with values.                     *
        /// </summary>
        /*******************************************************************/
        private static void InitDNPTable()
        {
            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {

                    if (((crc ^ c) & 0x0001) != 0) crc = (ushort)((crc >> 1) ^ P_DNP);
                    else crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }
                crc_tabdnp[i] = crc;
            }
            crc_tabdnp_init = true;
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitKermitTable() is used to fill the array     *
        /// *   for calculation of the CRC Kermit with values.                  *
        /// </summary>
        /*******************************************************************/
        private static void InitKermitTable()
        {
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
            crc_tabkermit_init = true;

        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitCRC32Table() is used  to  fill  the  array     *
        /// *   for calculation of the CRC-32 with values.                      *
        /// </summary>
        /*******************************************************************/
        private static void InitCRC32Table()
        {
            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    var res = c & 1;
                    c = (res == 1) ? (P_32 ^ (c >> 1)) : (c >> 1);
                }
                crc_tab32[n] = c;
            }

            crc_tab32_init = true;
        }
    }
}
