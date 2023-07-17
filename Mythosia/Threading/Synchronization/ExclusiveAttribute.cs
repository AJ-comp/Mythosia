using System;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Threading.Synchronization
{
    /*
    [Serializable]
    internal class ExclusiveExecution : OverrideMethodAspect
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        public override async Task<dynamic?> OverrideAsyncMethod()
        {
            var t = _semaphore.WaitAsync();
            t.GetAwaiter().GetResult();

            try
            {
                Console.WriteLine("adfsf");
                return await meta.ProceedAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public override dynamic OverrideMethod()
        {
            throw new NotImplementedException();
        }
    }
    */


    [AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface)]
    internal class ExclusiveAttribute : Attribute
    {
        public SemaphoreSlim Semapore { get; } = new SemaphoreSlim(1, 1);

        public async Task ExclusiveAsync(Func<Task> func)
        {
            await Semapore.WaitAsync();

            try
            {
                await func.Invoke();
            }
            finally
            {
                Semapore.Release();
            }
        }
    }

}
