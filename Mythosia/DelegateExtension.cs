using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class DelegateExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Executes the provided function repeatedly until a timeout occurs or the function returns true.
        /// </summary>
        /// <param name="action">The function to be executed</param>
        /// <param name="timeout_ms">The timeout duration in milliseconds</param>
        /// <param name="retryInterval_ms">The interval between each retry in milliseconds (optional, defaults to 0)</param>
        /// <returns>True if the function returns true within the timeout, false otherwise</returns>
        /*******************************************************************************/
        public static bool RetryUntilTimeout(this Func<bool> action, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result = false;
            while (true)
            {
                try
                {
                    result = action.Invoke();
                    if (result) break;
                }
                catch
                {
                    elapsedTime = DateTime.Now - startTime;
                    if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                    if (retryInterval_ms > 0) Thread.Sleep(retryInterval_ms);
                }
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the provided asynchronous function repeatedly until a timeout occurs or the function returns true.
        /// </summary>
        /// <param name="action">The asynchronous function to be executed</param>
        /// <param name="timeout_ms">The timeout duration in milliseconds</param>
        /// <param name="retryInterval_ms">The interval between each retry in milliseconds (optional, defaults to 0)</param>
        /// <returns>True if the function returns true within the timeout, false otherwise</returns>
        /*******************************************************************************/
        public static async Task<bool> RetryUntilTimeoutAsync(this Func<Task<bool>> action, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result = false;
            while (true)
            {
                try
                {
                    result = await action.Invoke();
                    if (result) break;
                }
                catch
                {
                    elapsedTime = DateTime.Now - startTime;
                    if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                    if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
                }
            }

            return result;
        }
    }
}
