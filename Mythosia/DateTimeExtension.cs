using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Mythosia
{
    public static class DateTimeExtension
    {
        /*******************************************************************************/
        /// <summary>
        /// Checks if the given date and time value is within the specified range.
        /// </summary>
        /// <param name="value">The date and time value to check.</param>
        /// <param name="minValue">The minimum value of the range.</param>
        /// <param name="maxValue">The maximum value of the range.</param>
        /// <returns><c>true</c> if the value is within the range; otherwise, <c>false</c>.</returns>
        /*******************************************************************************/
        public static bool IsInRange(this DateTime value, DateTime minValue, DateTime maxValue)
        {
            return (minValue <= value && value <= maxValue);
        }

        /*******************************************************************************/
        /// <summary>
        /// Determines whether the given date is today.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns><c>true</c> if the given date is today; otherwise, <c>false</c>.</returns>
        /*******************************************************************************/
        public static bool IsToday(this DateTime date)
        {
            return date.Date == DateTime.Today;
        }

        /*******************************************************************************/
        /// <summary>
        /// Determines whether the given date is a past date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns><c>true</c> if the given date is a past date; otherwise, <c>false</c>.</returns>
        /*******************************************************************************/
        public static bool IsPastDate(this DateTime date)
        {
            return date.Date < DateTime.Today;
        }

        /*******************************************************************************/
        /// <summary>
        /// Determines whether the given date is a future date.
        /// </summary>
        /// <param name="date">The date to check.</param>
        /// <returns><c>true</c> if the given date is a future date; otherwise, <c>false</c>.</returns>
        /*******************************************************************************/
        public static bool IsFutureDate(this DateTime date)
        {
            return date.Date > DateTime.Today;
        }

        /*******************************************************************************/
        /// <summary>
        /// Returns the time remaining from the given date and time relative to the current time.
        /// </summary>
        /// <param name="dateTime">The date and time to calculate the remaining time from.</param>
        /// <returns>The time remaining from the given date and time.</returns>
        /*******************************************************************************/
        public static TimeSpan GetTimeRemaining(this DateTime dateTime)
        {
            DateTime now = DateTime.Now;
            return dateTime > now ? dateTime - now : TimeSpan.Zero;
        }

        /*******************************************************************************/
        /// <summary>
        /// Converts the given date and time to a Unix timestamp.
        /// </summary>
        /// <param name="dateTime">The date and time to convert.</param>
        /// <returns>The Unix timestamp representing the given date and time.</returns>
        /*******************************************************************************/
        public static long ToUnixTimestamp(this DateTime dateTime)
        {
            return (long)(dateTime.ToUniversalTime() - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;
        }
    }
}
