using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mythosia.Integrity.CRC
{
    public enum CRC32Type
    {
        [Obsolete("Basic is obsolete. Use Classic instead.")]
        Basic,
        Classic,
    }

    public class CRC32 : ErrorDetection
    {
        public CRC32Type Type { get; } = CRC32Type.Classic;

        public CRC32(CRC32Type type = CRC32Type.Classic)
        {
            Type = type;

            if (type == CRC32Type.Basic)
            {
                if (crc_tab32 == null) GenerateCRC32Table();
            }
            else if (type == CRC32Type.Classic)
            {
                if (crc_tab32 == null) GenerateCRC32Table();
            }
        }

        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            List<byte> result = new List<byte>();

            if (Type == CRC32Type.Basic) result.AddRange(ComputeCRC32(source).ToByteArray());
            else if (Type == CRC32Type.Classic) result.AddRange(ComputeCRC32(source).ToByteArray());

            return result;
        }

        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            List<byte> result = new List<byte>();
            if (IsError(sourceWithCRC)) return result;

            return sourceWithCRC.Take(sourceWithCRC.Count() - 4);
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
            if (sourceWithCRC.Count() <= 4) return true;
            var toCheckCRC = sourceWithCRC.TakeLast(4);

            var calculatedCRC = Compute(sourceWithCRC.Take(sourceWithCRC.Count() - 4));
            return !toCheckCRC.SequenceEqual(calculatedCRC);
        }


        public override string GetDetectionType() => Type.ToString();


        private static uint[] crc_tab32;


        /*******************************************************************/
        /// <summary>
        /// This function returns the CRC32 value
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*******************************************************************/
        private uint ComputeCRC32(IEnumerable<byte> data)
        {
            long result = 0;
            if (data.Count() <= 0) return (uint)result;

            uint crc = 0xffffffff;
            for (int i = 0; i < data.Count(); i++)
            {
                var c = data.ElementAt(i);
                crc = (crc >> 8) ^ crc_tab32[(crc ^ c) & 0xFF];
            }

            return ~crc; //(crc ^ (-1)) >> 0;
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
        /// *   The function InitCRC32Table() is used  to  fill  the  array     *
        /// *   for calculation of the CRC-32 with values.                      *
        /// </summary>
        /*******************************************************************/
        private static void GenerateCRC32Table()
        {
            crc_tab32 = new uint[256];  // ulong?
            const uint P_32 = 0xEDB88320;

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
        }
    }
}
