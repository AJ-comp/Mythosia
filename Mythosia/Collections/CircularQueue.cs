using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Mythosia.Collections
{
    public class CircularQueue<T> : IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, ICollection
    {
        private Queue<T> queue;
        private int maxSize;
        private readonly object syncRoot;

        public CircularQueue(int maxSize, bool sync = false)
        {
//            queue = new ConcurrentQueue<T>();
            queue = new Queue<T>(maxSize);
            this.maxSize = maxSize;

            syncRoot = sync ? new object() : null;
        }


        /*******************************************************************************/
        /// <summary>
        /// Adds an item to the end of the circular queue. If the queue is full, the oldest item is removed.
        /// </summary>
        /// <param name="item">The item to enqueue.</param>
        /*******************************************************************************/
        public void Enqueue(T item)
        {
            SyncByCondition(syncRoot, () =>
            {
                while(queue.Count >= maxSize)
                    queue.Dequeue(); // Remove the oldest item if the queue is full

                queue.Enqueue(item);
            });
        }


        public void EnqueueRange(IEnumerable<T> list)
        {
            foreach (var item in list) Enqueue(item);
        }


        /*******************************************************************************/
        /// <summary>
        /// Removes and returns the item at the beginning of the circular queue.
        /// </summary>
        /// <returns>The item that was dequeued.</returns>
        /*******************************************************************************/
        public T Dequeue() => SyncByCondition(syncRoot, () => queue.Dequeue());


        public IEnumerable<T> DequeueRange(int count)
        {
            return SyncByCondition(syncRoot, () =>
            {
                var result = new List<T>();

                foreach (var item in this)
                {
                    if (count-- <= 0) break;

                    result.Add(item);
                }

                return result;
            });
        }


        /*******************************************************************************/
        /// <summary>
        /// Returns the item at the beginning of the circular queue without removing it.
        /// </summary>
        /// <returns>The item at the beginning of the circular queue.</returns>
        /*******************************************************************************/
        public T Peek() => SyncByCondition(syncRoot, () => queue.Peek());


        /*******************************************************************************/
        /// <summary>
        /// Sets the maximum size of the circular queue. If the new size is smaller than the current size, 
        /// the oldest items are removed from the queue to fit the new size.
        /// </summary>
        /// <param name="maxSize">The new maximum size of the circular queue.</param>
        /*******************************************************************************/
        public void SetMaxSize(int maxSize)
        {
            SyncByCondition(syncRoot, () =>
            {
                if (maxSize < queue.Count)
                {
                    int itemsToRemove = queue.Count - maxSize;
                    for (int i = 0; i < itemsToRemove; i++)
                    {
                        queue.Dequeue();
                    }
                }

                this.maxSize = maxSize;
            });
        }


        /// <summary>
        /// Gets the number of items currently in the circular queue.
        /// </summary>
        public int Count => SyncByCondition(syncRoot, () => queue.Count);


        /// <summary>
        /// Gets a value indicating whether access to the circular queue is synchronized (thread-safe).
        /// </summary>
        public bool IsSynchronized => syncRoot != null;

        public object SyncRoot => syncRoot;

        public void CopyTo(Array array, int index)
        {
            SyncByCondition(syncRoot, () => ((ICollection)queue).CopyTo(array, index));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return SyncByCondition(syncRoot, () => queue.GetEnumerator());
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }




        private void SyncByCondition(object lockObject, Action action)
        {
            if (lockObject == null) action();
            else
            {
                lock (syncRoot) { action(); }
            }
        }


        private T SyncByCondition<T>(object lockObject, Func<T> func)
        {
            if (lockObject == null) return func();
            else
            {
                lock (syncRoot) { return func(); }
            }
        }
    }
}
