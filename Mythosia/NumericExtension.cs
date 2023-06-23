using System;
using System.Collections.Generic;
using System.Net;
using System.Numerics;
using System.Text;

namespace Mythosia
{
    public static class NumericExtension
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
        public static bool IsInRange(this byte value, byte minValue, byte maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this sbyte value, sbyte minValue, sbyte maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this short value, short minValue, short maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this ushort value, ushort minValue, ushort maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this int value, int minValue, int maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this uint value, uint minValue, uint maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this BigInteger value, BigInteger minValue, BigInteger maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this float value, float minValue, float maxValue)
            => minValue <= value && value <= maxValue;


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this double? value, double minValue, double maxValue)
            => value == null ? false : ((double)value).IsInRange(minValue, maxValue);


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this double value, double minValue, double maxValue)
            => minValue <= value && value <= maxValue;

        /*
        public static bool IsInRangeTest<T>(this T? value, T minValue, T maxValue) where T : struct, IComparable<T>
            => value is T v && minValue.CompareTo(v) <= 0 && maxValue.CompareTo(v) >= 0;
        */



        public static string ToSIPrefix<T>(this T number) where T : struct, IComparable, IFormattable, IConvertible
        {
            double value = Convert.ToDouble(number);

            string[] suffixes = { "", " k", " M", " G", " T", " P", " E", " Z", " Y" };
            int suffixIndex = 0;

            while (Math.Abs(value) >= 1e3 && suffixIndex < suffixes.Length - 1)
            {
                value /= 1e3;
                suffixIndex++;
            }

            return $"{value:0.##}{suffixes[suffixIndex]}";
        }


        public static IEnumerable<byte> ToByteArray(this short data) => BitConverter.GetBytes(data);
        public static IEnumerable<byte> ToByteArray(this ushort data) => BitConverter.GetBytes(data);
        public static IEnumerable<byte> ToByteArray(this int data) => BitConverter.GetBytes(data);
        public static IEnumerable<byte> ToByteArray(this uint data) => BitConverter.GetBytes(data);
        public static IEnumerable<byte> ToByteArray(this float data) => BitConverter.GetBytes(data);
        public static IEnumerable<byte> ToByteArray(this double data) => BitConverter.GetBytes(data);


        /// <summary>
        /// Converts a short value from host byte order to network byte order.
        /// </summary>
        public static short HostToNetworkEndian(this short data)
            => (BitConverter.IsLittleEndian) ? IPAddress.HostToNetworkOrder(data) : data;

        /// <summary>
        /// Converts a short value from network byte order to host byte order.
        /// </summary>
        public static short NetworkToHostEndian(this short data)
            => (BitConverter.IsLittleEndian) ? IPAddress.NetworkToHostOrder(data) : data;

        /// <summary>
        /// Converts a ushort value from host byte order to network byte order.
        /// </summary>
        public static ushort HostToNetworkEndian(this ushort data)
            => (BitConverter.IsLittleEndian) ? (ushort)IPAddress.HostToNetworkOrder((short)data) : data;

        /// <summary>
        /// Converts a ushort value from network byte order to host byte order.
        /// </summary>
        public static ushort NetworkToHostEndian(this ushort data)
            => (BitConverter.IsLittleEndian) ? (ushort)IPAddress.NetworkToHostOrder((short)data) : data;

        /// <summary>
        /// Converts a uint value from host byte order to network byte order.
        /// </summary>
        public static uint HostToNetworkEndian(this uint data)
            => (BitConverter.IsLittleEndian) ? (uint)IPAddress.HostToNetworkOrder((int)data) : data;

        /// <summary>
        /// Converts a uint value from network byte order to host byte order.
        /// </summary>
        public static uint NetworkToHostEndian(this uint data)
            => (BitConverter.IsLittleEndian) ? (uint)IPAddress.NetworkToHostOrder((int)data) : data;

        /// <summary>
        /// Converts an int value from host byte order to network byte order.
        /// </summary>
        public static int HostToNetworkEndian(this int data)
            => (BitConverter.IsLittleEndian) ? IPAddress.HostToNetworkOrder(data) : data;

        /// <summary>
        /// Converts an int value from network byte order to host byte order.
        /// </summary>
        public static int NetworkToHostEndian(this int data)
            => (BitConverter.IsLittleEndian) ? IPAddress.NetworkToHostOrder(data) : data;

        /// <summary>
        /// Converts a float value from host byte order to network byte order.
        /// </summary>
        public static float HostToNetworkEndian(this float data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Converts a float value from network byte order to host byte order.
        /// </summary>
        public static float NetworkToHostEndian(this float data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// Converts a double value from host byte order to network byte order.
        /// </summary>
        public static double HostToNetworkEndian(this double data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// Converts a double value from network byte order to host byte order.
        /// </summary>
        public static double NetworkToHostEndian(this double data)
        {
            byte[] bytes = BitConverter.GetBytes(data);
            if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
            return BitConverter.ToDouble(bytes, 0);
        }



        public static double ConvertEndian(this double data)
        {
            var distance = BitConverter.GetBytes(data);
            Array.Reverse(distance);
            return BitConverter.ToDouble(distance, 0);
        }
    }
}
