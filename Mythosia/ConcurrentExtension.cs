using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class ConcurrentExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Adds a range of items to the concurrent bag.
        /// </summary>
        /// <typeparam name="T">The type of items in the bag.</typeparam>
        /// <param name="bag">The concurrent bag.</param>
        /// <param name="toAddList">The collection of items to add.</param>
        /*******************************************************************************/
        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAddList)
        {
            foreach (var item in toAddList) bag.Add(item);
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds a range of items to the concurrent bag in parallel using the specified parallel options.
        /// </summary>
        /// <typeparam name="T">The type of items in the bag.</typeparam>
        /// <param name="bag">The concurrent bag.</param>
        /// <param name="toAddList">The collection of items to add.</param>
        /// <param name="options">The parallel options for controlling the parallel execution.</param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeParallel<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAddList, ParallelOptions options)
        {
            Parallel.ForEach(toAddList, options, item =>
            {
                bag.Add(item);
            });
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds a range of items to the concurrent bag, optionally performing the addition in parallel with the specified task count.
        /// </summary>
        /// <typeparam name="T">The type of items in the bag.</typeparam>
        /// <param name="bag">The concurrent bag.</param>
        /// <param name="toAddList">The collection of items to add.</param>
        /// <param name="taskCount">
        /// The number of parallel tasks to use for adding items. If less than equal 0, the system decides the task count.
        /// </param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeParallel<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAddList, int taskCount = 0)
        {
            if (taskCount > 0)
            {
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = taskCount
                };

                bag.AddRangeParallel(toAddList, options);
            }
            else
            {
                Parallel.ForEach(toAddList, item =>
                {
                    bag.Add(item);
                });
            }
        }

        /*******************************************************************************/
        /// <summary>
        /// Adds a non-null item to the concurrent bag.
        /// </summary>
        /// <typeparam name="T">The type of item.</typeparam>
        /// <param name="obj">The concurrent bag.</param>
        /// <param name="item">The item to add.</param>
        /*******************************************************************************/
        public static void AddExceptNull<T>(this ConcurrentBag<T> obj, T iem)
        {
            if (iem == null) return;

            obj.Add(iem);
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds a range of non-null items to the concurrent bag.
        /// </summary>
        /// <typeparam name="T">The type of items.</typeparam>
        /// <param name="obj">The concurrent bag.</param>
        /// <param name="toAddList">The collection of items to add.</param>
        /*******************************************************************************/
        public static void AddRangeExceptNull<T>(this ConcurrentBag<T> obj, IEnumerable<T> toAddList)
        {
            foreach (var item in toAddList) obj.AddExceptNull(item);
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds the non-null elements from the specified collection to the target ConcurrentBag in parallel, while excluding null elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target ConcurrentBag.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="options">The parallel options to control the degree of parallelism and other settings.</param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeExceptNullParallel<T>(this ConcurrentBag<T> collection, IEnumerable<T> toAddList, ParallelOptions options)
        {
            Parallel.ForEach(toAddList, options, item =>
            {
                if (item == null) return;

                collection.Add(item);
            });
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds the non-null elements from the specified collection to the target ConcurrentBag in parallel, while excluding null elements.
        /// </summary>
        /// <typeparam name="T">The type of elements in the collections.</typeparam>
        /// <param name="collection">The target ConcurrentBag.</param>
        /// <param name="toAddList">The collection from which to add the elements.</param>
        /// <param name="taskCount">
        /// The number of tasks to be used for parallel execution. Use 0 to let the system determine the degree of parallelism.
        /// </param>
        /// <remarks>
        /// <b><i>Note: The order of element addition is not guaranteed due to parallel execution.</i></b>
        /// </remarks>
        /*******************************************************************************/
        public static void AddRangeExceptNullParallel<T>(this ConcurrentBag<T> collection, IEnumerable<T> toAddList, int taskCount = 0)
        {
            if (taskCount > 0)
            {
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = taskCount
                };

                collection.AddRangeExceptNullParallel(toAddList, options);
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
    }
}
