using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Numerics;
using System.Text;

namespace Mythosia
{
    public enum SIPrefixUnit
    {
        Auto = 0,
        Mili,
        Micro,
        Nano,
        Pico,

        Kilo = 101,
        Mega,
        Giga,
        Tera,
        Peta,
        Exa,
        Zetta,
        Yotta
    }



    public static partial class NumericExtension
    {
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
        public static bool IsInRange(this sbyte value, sbyte minValue, sbyte maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this ushort value, ushort minValue, ushort maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this uint value, uint minValue, uint maxValue)
            => minValue <= value && value <= maxValue;


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this long value, long minValue, long maxValue)
            => minValue <= value && value <= maxValue;


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this ulong value, ulong minValue, ulong maxValue)
            => minValue <= value && value <= maxValue;


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
        public static bool IsInRange(this float value, float minValue, float maxValue)
            => minValue <= value && value <= maxValue;


        /// <summary>
        /// check the condition <b><i>minValue <= value && value <= maxValue</i></b>
        /// </summary>
        /// <param name="value"></param>
        /// <param name="minValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static bool IsInRange(this double value, double minValue, double maxValue)
            => minValue <= value && value <= maxValue;


        /// <summary>
        /// Converts a numeric value to a prefixed hexadecimal string representation using Marshal serialization.
        /// The numeric value is first serialized using the Marshal class, and then the resulting byte array
        /// is converted to a prefixed hexadecimal string. Optionally, the hex digits can be separated by a specified connector.
        /// </summary>
        /// <typeparam name="T">The type of the numeric value.</typeparam>
        /// <param name="number">The numeric value to be converted.</param>
        /// <param name="separated">Specifies whether to separate the hex digits with a connector.</param>
        /// <returns>The prefixed hexadecimal string representation of the numeric value.</returns>
        public static string ToPrefixedHexString<T>(this T number, bool separated = false) where T : struct, IComparable, IFormattable, IConvertible
            => number.SerializeUsingMarshal().ToPrefixedHexString(separated);


        /// <summary>
        /// Converts a numeric value to an unprefixed hexadecimal string representation using Marshal serialization.
        /// The numeric value is first serialized using the Marshal class, and then the resulting byte array
        /// is converted to an unprefixed hexadecimal string. The hex digits are separated by a specified connector.
        /// </summary>
        /// <typeparam name="T">The type of the numeric value.</typeparam>
        /// <param name="number">The numeric value to be converted.</param>
        /// <param name="connector">The connector used to separate the hex digits.</param>
        /// <returns>The unprefixed hexadecimal string representation of the numeric value.</returns>
        public static string ToUnPrefixedHexString<T>(this T number, string connector = " ") where T : struct, IComparable, IFormattable, IConvertible
            => number.SerializeUsingMarshal().ToUnPrefixedHexString(connector);


        /// <summary>
        /// Converts the numeric value to a string representation with SI prefix (k, M, G, etc.).
        /// </summary>
        /// <typeparam name="T">The type of the numeric value.</typeparam>
        /// <param name="number">The numeric value to convert.</param>
        /// <returns>A string representation of the numeric value with SI prefix.</returns>
        /// <remarks>
        /// This method converts the numeric value to a string representation using SI prefixes (kilo, mega, giga, etc.) to indicate
        /// the magnitude of the value. The value is divided by 1,000 and a corresponding SI prefix is added (e.g., k for kilo, M for mega).
        /// The conversion is performed up to the Yotta (Y) prefix.
        /// </remarks>
        public static string ToSIPrefix<T>(this T number, SIPrefixUnit unit = SIPrefixUnit.Auto, int decimalCount = 2) where T : struct, IComparable, IFormattable, IConvertible
            => number.ToSIPrefixCore(unit, decimalCount);


        internal static string ToSIPrefixCore<T>(this T number, SIPrefixUnit unit, int decimalCount) where T : struct, IComparable, IFormattable, IConvertible
        {
            double value = Convert.ToDouble(number);

            string[] decimalSuffixes = { "", " m", "μ", " n", " p" };
            string[] suffixes = { "", " k", " M", " G", " T", " P", " E", " Z", " Y" };
            int suffixIndex = (int)unit;

            string result = string.Empty;
            if (suffixIndex == 0)   // if auto
            {
                if (value >= 1.0) result = value.FindSuitableValueInteger(decimalCount);
                else result = value.FindSuitableValueForDecimal(decimalCount);
            }
            else
            {
                if(suffixIndex < 100)
                {
                    for (int i = 0; i < suffixIndex; i++) value *= 1000;

                    string format = $"0.{new string('#', decimalCount)}";
                    result = $"{value.ToString(format)}{decimalSuffixes[suffixIndex]}";
                }
                else
                {
                    suffixIndex -= 100;
                    value /= Math.Pow(1e3, suffixIndex);

                    string format = $"0.{new string('#', decimalCount)}";
                    result = $"{value.ToString(format)}{suffixes[suffixIndex]}";
                }
            }

            return result;
        }


        internal static string FindSuitableValueInteger(this double value, int decimalCount)
        {
            string[] suffixes = { "", " k", " M", " G", " T", " P", " E", " Z", " Y" };

            int index = 0;
            while (Math.Abs(value) >= 1e3 && index < suffixes.Length - 1)
            {
                value /= 1e3;
                index++;
            }

            string format = $"0.{new string('#', decimalCount)}";
            return $"{value.ToString(format)}{suffixes[index]}";
        }

        internal static string FindSuitableValueForDecimal(this double value, int decimalCount)
        {
            string[] prefixes = { "", "m", "µ", "n", "p" };

            int index = 0;
            while (value < 1.0 && index > -prefixes.Length + 1)
            {
                value *= 1000.0;
                index--;
            }

            string format = $"0.{new string('#', decimalCount)}";
            return $"{value.ToString(format)}{prefixes[-index]}";
        }


        /*
        public static string ToSIPrefix(this double value)
        {
            if (value >= 1.0)
            {
                string[] suffixes = { "", " k", " M", " G", " T", " P", " E", " Z", " Y" };
                int suffixIndex = 0;

                while (Math.Abs(value) >= 1e3 && suffixIndex < suffixes.Length - 1)
                {
                    value /= 1e3;
                    suffixIndex++;
                }

                return $"{value:0.##}{suffixes[suffixIndex]}";
            }
            else
            {
                string[] prefixes = { "", "m", "µ", "n", "p" };  // SI 접두사 배열: 단위 없음, 밀리, 마이크로, 나노, 피코

                int index = 0;
                while (value < 1.0 && index > -prefixes.Length + 1)
                {
                    value *= 1000.0;
                    index--;
                }

                return $"{value:0.#####} {prefixes[-index]}";
            }
        }
        */


        public static byte[] ToByteArray<T>(this T data) where T : struct, IComparable, IFormattable, IConvertible
            => data.SerializeUsingMarshal();


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
        /// Converts a uint value from host byte order to network byte order.
        /// </summary>
        public static ulong HostToNetworkEndian(this ulong data)
            => (BitConverter.IsLittleEndian) ? (ulong)IPAddress.HostToNetworkOrder((long)data) : data;

        /// <summary>
        /// Converts a uint value from network byte order to host byte order.
        /// </summary>
        public static ulong NetworkToHostEndian(this ulong data)
            => (BitConverter.IsLittleEndian) ? (ulong)IPAddress.NetworkToHostOrder((long)data) : data;

        /// <summary>
        /// Converts an int value from host byte order to network byte order.
        /// </summary>
        public static long HostToNetworkEndian(this long data)
            => (BitConverter.IsLittleEndian) ? IPAddress.HostToNetworkOrder(data) : data;

        /// <summary>
        /// Converts an int value from network byte order to host byte order.
        /// </summary>
        public static long NetworkToHostEndian(this long data)
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



        internal static bool IsNumericType<T>(this T data) where T : struct, IComparable, IFormattable, IConvertible
        {
            bool result = true;
            try
            {
                double value = Convert.ToDouble(data);
            }
            catch
            {
                result = false;
            }

            return result;
        }
    }
}
