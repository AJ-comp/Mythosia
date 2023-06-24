using System;
using System.Collections.Generic;
using System.Linq;
using Mythosia;

namespace Mythosia.Integrity
{
    public static class ExtensionForChecksum
    {
        public enum CheckSum8Type
        {
            Xor,
            NMEA,
            Modulo256,
            TwosComplement,
        }


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means checksum algorithm type.</param>
        /// <see cref="https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/"/>
        /// <seealso cref="https://nmeachecksum.eqth.net/"/>
        /// <returns></returns>
        /*****************************************************/
        public static byte CheckSum8(this IEnumerable<byte> data, CheckSum8Type type = CheckSum8Type.Xor)
        {
            byte result = 0;

            if (type == CheckSum8Type.Xor) result = data.CheckSum8Xor();
            else if (type == CheckSum8Type.NMEA) result = data.CheckSum8Xor();
            else if (type == CheckSum8Type.Modulo256) result = data.CheckSum8Modulo256();
            else if (type == CheckSum8Type.TwosComplement) result = data.CheckSum8TwosComplement();

            return result;
        }

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included 1byte checksum value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means checksum algorithm type.</param>
        /// <returns></returns>
        /*****************************************************/
        public static IEnumerable<byte> WithCheckSum8(this IEnumerable<byte> data, CheckSum8Type type = CheckSum8Type.Xor)
        {
            if (type == CheckSum8Type.Xor) return data.Append(data.CheckSum8Xor());
            else if (type == CheckSum8Type.NMEA) return data.Append(data.CheckSum8Xor());
            else if (type == CheckSum8Type.Modulo256) return data.Append(data.CheckSum8Modulo256());
            else if (type == CheckSum8Type.TwosComplement) return data.Append(data.CheckSum8TwosComplement());

            return new List<byte>();
        }


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum that NMEA format use. <br/>
        /// </summary>
        /// <remarks>checksum8 xor</remarks>
        /// <param name="data"></param>
        /// <see cref="https://nmeachecksum.eqth.net/"/>
        /// <returns></returns>
        /*****************************************************/
        [Obsolete("This method is deprecated, please use the extension method .CheckSum8 instead.")]
        public static byte NMEACheckSum(this IEnumerable<byte> data) => data.CheckSum8Xor();

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included NMEA checksum value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*****************************************************/
        [Obsolete("This method is deprecated, please use the extension method .WithCheckSum8 instead.")]
        public static IEnumerable<byte> WithNMEACheckSum(this IEnumerable<byte> data) => data.WithCheckSum8Xor();


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum. [xor]
        /// </summary>
        /// <param name="data"></param>
        /// <see cref="https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/"/>
        /// <returns></returns>
        /*****************************************************/
        private static byte CheckSum8Xor(this IEnumerable<byte> data)
        {
            byte result = 0;

            for (int i = 0; i < data.Count(); i++) result ^= data.ElementAt(i);

            return result;
        }

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included 1byte checksum [xor] value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*****************************************************/
        private static IEnumerable<byte> WithCheckSum8Xor(this IEnumerable<byte> data)
        {
            List<byte> result = new List<byte>();
            result.AddRange(data);
            result.Add(data.CheckSum8Xor());

            return result;
        }


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum. [modulo-256]
        /// </summary>
        /// <param name="data"></param>
        /// <see cref="https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/"/>
        /// <returns></returns>
        /*****************************************************/
        private static byte CheckSum8Modulo256(this IEnumerable<byte> data)
        {
            UInt64 sum = 0;
            for (int i = 0; i < data.Count(); i++) sum += data.ElementAt(i);
            byte result = (byte)(sum % 256);

            return result;
        }

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included 1byte checksum [modulo-256] value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*****************************************************/
        private static IEnumerable<byte> WithCheckSum8Modulo256(this IEnumerable<byte> data)
        {
            List<byte> result = new List<byte>();
            result.AddRange(data);
            result.Add(data.CheckSum8Modulo256());

            return result;
        }


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum. [2's complement]
        /// </summary>
        /// <param name="data"></param>
        /// <see cref="https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/"/>
        /// <returns></returns>
        /*****************************************************/
        private static byte CheckSum8TwosComplement(this IEnumerable<byte> data)
        {
            UInt64 sum = 0;
            for (int i = 0; i < data.Count(); i++) sum += data.ElementAt(i);
            byte result = (byte)(0x100 - sum);

            return result;
        }

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included 1byte checksum [2's complement] value.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        /*****************************************************/
        private static IEnumerable<byte> WithCheckSum8TwosComplement(this IEnumerable<byte> data)
        {
            List<byte> result = new List<byte>();
            result.AddRange(data);
            result.Add(data.CheckSum8TwosComplement());

            return result;
        }
    }
}
