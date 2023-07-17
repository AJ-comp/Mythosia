using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Threading.Synchronization
{
    [Exclusive]
    internal interface IExclusive
    {
    }


    public static class ExclusiveExtensions
    {
        internal static async Task ExclusiveAsync(this IExclusive exclusive, Func<Task> func)
        {
            var properties = exclusive.GetType().GetProperties(BindingFlags.Public);

            ExclusiveAttribute attribute = null;
            foreach (var property in properties)
            {
                attribute = property.GetCustomAttribute(typeof(ExclusiveAttribute)) as ExclusiveAttribute;
                if (attribute != null) break;
            }

            await attribute.Semapore.WaitAsync();

            try
            {
                await func.Invoke();
            }
            finally
            {
                attribute.Semapore.Release();
            }
        }
    }
}
