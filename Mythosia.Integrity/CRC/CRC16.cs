using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace Mythosia.Integrity.CRC
{
    public enum CRC16Type
    {
        [Obsolete("Basic is obsolete. Use Classic instead.")]
        Basic,
        Classic,
        Modbus,
        CCITTxModem,
        DNP,
        /// <summary>CRC-16/CCITT-FALSE (CRC-16/IBM-3740): polynomial 0x1021, initial state 0xFFFF, no reflection or final XOR.</summary>
        CCITTFalse = 5,
        /// <summary>CRC-16/X-25 (CRC-16/IBM-SDLC): polynomial 0x1021, reflected input and output, initial state and final XOR 0xFFFF.</summary>
        X25 = 6,
        /// <summary>CRC-16/MCRF4XX: polynomial 0x1021, initial state 0xFFFF, reflected input and output, no final XOR.</summary>
        MCRF4XX = 7,
        /// <summary>CRC-16/USB: polynomial 0x8005, reflected input and output, initial state and final XOR 0xFFFF.</summary>
        USB = 8,
    }

    public class CRC16 : ErrorDetection
    {
        public CRC16Type Type { get; } = CRC16Type.Classic;

        public CRC16(CRC16Type type = CRC16Type.Classic)
        {
            Type = type;
        }

        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            List<byte> result = new List<byte>();

            if (Type == CRC16Type.Basic) AppendNativeBytes(result, ComputeCRC16(source));
            else if (Type == CRC16Type.Classic) AppendNativeBytes(result, ComputeCRC16(source));
            else if (Type == CRC16Type.Modbus) AppendNativeBytes(result, ComputeCRC16Modbus(source));
            else if (Type == CRC16Type.CCITTxModem) AppendNativeBytes(result, ComputeCCITTxModem(source));
            else if (Type == CRC16Type.DNP) AppendNativeBytes(result, ComputeDNP(source));
            else if (Type == CRC16Type.CCITTFalse || Type == CRC16Type.X25
                || Type == CRC16Type.MCRF4XX || Type == CRC16Type.USB)
                AppendNativeBytes(result, ComputeIncrementally(source));

            return result;
        }

        private ushort ComputeIncrementally(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var accumulator = new CRC16Accumulator(Type);
            if (source is byte[] bytes)
            {
                accumulator.Append(bytes);
                return accumulator.GetCurrentValue();
            }

            Span<byte> nextByte = stackalloc byte[1];
            foreach (byte value in source)
            {
                nextByte[0] = value;
                accumulator.Append(nextByte);
            }

            return accumulator.GetCurrentValue();
        }

        private static void AppendNativeBytes(List<byte> result, ushort value)
        {
            // Preserve the native byte order previously produced by Marshal.
            if (BitConverter.IsLittleEndian)
            {
                result.Add(unchecked((byte)value));
                result.Add(unchecked((byte)(value >> 8)));
                return;
            }

            result.Add(unchecked((byte)(value >> 8)));
            result.Add(unchecked((byte)value));
        }

        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            List<byte> result = new List<byte>();
            if (IsError(sourceWithCRC)) return result;

            return sourceWithCRC.Take(sourceWithCRC.Count() - 2);
        }

        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
        {
            List<byte> result = new List<byte>();
            result.AddRange(source);
            result.AddRange(Compute(source));

            return result;
        }

        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            int frameLength = sourceWithCRC.Count();
            if (frameLength <= 2) return true;
            var toCheckCRC = sourceWithCRC.TakeLast(2);

            var calculatedCRC = Compute(sourceWithCRC.Take(frameLength - 2));
            return !toCheckCRC.SequenceEqual(calculatedCRC);
        }

        public override string GetDetectionType() => Type.ToString();



        // Build each table locally before publishing it. CLR type initialization
        // runs once and prevents concurrent readers from seeing partial tables.
        private static readonly ushort[] crc_tab16 = GenerateCRC16Table();
        private static readonly ushort[] crc16ModbusTable = GenerateCRC16ModbusTable();
        private static readonly ushort[] crc_tabccitt = GenerateCCITTTable();
        private static readonly ushort[] crc_tabdnp = GenerateDNPTable();


        private const ushort P_16 = 0xA001;

        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC16 value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        private ushort ComputeCRC16(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            // Keep other sources on their existing enumeration path.
            if (data is byte[] bytes)
            {
                foreach (byte value in bytes)
                    result = UpdateCRC16(result, value);
            }
            else
            {
                foreach (var d in data)
                    result = UpdateCRC16(result, d);
            }

            return result;
        }

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
        private ushort UpdateCRC16(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc ^ short_c);
            return (ushort)((crc >> 8) ^ crc_tab16[tmp & 0xff]);
        }

        /*******************************************************************/
        /// <summary>
        /// *   The function InitCRC16Table() is used  to  fill  the  array     *
        /// *   for calculation of the CRC-16 with values.                      *
        /// </summary>
        /*******************************************************************/
        private static ushort[] GenerateCRC16Table()
        {
            ushort[] table = new ushort[256];

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

                table[i] = crc;
            }

            return table;
        }



        /*******************************************************************/
        /// <summary>
        /// Computes the CRC-16 (Modbus) value for the given byte array.
        /// </summary>
        /// <param name="data">The byte array to calculate the CRC-16 for.</param>
        /// <returns>The computed CRC-16 (Modbus) value.</returns>
        /*******************************************************************/
        private ushort ComputeCRC16Modbus(IEnumerable<byte> data)
        {
            ushort crc = 0xFFFF;

            if (data is byte[] bytes)
            {
                foreach (byte value in bytes)
                {
                    byte index = (byte)(crc ^ value);
                    crc = (ushort)((crc >> 8) ^ crc16ModbusTable[index]);
                }
            }
            else
            {
                foreach (var b in data)
                {
                    byte index = (byte)(crc ^ b);
                    crc = (ushort)((crc >> 8) ^ crc16ModbusTable[index]);
                }
            }

            return crc;
        }


        private static ushort[] GenerateCRC16ModbusTable()
        {
            ushort[] table = new ushort[256];
            const ushort polynomial = 0xA001;

            for (ushort i = 0; i < 256; i++)
            {
                ushort value = i;

                for (int j = 0; j < 8; j++)
                {
                    if ((value & 1) == 1)
                    {
                        value = (ushort)((value >> 1) ^ polynomial);
                    }
                    else
                    {
                        value >>= 1;
                    }
                }

                table[i] = value;
            }

            return table;
        }


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC CCITT (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        private ushort ComputeCCITTxModem(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            if (data is byte[] bytes)
            {
                foreach (byte value in bytes)
                    result = UpdateCCITTxModem(result, value);
            }
            else
            {
                foreach (var d in data)
                    result = UpdateCCITTxModem(result, d);
            }

            return result;
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitCCITTTable() is used to fill the  array     *
        /// *   for calculation of the CRC-CCITT with values.                   *
        /// </summary>
        /*******************************************************************/
        private static ushort[] GenerateCCITTTable()
        {
            ushort[] table = new ushort[256];

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)(i << 8);

                for (int j = 0; j < 8; j++)
                {
                    if (((crc ^ c) & 0x8000) != 0) crc = (ushort)((crc << 1) ^ 0x1021);
                    else crc = (ushort)(crc << 1);

                    c = (ushort)(c << 1);
                }
                table[i] = crc;
            }

            return table;
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
        private ushort UpdateCCITTxModem(ushort crc, byte c)
        {
            var short_c = (ushort)(0x00ff & c);

            var tmp = (ushort)(crc >> 8) ^ short_c;
            return (ushort)((crc << 8) ^ crc_tabccitt[tmp]);
        }


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC DNP (xModem) value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        private ushort ComputeDNP(IEnumerable<byte> data)
        {
            ushort result = 0;
            if (data.Count() <= 0) return result;

            // Preserve allocation-free traversal for arrays and indexable inputs.
            if (data is byte[] bytes)
            {
                foreach (byte value in bytes)
                {
                    byte index = (byte)(result ^ value);
                    result = (ushort)((result >> 8) ^ crc_tabdnp[index]);
                }
            }
            else if (data is IList<byte> list)
            {
                int count = list.Count;
                for (int i = 0; i < count; i++)
                {
                    byte index = (byte)(result ^ list[i]);
                    result = (ushort)((result >> 8) ^ crc_tabdnp[index]);
                }
            }
            else
            {
                foreach (byte value in data)
                {
                    byte index = (byte)(result ^ value);
                    result = (ushort)((result >> 8) ^ crc_tabdnp[index]);
                }
            }

            result = (ushort)(result ^ 0xffff);
            return (ushort)((ushort)((result & 0xff) << 8) | ((result >> 8) & 0xff));
        }


        /*******************************************************************/
        /// <summary>
        /// *   The function InitDNPTable() is used  to  fill  the  array    *
        /// *   for calculation of the CRC-DNP with values.                     *
        /// </summary>
        /*******************************************************************/
        private static ushort[] GenerateDNPTable()
        {
            ushort[] table = new ushort[256];
            const ushort p_dnp = 0xA6BC;

            for (int i = 0; i < 256; i++)
            {
                ushort crc = 0;
                ushort c = (ushort)i;

                for (int j = 0; j < 8; j++)
                {

                    if (((crc ^ c) & 0x0001) != 0) crc = (ushort)((crc >> 1) ^ p_dnp);
                    else crc = (ushort)(crc >> 1);

                    c = (ushort)(c >> 1);
                }
                table[i] = crc;
            }

            return table;
        }
    }
}
