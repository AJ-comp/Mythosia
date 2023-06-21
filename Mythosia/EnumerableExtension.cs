using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Mythosia
{
    public static class EnumerableExtension
    {
        public static IEnumerable<byte> Append(this IEnumerable<byte> data, byte toAddData)
        {
            var result = data.ToList();
            if (data.Count() <= 0) return result;

            result.Add(toAddData);

            return result;
        }


        public static IEnumerable<byte> AppendRange(this IEnumerable<byte> data, IEnumerable<byte> toAddData)
        {
            var result = data.ToList();
            if (data.Count() <= 0) return result;

            result.AddRange(toAddData);

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Joins the elements of an enumerable collection into a single string using the specified connector.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="obj">The enumerable collection to join.</param>
        /// <param name="connector">The string used to connect the elements in the resulting string. Default value is a comma (,).</param>
        /// <returns>A string that contains the joined elements of the collection.</returns>
        /*******************************************************************************/
        public static string JoinItems<T>(this IEnumerable<T> obj, string connector = ",") => string.Join(connector, obj);


        /*******************************************************************************/
        /// <summary>
        /// Returns the index of the first occurrence of a specified subsequence within the source sequence.
        /// </summary>
        /// <typeparam name="T">The type of elements in the sequences.</typeparam>
        /// <param name="obj">The source sequence to search within.</param>
        /// <param name="param">The subsequence to locate within the source sequence.</param>
        /// <returns>
        /// The zero-based index of the first occurrence of the specified subsequence within the source sequence,
        /// if found; otherwise, -1.
        /// </returns>
        /*******************************************************************************/
        public static int IndexOf<T>(this IEnumerable<T> obj, IEnumerable<T> param)
        {
            if (param.Count() == 0) return -1;

            int result = -1;
            var matchedCandidate = new List<int>();
            for (int i = 0; i < obj.Count(); i++)
            {
                // The time complexity of ElementAt(i) is O(1) because try to access after to convert to IList<T> internally except HashSet.
                if (obj.ElementAt(i).Equals(param.ElementAt(0))) matchedCandidate.Add(i);
            }

            foreach (var item in matchedCandidate)
            {
                var candidate = obj.Skip(item).Take(param.Count());
                if (candidate.SequenceEqual(param))
                {
                    result = item;
                    break;
                }
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds a specified element to the collection, excluding null values.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="obj">The collection to which the element should be added.</param>
        /// <param name="data">The element to add to the collection.</param>
        /// <remarks>
        /// This extension method checks if the provided element is null. If the element is not null,
        /// it is added to the collection using the <see cref="ICollection{T}.Add"/> method.
        /// If the element is null, no action is taken, and the method returns without modifying the collection.
        /// </remarks>
        /*******************************************************************************/
        public static void AddExceptNull<T>(this ICollection<T> obj, T data)
        {
            if (data == null) return;

            obj.Add(data);
        }


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
        /// Converts the given signed byte array to an unprefixed hexadecimal string representation.
        /// </summary>
        /// <param name="data">The signed byte array to convert.</param>
        /// <param name="connector">
        /// The optional connector string to separate each hexadecimal value. Default is a single space.
        /// </param>
        /// <returns>The unprefixed hexadecimal string representation of the signed byte array.</returns>
        /*******************************************************************************/
        public static string ToUnPrefixedHexString(this IEnumerable<byte> data, string connector = " ")
        {
            string result = string.Empty;

            foreach (var item in data)
            {
                result += item.ToString("x2") + connector;
            }

            if (data.Count() > 0) result = result.Substring(0, result.Length - connector.Length); // remove last connector

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts the given signed byte array to an unprefixed hexadecimal string representation.
        /// </summary>
        /// <param name="data">The signed byte array to convert.</param>
        /// <param name="connector">
        /// The optional connector string to separate each hexadecimal value. Default is a single space.
        /// </param>
        /// <returns>The unprefixed hexadecimal string representation of the signed byte array.</returns>
        /*******************************************************************************/
        public static string ToUnPrefixedHexString(this IEnumerable<sbyte> data, string connector = " ")
        {
            string result = string.Empty;

            foreach (var item in data)
            {
                result += item.ToString("x2") + connector;
            }

            if (data.Count() > 0) result = result.Substring(0, result.Length - connector.Length); // remove last connector

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts the given byte array to a prefixed hexadecimal string representation.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The prefixed hexadecimal string representation of the byte array.</returns>
        /*******************************************************************************/
        public static string ToPrefixedHexString(this IEnumerable<byte> value, bool separated = false)
        {
            var result = ToUnPrefixedHexString(value, separated ? " " : "");
            if (result.Length > 0) result = "0x" + result;
            if (separated) result = result.Replace(" ", " 0x");

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Converts the given byte array to a prefixed hexadecimal string representation.
        /// </summary>
        /// <param name="value">The byte array to convert.</param>
        /// <returns>The prefixed hexadecimal string representation of the byte array.</returns>
        /*******************************************************************************/
        public static string ToPrefixedHexString(this IEnumerable<sbyte> value, bool separated = false)
        {
            var result = ToUnPrefixedHexString(value, separated ? " " : "");
            if (result.Length > 0) result = "0x" + result;
            if (separated) result = result.Replace(" ", " 0x");

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Determines whether the array contains all the specified values.
        /// </summary>
        /// <typeparam name="T">The type of elements in the array.</typeparam>
        /// <param name="array">The array to check.</param>
        /// <param name="values">The values to check for.</param>
        /// <returns>
        /// <c>true</c> if the array contains all the values; otherwise, <c>false</c>.
        /// </returns>
        /*******************************************************************************/
        public static bool ContainsAll<T>(this IEnumerable<T> array, params T[] values)
        {
            HashSet<T> valueSet = new HashSet<T>(values);
            foreach (T item in array)
            {
                if (!valueSet.Contains(item))
                {
                    return false;
                }
            }
            return true;
        }


        /*******************************************************************************/
        /// <summary>
        /// Shuffles the elements in the enumerable randomly.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to shuffle.</param>
        /// <returns>A new enumerable with elements shuffled randomly.</returns>
        /*******************************************************************************/
        public static IEnumerable<T> Shuffle<T>(this IEnumerable<T> enumerable)
        {
            // Convert the input enumerable to a list for in-place shuffling
            List<T> shuffledList = enumerable.ToList();

            // Perform Fisher-Yates shuffle algorithm
            Random random = new Random();
            int n = shuffledList.Count;
            while (n > 1)
            {
                int k = random.Next(n--);
                T value = shuffledList[k];
                shuffledList[k] = shuffledList[n];
                shuffledList[n] = value;
            }

            // Return the shuffled enumerable
            return shuffledList;
        }


        /*******************************************************************************/
        /// <summary>
        /// Swaps the elements at the specified indices in the enumerable.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to perform the swap on.</param>
        /// <param name="index1">The index of the first element to swap.</param>
        /// <param name="index2">The index of the second element to swap.</param>
        /// <returns>
        /// A new enumerable with the elements swapped at the specified indices.
        /// </returns>
        /*******************************************************************************/
        public static IEnumerable<T> Swap<T>(this IEnumerable<T> enumerable, int index1, int index2)
        {
            List<T> swappedList = enumerable.ToList();

            // Perform the swap
            T temp = swappedList[index1];
            swappedList[index1] = swappedList[index2];
            swappedList[index2] = temp;

            // Return the enumerable with swapped elements
            return swappedList;
        }


        /*******************************************************************************/
        /// <summary>
        /// Replaces all occurrences of the old value with the new value in the enumerable.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to perform the replacement on.</param>
        /// <param name="oldValue">The value to be replaced.</param>
        /// <param name="newValue">The new value to replace the old value with.</param>
        /// <returns>
        /// A new enumerable with all occurrences of the old value replaced with the new value.
        /// </returns>
        /// /*******************************************************************************/
        public static IEnumerable<T> Replace<T>(this IEnumerable<T> enumerable, T oldValue, T newValue)
        {
            List<T> replacedList = new List<T>();

            foreach (T item in enumerable)
            {
                // Replace the old value with the new value
                T newItem = EqualityComparer<T>.Default.Equals(item, oldValue) ? newValue : item;
                replacedList.Add(newItem);
            }

            return replacedList;
        }


        /*******************************************************************************/
        /// <summary>
        /// Determines whether all elements in the enumerable are equal.
        /// </summary>
        /// <typeparam name="T">The type of elements in the enumerable.</typeparam>
        /// <param name="enumerable">The enumerable to check.</param>
        /// <returns>True if all elements are equal; otherwise, false.</returns>
        /*******************************************************************************/
        public static bool AllEqual<T>(this IEnumerable<T> enumerable)
        {
            using (var enumerator = enumerable.GetEnumerator())
            {
                if (!enumerator.MoveNext())
                    return true;

                T firstElement = enumerator.Current;
                while (enumerator.MoveNext())
                {
                    if (!EqualityComparer<T>.Default.Equals(enumerator.Current, firstElement))
                        return false;
                }
            }
            return true;
        }


        public static string ToEncodedString(this IEnumerable<byte> data, Encoding encoding)
        {
            return (data.Count() == 0) ? string.Empty : encoding.GetString(data.ToArray(), 0, data.Count());
        }

        public static string ToASCIIString(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.ASCII);
        public static string ToUTF7String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF7);
        public static string ToUTF8String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF8);
        public static string ToUnicodeString(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.Unicode);
        public static string ToUTF32String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF32);
    }
}
