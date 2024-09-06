using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace Mythosia.Diagnostics
{
    public static class ExecutionTimeExtension
    {
        #region Sync method
        // Measure execution time for sync functions with return value
        public static TResult MeasureExecutionTime<TResult>(Func<TResult> func, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            TResult result = func();
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
            return result;
        }

        // Measure execution time for sync actions without return value
        public static void MeasureExecutionTime(Action action, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            action();
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
        }

        // Measure execution time for sync functions with return value
        public static TResult MeasureExecutionTime<T, TResult>(Func<T, TResult> func, T param, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            TResult result = func(param);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
            return result;
        }

        // Measure execution time for sync actions without return value
        public static void MeasureExecutionTime<T>(Action<T> action, T param, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            action(param);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
        }
        #endregion



        #region Async method
        // Measure execution time for tasks with return value and parameters
        public static async Task<TResult> MeasureExecutionTimeAsync<TResult>(this Task<TResult> task, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            TResult result = await task;
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
            return result;
        }

        // Measure execution time for tasks without return value and parameters
        public static async Task MeasureExecutionTimeAsync(this Task task, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            await task;
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
        }


        public static async Task<TResult> MeasureExecutionTimeAsync<T, TResult>(Func<T, Task<TResult>> func, T param, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            TResult result = await func(param);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
            return result;
        }


        // Measure execution time for actions with single parameter without return value
        public static async Task MeasureExecutionTimeAsync<T>(Func<T, Task> func, T param, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            await func(param);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
        }


        // Measure execution time for functions with multiple parameters
        public static async Task<TResult> MeasureExecutionTimeAsync<T1, T2, TResult>(Func<T1, T2, Task<TResult>> func, T1 param1, T2 param2, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            TResult result = await func(param1, param2);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
            return result;
        }

        // Measure execution time for actions with multiple parameters
        public static async Task MeasureExecutionTimeAsync<T1, T2>(Func<T1, T2, Task> func, T1 param1, T2 param2, Action<TimeSpan> reportAction)
        {
            var stopwatch = Stopwatch.StartNew();
            await func(param1, param2);
            stopwatch.Stop();
            reportAction(stopwatch.Elapsed);
        }
        #endregion
    }
}
