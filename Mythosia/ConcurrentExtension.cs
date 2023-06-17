using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace Mythosia
{
    public static class ConcurrentExtension
    {
        public static void AddRange<T>(this ConcurrentBag<T> value, IEnumerable<T> list)
        {
            foreach (var item in list) value.Add(item);
        }

    }
}
