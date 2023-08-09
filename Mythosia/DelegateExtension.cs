using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class DelegateExtension
    {
        [Obsolete("The Retry method is obsolete and will be removed in the future versions. Consider using RetryIfFailed method instead.")]
        public static bool Retry(this Func<bool> action, uint timeout_ms, int retryInterval_ms = 0) => action.RetryIfFailed(timeout_ms, retryInterval_ms);

        [Obsolete("The Retry method is obsolete and will be removed in the future versions. Consider using RetryIfFailed method instead.")]
        public static bool Retry<T>(this Func<T, bool> action, T arg, uint timeout_ms, int retryInterval_ms = 0) => action.RetryIfFailed(arg, timeout_ms, retryInterval_ms);

        [Obsolete ("The Retry method is obsolete and will be removed in the future versions. Consider using RetryIfFailed method instead.")]
        public static bool Retry<T1, T2>(this Func<T1, T2, bool> action, T1 arg1, T2 arg2, uint timeout_ms, int retryInterval_ms = 0)
            => action.RetryIfFailed(arg1, arg2, timeout_ms, retryInterval_ms);

        [Obsolete("The Retry method is obsolete and will be removed in the future versions. Consider using RetryIfFailed method instead.")]
        public static bool Retry<T1, T2, T3>(this Func<T1, T2, T3, bool> action, T1 arg1, T2 arg2, T3 arg3, uint timeout_ms, int retryInterval_ms = 0)
            => action.RetryIfFailed(arg1, arg2, arg3, timeout_ms, retryInterval_ms);

        [Obsolete("The RetryAsync method is obsolete and will be removed in the future versions. Consider using RetryIfFailedAsync method instead.")]
        public static async Task<bool> RetryAsync(this Func<Task<bool>> action, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke();
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }


        [Obsolete("The RetryAsync method is obsolete and will be removed in the future versions. Consider using RetryIfFailedAsync method instead.")]
        public static async Task<bool> RetryAsync<T1>(this Func<T1, Task<bool>> action, T1 arg, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }


        [Obsolete("The RetryAsync method is obsolete and will be removed in the future versions. Consider using RetryIfFailedAsync method instead.")]
        public static async Task<bool> RetryAsync<T1, T2>(this Func<T1, T2, Task<bool>> action, T1 arg1, T2 arg2, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg1, arg2);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }

        [Obsolete("The RetryAsync method is obsolete and will be removed in the future versions. Consider using RetryIfFailedAsync method instead.")]
        public static async Task<bool> RetryAsync<T1, T2, T3>(this Func<T1, T2, T3, Task<bool>> action, T1 arg1, T2 arg2, T3 arg3, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg1, arg2, arg3);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }







        /*******************************************************************************/
        /// <summary>
        /// Executes the specified action and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>A boolean value indicating whether the action returns true within the timeout period.</returns>
        /*******************************************************************************/
        public static bool RetryIfFailed(this Func<bool> action, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = action.Invoke();
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) Thread.Sleep(retryInterval_ms);
            }

            return result;
        }



        /*******************************************************************************/
        /// <summary>
        /// Executes the specified action with the given argument and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="arg">The argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>True if the action returns true within the timeout period, false otherwise.</returns>
        /*******************************************************************************/
        public static bool RetryIfFailed<T>(this Func<T, bool> action, T arg, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = action.Invoke(arg);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) Thread.Sleep(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified action with the given arguments and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>True if the action returns true within the timeout period, false otherwise.</returns>
        /*******************************************************************************/
        [Obsolete("The Retry method is obsolete and will be removed in the future versions. Consider using RetryIfFailed method instead.")]
        public static bool RetryIfFailed<T1, T2>(this Func<T1, T2, bool> action, T1 arg1, T2 arg2, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = action.Invoke(arg1, arg2);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) Thread.Sleep(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified action with the given arguments and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <param name="action">The action to execute.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>True if the action returns true within the timeout period, false otherwise.</returns>
        /*******************************************************************************/
        public static bool RetryIfFailed<T1, T2, T3>(this Func<T1, T2, T3, bool> action, T1 arg1, T2 arg2, T3 arg3, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = action.Invoke(arg1, arg2, arg3);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) Thread.Sleep(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous action and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task will complete with a boolean value indicating whether the action returns true within the timeout period.
        /// </returns>
        /*******************************************************************************/
        public static async Task<bool> RetryIfFailedAsync(this Func<Task<bool>> action, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke();
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous action with the given argument and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T1">The type of the argument.</typeparam>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <param name="arg">The argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task will complete with a boolean value indicating whether the action returns true within the timeout period.
        /// </returns>
        /*******************************************************************************/
        public static async Task<bool> RetryIfFailedAsync<T1>(this Func<T1, Task<bool>> action, T1 arg, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous action with the given arguments and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task will complete with a boolean value indicating whether the action returns true within the timeout period.
        /// </returns>
        /*******************************************************************************/
        public static async Task<bool> RetryIfFailedAsync<T1, T2>(this Func<T1, T2, Task<bool>> action, T1 arg1, T2 arg2, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg1, arg2);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous action with the given arguments and retries the execution until it returns true or the timeout is reached.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <param name="arg1">The first argument to pass to the action.</param>
        /// <param name="arg2">The second argument to pass to the action.</param>
        /// <param name="arg3">The third argument to pass to the action.</param>
        /// <param name="timeout_ms">The timeout value in milliseconds.</param>
        /// <param name="retryInterval_ms">The interval between retries in milliseconds. If set to 0, no interval is applied.</param>
        /// <returns>
        /// A task representing the asynchronous operation. The task will complete with a boolean value indicating whether the action returns true within the timeout period.
        /// </returns>
        /*******************************************************************************/
        public static async Task<bool> RetryIfFailedAsync<T1, T2, T3>(this Func<T1, T2, T3, Task<bool>> action, T1 arg1, T2 arg2, T3 arg3, uint timeout_ms, int retryInterval_ms = 0)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan elapsedTime;

            bool result;
            while (true)
            {
                result = await action.Invoke(arg1, arg2, arg3);
                if (result) break;

                elapsedTime = DateTime.Now - startTime;
                if (elapsedTime.TotalMilliseconds >= timeout_ms) break; // Timeout occurred

                if (retryInterval_ms > 0) await Task.Delay(retryInterval_ms);
            }

            return result;
        }
    }
}
