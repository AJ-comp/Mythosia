using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;

namespace Mythosia
{
    public static class StringExtension
    {
        public static double ToDouble(this string value) => Convert.ToDouble(value);
        public static float ToFloat(this string value) => Convert.ToSingle(value);
        public static byte ToByte(this string value) => Convert.ToByte(value);
        public static sbyte ToSByte(this string value) => Convert.ToSByte(value);
        public static short ToInt16(this string value) => Convert.ToInt16(value);
        public static ushort ToUInt16(this string value) => Convert.ToUInt16(value);
        public static int ToInt32(this string value) => Convert.ToInt32(value);
        public static uint ToUInt32(this string value) => Convert.ToUInt32(value);


        /*******************************************************************************/
        /// <summary>
        /// returns the string between startStr and endStr<br/>
        /// </summary>
        /// <remarks>
        /// total string : param:[abcdefg]:ok\r\n; <br/>
        /// startStr : [ <br/>
        /// endStr : ] <br/>
        /// result : abcdefg <br/>
        /// </remarks>
        /// <param name="value"></param>
        /// <param name="startStr"></param>
        /// <param name="endStr"></param>
        /// <param name="bInclude">if true the startStr and endStr is included</param>
        /// <returns></returns>
        /*******************************************************************************/
        public static string GetBetweenStr(this string value, string startStr, string endStr, bool bInclude = false)
        {
            string result = string.Empty;

            var u_Pos = value.IndexOf(startStr);
            if (u_Pos < 0) return result;
            if (bInclude == false) u_Pos += startStr.Length;

            var e_Pos = value.IndexOf(endStr, u_Pos);
            if (e_Pos < 0) return result;
            if (bInclude) e_Pos += endStr.Length;

            for (var i = u_Pos; i < e_Pos; i++) result += value[i];

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Repeats the given string a specified number of times.
        /// </summary>
        /// <param name="value">The string to repeat.</param>
        /// <param name="count">The number of times to repeat the string.</param>
        /// <returns>
        /// A new string that consists of the original string repeated the specified number of times.
        /// </returns>
        /*******************************************************************************/
        public static string Repeat(this string value, int count)
        {
            if(value == null) return string.Empty;

            var result = string.Empty;

            for (int i = 0; i < count; i++) result += value;

            return result;
        }



        public static byte[] ToEncodingArray(this string value, Encoding encoding)
        {
            if (value == null)
            {
                List<byte> result = new List<byte>();
                return result.ToArray();
            }

            return encoding.GetBytes(value);
        }

        public static byte[] ToDefaultArray(this string value) => value.ToEncodingArray(Encoding.Default);
        public static byte[] ToASCIIArray(this string value) => value.ToEncodingArray(Encoding.ASCII);
        public static byte[] ToUTF7Array(this string value) => value.ToEncodingArray(Encoding.UTF7);
        public static byte[] ToUTF8Array(this string value) => value.ToEncodingArray(Encoding.UTF8);
        public static byte[] ToUTF16Array(this string value) => value.ToEncodingArray(Encoding.Unicode);
        public static byte[] ToUTF32Array(this string value) => value.ToEncodingArray(Encoding.UTF32);

    }
}
