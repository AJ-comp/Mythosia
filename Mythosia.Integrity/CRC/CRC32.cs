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
        /// <summary>CRC-32/MPEG-2: polynomial 0x04C11DB7, initial state 0xFFFFFFFF, no reflection or final XOR.</summary>
        Mpeg2 = 2,
        /// <summary>CRC-32/BZIP2: polynomial 0x04C11DB7, no reflection, initial state and final XOR 0xFFFFFFFF.</summary>
        Bzip2 = 3,
        /// <summary>CRC-32/AUTOSAR (P4): polynomial 0xF4ACFB13, reflected input and output, initial state and final XOR 0xFFFFFFFF.</summary>
        AutosarP4 = 4,
    }

    public class CRC32 : ErrorDetection
    {
        public CRC32Type Type { get; } = CRC32Type.Classic;

        public CRC32(CRC32Type type = CRC32Type.Classic)
        {
            Type = type;
        }

        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            List<byte> result = new List<byte>();

            if (Type == CRC32Type.Basic) AppendNativeBytes(result, ComputeCRC32(source));
            else if (Type == CRC32Type.Classic) AppendNativeBytes(result, ComputeCRC32(source));
            else if (Type == CRC32Type.Mpeg2 || Type == CRC32Type.Bzip2 || Type == CRC32Type.AutosarP4)
                AppendNativeBytes(result, ComputeIncrementally(source));

            return result;
        }

        private uint ComputeIncrementally(IEnumerable<byte> source)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));

            var accumulator = new CRC32Accumulator(Type);
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

        private static void AppendNativeBytes(List<byte> result, uint value)
        {
            // Preserve the native byte order previously produced by Marshal.
            if (BitConverter.IsLittleEndian)
            {
                result.Add(unchecked((byte)value));
                result.Add(unchecked((byte)(value >> 8)));
                result.Add(unchecked((byte)(value >> 16)));
                result.Add(unchecked((byte)(value >> 24)));
                return;
            }

            result.Add(unchecked((byte)(value >> 24)));
            result.Add(unchecked((byte)(value >> 16)));
            result.Add(unchecked((byte)(value >> 8)));
            result.Add(unchecked((byte)value));
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
            int frameLength = sourceWithCRC.Count();
            if (frameLength <= 4) return true;
            var toCheckCRC = sourceWithCRC.TakeLast(4);

            var calculatedCRC = Compute(sourceWithCRC.Take(frameLength - 4));
            return !toCheckCRC.SequenceEqual(calculatedCRC);
        }


        public override string GetDetectionType() => Type.ToString();


        // Publish only the completed table through thread-safe CLR initialization.
        private static readonly uint[] crc_tab32 = GenerateCRC32Table();


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
            // Preserve allocation-free traversal for arrays and indexable inputs.
            if (data is byte[] bytes)
            {
                foreach (byte value in bytes)
                    crc = (crc >> 8) ^ crc_tab32[(crc ^ value) & 0xFF];
            }
            else if (data is IList<byte> list)
            {
                int count = list.Count;
                for (int i = 0; i < count; i++)
                    crc = (crc >> 8) ^ crc_tab32[(crc ^ list[i]) & 0xFF];
            }
            else
            {
                foreach (byte value in data)
                    crc = (crc >> 8) ^ crc_tab32[(crc ^ value) & 0xFF];
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
        private static uint[] GenerateCRC32Table()
        {
            uint[] table = new uint[256];
            const uint P_32 = 0xEDB88320;

            for (uint n = 0; n < 256; n++)
            {
                uint c = n;
                for (int k = 0; k < 8; k++)
                {
                    var res = c & 1;
                    c = (res == 1) ? (P_32 ^ (c >> 1)) : (c >> 1);
                }
                table[n] = c;
            }

            return table;
        }
    }
}
