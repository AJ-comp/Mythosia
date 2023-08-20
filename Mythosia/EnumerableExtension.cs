using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class EnumerableExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Joins the elements of an enumerable collection into a single string using the specified connector.
        /// </summary>
        /// <typeparam name="T">The type of the elements in the collection.</typeparam>
        /// <param name="obj">The enumerable collection to join.</param>
        /// <param name="connector">
        /// The string used to connect the elements in the resulting string. Default value is a comma (,).
        /// </param>
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
        /// Adds the elements from the specified collection to the target collection in parallel, with the provided parallel options.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target collection.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="options">The parallel options to control the degree of parallelism and other settings.</param>
        /// <remarks>
        /// This method performs parallel addition of elements from the <paramref name="toAddList"/> collection to the target <paramref name="collection"/>.
        /// The parallel execution is controlled by the specified <paramref name="options"/>. <br/>
        /// The parallel options provide control over the degree of parallelism and other settings such as cancellation and exception handling. <br/>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeParallel<T>(this ICollection<T> collection, IEnumerable<T> toAddList, ParallelOptions options)
        {
            Parallel.ForEach(toAddList, options, item =>
            {
                collection.Add(item);
            });
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds the elements from the specified collection to the target collection in parallel, with the option to control the degree of parallelism.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target collection.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="taskCount">
        /// The number of tasks to be used for parallel execution. Use 0 to let the system determine the degree of parallelism.
        /// </param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeParallel<T>(this ICollection<T> collection, IEnumerable<T> toAddList, int taskCount = 0)
        {
            if (taskCount > 0)
            {
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = taskCount
                };

                collection.AddRangeParallel(toAddList, options);
            }
            else
            {
                Parallel.ForEach(toAddList, item =>
                {
                    collection.Add(item);
                });
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds a specified element to the collection, excluding null values.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection.</typeparam>
        /// <param name="collection">The collection to which the element should be added.</param>
        /// <param name="item">The element to add to the collection.</param>
        /// <remarks>
        /// This extension method checks if the provided element is null. If the element is not null,
        /// it is added to the collection using the <see cref="ICollection{T}.Add"/> method.
        /// If the element is null, no action is taken, and the method returns without modifying the collection.
        /// </remarks>
        /*******************************************************************************/
        public static void AddExceptNull<T>(this ICollection<T> collection, T item)
        {
            if (item == null) return;

            collection.Add(item);
        }


        /*******************************************************************************/
        /// <summary>
        /// An extension method that adds non-null elements from the specified collection.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collection</typeparam>
        /// <param name="collection">The collection to which elements will be added</param>
        /// <param name="toAddList">The collection containing elements to be added</param>
        /*******************************************************************************/
        public static void AddRangeExceptNull<T>(this ICollection<T> collection, IEnumerable<T> toAddList)
        {
            foreach(var item in toAddList) collection.AddExceptNull(item);
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds the non-null elements from the specified collection to the target collection in parallel, while excluding null elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target collection.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="options">The parallel options to control the degree of parallelism and other settings.</param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeExceptNullParallel<T>(this ICollection<T> collection, IEnumerable<T> toAddList, ParallelOptions options)
        {
            Parallel.ForEach(toAddList, options, item =>
            {
                if (item == null) return;

                collection.Add(item);
            });
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds the non-null elements from the specified collection to the target collection in parallel, while excluding null elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target collection.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="taskCount">
        /// The number of tasks to be used for parallel execution. Use 0 to let the system determine the degree of parallelism.
        /// </param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b> 
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeExceptNullParallel<T>(this ICollection<T> collection, IEnumerable<T> toAddList, int taskCount = 0)
        {
            if (taskCount > 0)
            {
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = taskCount
                };

                collection.AddRangeParallel(toAddList, options);
            }
            else
            {
                Parallel.ForEach(toAddList, item =>
                {
                    if (item == null) return;

                    collection.Add(item);
                });
            }
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
        [Obsolete ("This function will be removed on 1.3.0 ver. use the ToHexStringL or ToHexStringU.")]
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
        [Obsolete("This function will be removed on 1.3.0 ver. use the ToHexStringL or ToHexStringU.")]
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
        [Obsolete("This function will be removed on 1.3.0 ver. use the ToHexStringL or ToHexStringU.")]
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
        [Obsolete("This function will be removed on 1.3.0 ver. use the ToHexStringL or ToHexStringU.")]
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
        /// <param name="source">The enumerable to check.</param>
        /// <returns>True if all elements are equal; otherwise, false.</returns>
        /*******************************************************************************/
        public static bool AllEqual<T>(this IEnumerable<T> source)
        {
            using var enumerator = source.GetEnumerator();
            if (!enumerator.MoveNext()) return true;

            T firstElement = enumerator.Current;
            while (enumerator.MoveNext())
            {
                if (!EqualityComparer<T>.Default.Equals(enumerator.Current, firstElement))
                    return false;
            }
            return true;
        }


        public static string ToEncodedString(this IEnumerable<byte> data, Encoding encoding)
        {
            return (data.Count() == 0) ? string.Empty : encoding.GetString(data.ToArray(), 0, data.Count());
        }

        public static string ToASCIIString(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.ASCII);
        public static string ToBase64String(this IEnumerable<byte> data) => Convert.ToBase64String(data.ToArray());
        public static string ToUTF7String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF7);
        public static string ToUTF8String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF8);
        public static string ToUnicodeString(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.Unicode);
        public static string ToUTF32String(this IEnumerable<byte> data) => data.ToEncodedString(Encoding.UTF32);


        private static T[] ConvertToNumericArray<T>(byte[] data) where T : struct
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (data.Length == 0) return new T[0];

            int sizeOfT = Marshal.SizeOf<T>();
            int remainder = data.Length % sizeOfT;
            int requiredLength = data.Length + (remainder == 0 ? 0 : sizeOfT - remainder);
            byte[] buffer = new byte[requiredLength];

            Array.Copy(data, buffer, data.Length);

            T[] result = new T[buffer.Length / sizeOfT];
            Buffer.BlockCopy(buffer, 0, result, 0, buffer.Length);

            return result;
        }


//        public static short[] ToShortArray(this IEnumerable<byte> data) => ConvertToNumericArray<short>(data.ToArray());
        public static ushort[] ToUShortArray(this IEnumerable<byte> data) => ConvertToNumericArray<ushort>(data.ToArray());
//        public static int[] ToIntArray(this IEnumerable<byte> data) => ConvertToNumericArray<int>(data.ToArray());
        public static uint[] ToUIntArray(this IEnumerable<byte> data) => ConvertToNumericArray<uint>(data.ToArray());
//        public static long[] ToLongArray(this IEnumerable<byte> data) => ConvertToNumericArray<long>(data.ToArray());
        public static ulong[] ToULongArray(this IEnumerable<byte> data) => ConvertToNumericArray<ulong>(data.ToArray());


        public static IEnumerable<T> Copy<T>(this IEnumerable<T> data)
        {
            List<T> result = new List<T>();
            result.AddRange(data);

            return result;
        }


        public static T[] Copy<T>(this T[] data)
        {
            T[] copy = new T[data.Length];
            Array.Copy(data, copy, data.Length);
            return copy;
        }
    }
}
