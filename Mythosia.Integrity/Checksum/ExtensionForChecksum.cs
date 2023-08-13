using System;
using System.Collections.Generic;
using System.Linq;

namespace Mythosia.Integrity.Checksum
{
    public static class ExtensionForChecksum
    {
        [Obsolete("The CheckSum8 method is obsolete and will be removed in the future versions. Consider using Checksum8 method instead.")]
        public static byte CheckSum8(this IEnumerable<byte> data, CheckSum8Type type = CheckSum8Type.Xor)
        {
            Checksum8Type newType = Checksum8Type.Xor;

            if (type == CheckSum8Type.NMEA) newType = Checksum8Type.NMEA;
            else if (type == CheckSum8Type.Modulo256) newType = Checksum8Type.Modulo256;
            else if (type == CheckSum8Type.TwosComplement) newType = Checksum8Type.TwosComplement;

            return new Checksum8(newType).Compute(data).ElementAt(0);
        }

        [Obsolete("The WithCheckSum8 method is obsolete and will be removed in the future versions. Consider using WithChecksum8 method instead.")]
        public static IEnumerable<byte> WithCheckSum8(this IEnumerable<byte> data, CheckSum8Type type = CheckSum8Type.Xor)
        {
            Checksum8Type newType = Checksum8Type.Xor;

            if (type == CheckSum8Type.NMEA) newType = Checksum8Type.NMEA;
            else if (type == CheckSum8Type.Modulo256) newType = Checksum8Type.Modulo256;
            else if (type == CheckSum8Type.TwosComplement) newType = Checksum8Type.TwosComplement;

            return new Checksum8(newType).Encode(data);
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
        public static byte Checksum8(this IEnumerable<byte> data, Checksum8Type type = Checksum8Type.Xor)
        {
            return new Checksum8(type).Compute(data).ElementAt(0);
        }

        /*****************************************************/
        /// <summary>
        /// This function returns the byte array included 1byte checksum value.
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type">This parameter means checksum algorithm type.</param>
        /// <returns></returns>
        /*****************************************************/
        public static IEnumerable<byte> WithChecksum8(this IEnumerable<byte> data, Checksum8Type type = Checksum8Type.Xor)
        {
            return new Checksum8(type).Encode(data);
        }
    }
}
