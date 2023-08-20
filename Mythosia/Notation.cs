using System;
using System.Collections.Generic;
using System.Text;

namespace Mythosia
{
    public enum HexPartitionSize
    {
        None = 0,
        Byte = 2,
        Byte2 = 4,
    }

    public enum BinaryPartitionSize
    {
        None = 0,
        Bit = 1,
        Bit2 = 2,
        HalfByte = 4,
        Byte = 8,
    }


    public enum OctalPartitionSize
    {
        None = 0,
        Byte = 3,
    }


    public static class ExtensionForNotation
    {
        /*******************************************************************************/
        /// <summary>
        /// Converts the elements of an enumerable collection to their decimal string representations, separated by a delimiter.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="list">The enumerable collection to convert.</param>
        /// <param name="delimiter">The delimiter string used to separate the decimal string representations. Default value is a single space.</param>
        /// <returns>A string containing the decimal string representations of the elements in the collection, separated by the specified delimiter.</returns>
        /// <exception cref="ArgumentException">Thrown when an element of the collection cannot be converted to decimal.</exception>
        /*******************************************************************************/
        public static string ToDecimalString<T>(this IEnumerable<T> list, string delimiter = " ")
        {
            StringBuilder decimalString = new StringBuilder();

            foreach (var value in list)
            {
                // if the type is not converted to decimal then occurs exception.
                decimalString.Append(Convert.ToDecimal(value).ToString());
                decimalString.Append(delimiter);
            }

            if (decimalString.Length > 0)
                decimalString.Length -= delimiter.Length; // Remove the last delimiter

            return decimalString.ToString();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts an IEnumerable of bytes to its binary string representation.
        /// </summary>
        /// <param name="bytes">The bytes to be converted.</param>
        /// <param name="partitionSize">The size to partition the binary string. Default is 
        /// <see cref="BinaryPartitionSize.None"/> meaning no partitioning.</param>
        /// <param name="prefix">Optional prefix to add before each partitioned segment of the binary string.</param>
        /// <param name="postfix">Optional postfix to add after each partitioned segment of the binary string.</param>
        /// <returns>A binary string representation of the input bytes, partitioned as specified, with optional prefix and postfix.</returns>
        /*******************************************************************************/
        public static string ToBinaryString(this IEnumerable<byte> bytes, BinaryPartitionSize partitionSize = BinaryPartitionSize.None, string prefix = "", string postfix = "")
            => GenerateBinaryString(bytes, partitionSize, prefix, postfix);


        /*******************************************************************************/
        /// <summary>
        /// Converts an IEnumerable of bytes to its binary string representation.
        /// </summary>
        /// <param name="bytes">The bytes to be converted.</param>
        /// <param name="partitionSize">The size to partition the binary string. Default is 
        /// <see cref="BinaryPartitionSize.None"/> meaning no partitioning.</param>
        /// <param name="prefix">Optional prefix to add before each partitioned segment of the binary string.</param>
        /// <param name="postfix">Optional postfix to add after each partitioned segment of the binary string.</param>
        /// <returns>A binary string representation of the input bytes, partitioned as specified, with optional prefix and postfix.</returns>
        /*******************************************************************************/
        public static string ToBinaryString(this IEnumerable<sbyte> bytes, BinaryPartitionSize partitionSize = BinaryPartitionSize.None, string prefix = "", string postfix = "")
            => GenerateBinaryString(bytes, partitionSize, prefix, postfix);

        private static string GenerateBinaryString<T>(IEnumerable<T> bytes, BinaryPartitionSize partitionSize, string prefix = "", string postfix = "") where T : IConvertible
        {
            int partitionIntSize = (int)partitionSize;
            StringBuilder binaryStringBuilder = new StringBuilder();

            foreach (var b in bytes)
            {
                string binarySegment = Convert.ToString(Convert.ToInt32(b), 2).PadLeft(8, '0');

                if (partitionSize == BinaryPartitionSize.None) binaryStringBuilder.Append(binarySegment);
                else
                {
                    for (int i = 0; i < binarySegment.Length; i += partitionIntSize)
                    {
                        // Add partitioned segment with a space
                        binaryStringBuilder.Append(prefix)
                                                   .Append(binarySegment.Substring(i, Math.Min(partitionIntSize, binarySegment.Length - i)))
                                                   .Append(postfix)
                                                   .Append(' ');
                    }
                }
            }

            // Remove trailing space and return
            return (partitionSize == BinaryPartitionSize.None) ? prefix + binaryStringBuilder.ToString().TrimEnd() + postfix
                                                                                    : binaryStringBuilder.ToString().TrimEnd();
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts a given IEnumerable of bytes into a lowercase hexadecimal string representation with optional partitioning, prefix, and postfix.
        /// </summary>
        /// <param name="byteValues">The bytes to be converted to a hexadecimal string.</param>
        /// <param name="partitionSize">The size of the partition for dividing the output string. Defaults to None.</param>
        /// <param name="prefix">An optional prefix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <param name="postfix">An optional postfix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <returns>A string representation of the byte values in hexadecimal format with optional partitioning, prefix, and postfix.</returns>
        /*******************************************************************************/
        public static string ToHexStringL(this IEnumerable<byte> byteValues, HexPartitionSize partitionSize = HexPartitionSize.None, string prefix = "", string postfix = "")
            => InternalToHexString(byteValues, "x2", partitionSize, prefix, postfix);


        /*******************************************************************************/
        /// <summary>
        /// Converts a given IEnumerable of bytes into a uppercase hexadecimal string representation with optional partitioning, prefix, and postfix.
        /// </summary>
        /// <param name="byteValues">The bytes to be converted to a hexadecimal string.</param>
        /// <param name="partitionSize">The size of the partition for dividing the output string. Defaults to None.</param>
        /// <param name="prefix">An optional prefix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <param name="postfix">An optional postfix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <returns>A string representation of the byte values in hexadecimal format with optional partitioning, prefix, and postfix.</returns>
        /*******************************************************************************/
        public static string ToHexStringU(this IEnumerable<byte> byteValues, HexPartitionSize partitionSize = HexPartitionSize.None, string prefix = "", string postfix = "")
            => InternalToHexString(byteValues, "X2", partitionSize, prefix, postfix);


        /*******************************************************************************/
        /// <summary>
        /// Converts a given IEnumerable of bytes into a lowercase hexadecimal string representation with optional partitioning, prefix, and postfix.
        /// </summary>
        /// <param name="byteValues">The bytes to be converted to a hexadecimal string.</param>
        /// <param name="partitionSize">The size of the partition for dividing the output string. Defaults to None.</param>
        /// <param name="prefix">An optional prefix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <param name="postfix">An optional postfix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <returns>A string representation of the byte values in hexadecimal format with optional partitioning, prefix, and postfix.</returns>
        /*******************************************************************************/
        public static string ToHexStringL(this IEnumerable<sbyte> sbyteValues, HexPartitionSize partitionSize = HexPartitionSize.None, string prefix = "", string postfix = "")
            => InternalToHexString(sbyteValues, "x2", partitionSize, prefix, postfix);


        /*******************************************************************************/
        /// <summary>
        /// Converts a given IEnumerable of bytes into a uppercase hexadecimal string representation with optional partitioning, prefix, and postfix.
        /// </summary>
        /// <param name="byteValues">The bytes to be converted to a hexadecimal string.</param>
        /// <param name="partitionSize">The size of the partition for dividing the output string. Defaults to None.</param>
        /// <param name="prefix">An optional prefix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <param name="postfix">An optional postfix to be added to each partitioned segment. Defaults to an empty string.</param>
        /// <returns>A string representation of the byte values in hexadecimal format with optional partitioning, prefix, and postfix.</returns>
        /*******************************************************************************/
        public static string ToHexStringU(this IEnumerable<sbyte> sbyteValues, HexPartitionSize partitionSize = HexPartitionSize.None, string prefix = "", string postfix = "")
            => InternalToHexString(sbyteValues, "X2", partitionSize, prefix, postfix);


        private static string InternalToHexString(string hexValue, HexPartitionSize partitionSize, string prefix = "", string postfix = "")
        {
            if (partitionSize == HexPartitionSize.None) return prefix + hexValue + postfix;

            int partitionIntSize = (int)partitionSize;
            StringBuilder partitionedStringBuilder = new StringBuilder();

            // Start from the end and move to the front.
            for (int i = hexValue.Length; i > 0; i -= partitionIntSize)
            {
                // Calculate start index for the substring, ensure it's not negative.
                int startIndex = Math.Max(i - partitionIntSize, 0);
                string partition = prefix + hexValue.Substring(startIndex, i - startIndex) + postfix;

                partitionedStringBuilder.Insert(0, partition).Insert(0, ' ');
            }

            return partitionedStringBuilder.ToString().TrimStart();
        }


        private static string InternalToHexString(IEnumerable<byte> byteValues, string format, HexPartitionSize partitionSize, string prefix, string postfix)
        {
            StringBuilder hexStringBuilder = new StringBuilder();
            foreach (var b in byteValues)
            {
                hexStringBuilder.Append(b.ToString(format));
            }

            return InternalToHexString(hexStringBuilder.ToString(), partitionSize, prefix, postfix);
        }


        private static string InternalToHexString(this IEnumerable<sbyte> sbyteValues, string format, HexPartitionSize partitionSize, string prefix, string postfix)
        {
            StringBuilder hexStringBuilder = new StringBuilder();
            foreach (var b in sbyteValues)
            {
                hexStringBuilder.Append(((byte)b).ToString(format));
            }

            return InternalToHexString(hexStringBuilder.ToString(), partitionSize, prefix, postfix);
        }



        /*
        public static string ToOctalString(this IEnumerable<byte> bytes, OctalPartitionSize partitionSize = OctalPartitionSize.None, string prefix = "", string postfix = "")
            => ConvertToOctalWithPartition(bytes, partitionSize, prefix, postfix);


        public static string ToOctalString(this IEnumerable<sbyte> bytes, OctalPartitionSize partitionSize = OctalPartitionSize.None, string prefix = "", string postfix = "")
            => ConvertToOctalWithPartition(bytes, partitionSize, prefix, postfix);


        private static string ConvertToOctalWithPartition(IEnumerable<byte> bytes, OctalPartitionSize partitionSize = OctalPartitionSize.None, string prefix = "", string postfix = "")
        {
            string octalStr = ConvertToOctal(bytes);
            if (partitionSize == OctalPartitionSize.None) return prefix + octalStr + postfix;

            int partitionIntSize = (int)partitionSize;
            StringBuilder result = new StringBuilder();

            for (int i = 0; i < octalStr.Length; i += partitionIntSize)
            {
                result.Append(prefix)
                      .Append(octalStr.Substring(i, Math.Min(partitionIntSize, octalStr.Length - i)))
                      .Append(postfix)
                      .Append(' ');
            }

            return result.ToString().TrimEnd();
        }

        // 이전에 제공된 ConvertToOctal 함수
        private static string ConvertToOctal(IEnumerable<byte> bytes)
        {
            StringBuilder result = new StringBuilder();

            foreach (byte b in bytes)
            {
                int first = (b >> 5) & 0x07;
                int second = (b >> 2) & 0x07;
                int third = b & 0x03;

                result.Append(first.ToString());
                result.Append(second.ToString());
                if (third != 0)
                    result.Append(third.ToString());
            }

            return result.ToString();
        }
        */
    }
}
