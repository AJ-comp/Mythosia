using Mythosia.Collections;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Test
{
    internal class CollectionTest
    {
        CircularQueue<int> queue = new(5, true);

        public void StartTest()
        {
            // Producer threads
            Task producer1 = Task.Run(() => EnqueueItems(1, 10));
            Task producer2 = Task.Run(() => EnqueueItems(11, 20));

            // Consumer threads
            Task consumer1 = Task.Run(DequeueItems);
            Task consumer2 = Task.Run(DequeueItems);

            Task.WaitAll(producer1, producer2, consumer1, consumer2);

            Console.WriteLine("All tasks completed. Press any key to exit.");
            Console.ReadKey();
        }


        void EnqueueItems(int start, int end)
        {
            for (int i = start; i <= end; i++)
            {
                queue.Enqueue(i);
                Console.WriteLine($"Enqueued: {i}");
                Thread.Sleep(100); // Simulate some work being done
            }
        }

        void DequeueItems()
        {
            List<int> items = new List<int>();

            while (true)
            {
                items.Add(queue.Dequeue());
                Thread.Sleep(200); // Simulate some work being done
            }
        }
    }
}
