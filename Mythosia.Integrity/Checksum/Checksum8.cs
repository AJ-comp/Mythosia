using Mythosia.Integrity.CRC;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mythosia.Integrity.Checksum
{
    [Obsolete("CheckSum8Type is obsolete. Use Checksum8Type instead.")]
    public enum CheckSum8Type
    {
        Xor,
        NMEA,
        Modulo256,
        TwosComplement,
    }

    public enum Checksum8Type
    {
        Xor,
        NMEA,
        Modulo256,
        TwosComplement,
    }

    public class Checksum8 : ErrorDetection
    {
        public Checksum8Type Type { get; } = Checksum8Type.Xor;

        public Checksum8(Checksum8Type type = Checksum8Type.Xor)
        {
            Type = type;
        }


        public override IEnumerable<byte> Compute(IEnumerable<byte> source)
        {
            List<byte> result = new List<byte>();
            if (source.Count() == 0) return result;

            if (Type == Checksum8Type.Xor) result.Add(Checksum8Xor(source));
            else if (Type == Checksum8Type.NMEA) result.Add(Checksum8Xor(source));
            else if (Type == Checksum8Type.Modulo256) result.Add(Checksum8Modulo256(source));
            else if (Type == Checksum8Type.TwosComplement) result.Add(Checksum8TwosComplement(source));

            return result;
        }

        public override IEnumerable<byte> Decode(IEnumerable<byte> sourceWithCRC)
        {
            List<byte> result = new List<byte>();
            if (IsError(sourceWithCRC)) return result;

            return sourceWithCRC.Take(sourceWithCRC.Count() - 1);
        }

        public override IEnumerable<byte> Encode(IEnumerable<byte> source)
            => source.Append(Compute(source).ElementAt(0));

        public override bool IsError(IEnumerable<byte> sourceWithCRC)
        {
            if (sourceWithCRC.Count() < 2) return true;

            return sourceWithCRC.Last() != sourceWithCRC.Take(sourceWithCRC.Count() - 1).Checksum8(Type);
        }

        public override string GetDetectionType() => Type.ToString();


        /*****************************************************/
        /// <summary>
        /// This function returns 1byte checksum. [xor]
        /// </summary>
        /// <param name="data"></param>
        /// <see cref="https://www.scadacore.com/tools/programming-calculators/online-checksum-calculator/"/>
        /// <returns></returns>
        /*****************************************************/
        private byte Checksum8Xor(IEnumerable<byte> data)
        {
            byte result = 0;

            for (int i = 0; i < data.Count(); i++) result ^= data.ElementAt(i);

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
        private static byte Checksum8Modulo256(IEnumerable<byte> data)
        {
            ulong sum = 0;
            for (int i = 0; i < data.Count(); i++) sum += data.ElementAt(i);
            byte result = (byte)(sum % 256);

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
        private static byte Checksum8TwosComplement(IEnumerable<byte> data)
        {
            ulong sum = 0;
            for (int i = 0; i < data.Count(); i++) sum += data.ElementAt(i);
            byte result = (byte)(0x100 - sum);

            return result;
        }
    }
}
