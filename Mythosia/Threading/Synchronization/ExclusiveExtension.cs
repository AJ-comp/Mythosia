using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia.Threading.Synchronization
{
    public static class ExclusiveExtension
    {
        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);

        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively, ensuring that only one execution can occur at a time.
        /// </summary>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /*******************************************************************************/
        public static async Task ExclusiveAsync(this Func<Task> func)
        {
            await _semaphore.WaitAsync();

            try
            {
                await func.Invoke();
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided argument.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T">The type of the argument.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg">The argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /*******************************************************************************/
        public static async Task ExclusiveAsync<T>(this Func<T, Task> func, T arg)
        {
            await _semaphore.WaitAsync();

            try
            {
                await func.Invoke(arg);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /*******************************************************************************/
        public static async Task ExclusiveAsync<T1, T2>(this Func<T1, T2, Task> func, T1 arg1, T2 arg2)
        {
            await _semaphore.WaitAsync();

            try
            {
                await func.Invoke(arg1, arg2);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /*******************************************************************************/
        public static async Task ExclusiveAsync<T1, T2, T3>(this Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3)
        {
            await _semaphore.WaitAsync();

            try
            {
                await func.Invoke(arg1, arg2, arg3);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <typeparam name="T4">The type of the fourth argument.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        /*******************************************************************************/
        public static async Task ExclusiveAsync<T1, T2, T3, T4>(this Func<T1, T2, T3, T4, Task> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            await _semaphore.WaitAsync();

            try
            {
                await func.Invoke(arg1, arg2, arg3, arg4);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="R">The type of the result.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <returns>A task that represents the asynchronous operation and contains the result of the function.</returns>
        /*******************************************************************************/
        public static async Task<R> ExclusiveAsync<R>(this Func<Task<R>> func)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await func.Invoke();
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided argument, ensuring that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the argument.</typeparam>
        /// <typeparam name="R">The type of the result.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation and contains the result of the function.</returns>
        /*******************************************************************************/
        public static async Task<R> ExclusiveAsync<T1, R>(this Func<T1, Task<R>> func, T1 arg1)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await func.Invoke(arg1);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="R">The type of the result.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation and contains the result of the function.</returns>
        /*******************************************************************************/
        public static async Task<R> ExclusiveAsync<T1, T2, R>(this Func<T1, T2, Task<R>> func, T1 arg1, T2 arg2)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await func.Invoke(arg1, arg2);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <typeparam name="R">The type of the result.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation and contains the result of the function.</returns>
        /*******************************************************************************/
        public static async Task<R> ExclusiveAsync<T1, T2, T3, R>(this Func<T1, T2, T3, Task<R>> func, T1 arg1, T2 arg2, T3 arg3)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await func.Invoke(arg1, arg2, arg3);
            }
            finally
            {
                _semaphore.Release();
            }
        }


        /*******************************************************************************/
        /// <summary>
        /// Executes the specified asynchronous function exclusively with the provided arguments.
        /// Ensures that only one execution can occur at a time.
        /// </summary>
        /// <typeparam name="T1">The type of the first argument.</typeparam>
        /// <typeparam name="T2">The type of the second argument.</typeparam>
        /// <typeparam name="T3">The type of the third argument.</typeparam>
        /// <typeparam name="T4">The type of the fourth argument.</typeparam>
        /// <typeparam name="R">The type of the result.</typeparam>
        /// <param name="func">The asynchronous function to execute exclusively.</param>
        /// <param name="arg1">The first argument to pass to the function.</param>
        /// <param name="arg2">The second argument to pass to the function.</param>
        /// <param name="arg3">The third argument to pass to the function.</param>
        /// <param name="arg4">The fourth argument to pass to the function.</param>
        /// <returns>A task that represents the asynchronous operation and contains the result of the function.</returns>
        /*******************************************************************************/
        public static async Task<R> ExclusiveAsync<T1, T2, T3, T4, R>(this Func<T1, T2, T3, T4, Task<R>> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            await _semaphore.WaitAsync();

            try
            {
                return await func.Invoke(arg1, arg2, arg3, arg4);
            }
            finally
            {
                _semaphore.Release();
            }
        }



        public static async Task ExclusiveAsync<T1>(this SemaphoreSlim semaphore, Func<T1, Task> func, T1 arg1)
        {
            await ExclusiveAsync(semaphore, () => func.Invoke(arg1));
        }

        public static async Task ExclusiveAsync<T1, T2>(this SemaphoreSlim semaphore, Func<T1, T2, Task> func, T1 arg1, T2 arg2)
        {
            await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2));
        }

        public static async Task ExclusiveAsync<T1, T2, T3>(this SemaphoreSlim semaphore, Func<T1, T2, T3, Task> func, T1 arg1, T2 arg2, T3 arg3)
        {
            await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2, arg3));
        }

        public static async Task ExclusiveAsync<T1, T2, T3, T4>(this SemaphoreSlim semaphore, Func<T1, T2, T3, T4, Task> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2, arg3, arg4));
        }

        public static async Task ExclusiveAsync(this SemaphoreSlim semaphore, Func<Task> func)
        {
            await semaphore.WaitAsync();

            try
            {
                await func.Invoke();
            }
            finally
            {
                semaphore.Release();
            }
        }



        public static async Task<R> ExclusiveAsync<T1, R>(this SemaphoreSlim semaphore, Func<T1, Task<R>> func, T1 arg1)
        {
            return await ExclusiveAsync(semaphore, () => func.Invoke(arg1));
        }

        public static async Task<R> ExclusiveAsync<T1, T2, R>(this SemaphoreSlim semaphore, Func<T1, T2, Task<R>> func, T1 arg1, T2 arg2)
        {
            return await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2));
        }

        public static async Task<R> ExclusiveAsync<T1, T2, T3, R>(this SemaphoreSlim semaphore, Func<T1, T2, T3, Task<R>> func, T1 arg1, T2 arg2, T3 arg3)
        {
            return await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2, arg3));
        }

        public static async Task<R> ExclusiveAsync<T1, T2, T3, T4, R>(this SemaphoreSlim semaphore, Func<T1, T2, T3, T4, Task<R>> func, T1 arg1, T2 arg2, T3 arg3, T4 arg4)
        {
            return await ExclusiveAsync(semaphore, () => func.Invoke(arg1, arg2, arg3, arg4));
        }

        public static async Task<R> ExclusiveAsync<R>(SemaphoreSlim semaphore, Func<Task<R>> func)
        {
            await semaphore.WaitAsync();

            try
            {
                return await func.Invoke();
            }
            finally
            {
                semaphore.Release();
            }
        }
    }
}
