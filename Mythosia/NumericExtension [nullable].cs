using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace Mythosia
{
    public static partial class NumericExtension
    {
        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this byte? value, byte minValue, byte maxValue)
            => value == null ? false : ((byte)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this sbyte? value, sbyte minValue, sbyte maxValue)
            => value == null ? false : ((sbyte)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this short? value, short minValue, short maxValue)
            => value == null ? false : ((short)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this ushort? value, ushort minValue, ushort maxValue)
            => value == null ? false : ((ushort)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this int? value, int minValue, int maxValue)
           => value == null ? false : ((int)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this uint? value, uint minValue, uint maxValue)
            => value == null ? false : ((uint)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this long? value, long minValue, long maxValue)
           => value == null ? false : ((long)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this ulong? value, ulong minValue, ulong maxValue)
            => value == null ? false : ((ulong)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this BigInteger? value, BigInteger minValue, BigInteger maxValue)
            => value == null ? false : ((BigInteger)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this float? value, float minValue, float maxValue)
            => value == null ? false : ((float)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this double? value, double minValue, double maxValue)
            => value == null ? false : ((double)value).IsInRange(minValue, maxValue);
    }
}
