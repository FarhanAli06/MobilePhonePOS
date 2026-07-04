using System;
using Empire.Web.Services;

namespace Empire.Web.Extensions
{
    /// <summary>
    /// Extension methods for DateTime timezone conversions
    /// </summary>
    public static class DateTimeExtensions
    {
        /// <summary>
        /// Convert UTC DateTime to user's local timezone
        /// </summary>
        public static DateTime ToUserTime(this DateTime utcDateTime, ITimezoneService timezoneService)
        {
            if (timezoneService == null)
                return utcDateTime;

            return timezoneService.ConvertToUserTime(utcDateTime);
        }

        /// <summary>
        /// Convert DateTime to user's local timezone and format as string
        /// </summary>
        public static string ToUserTimeString(this DateTime utcDateTime, ITimezoneService timezoneService, string format = "MM/dd/yyyy hh:mm:ss tt")
        {
            if (timezoneService == null)
                return utcDateTime.ToString(format);

            var localTime = timezoneService.ConvertToUserTime(utcDateTime);
            return localTime.ToString(format);
        }

        /// <summary>
        /// Convert nullable DateTime to user's local timezone
        /// </summary>
        public static DateTime? ToUserTime(this DateTime? utcDateTime, ITimezoneService timezoneService)
        {
            if (utcDateTime == null || timezoneService == null)
                return utcDateTime;

            return timezoneService.ConvertToUserTime(utcDateTime.Value);
        }

        /// <summary>
        /// Convert nullable DateTime to user's local timezone and format as string
        /// </summary>
        public static string ToUserTimeString(this DateTime? utcDateTime, ITimezoneService timezoneService, string format = "MM/dd/yyyy hh:mm:ss tt")
        {
            if (utcDateTime == null)
                return string.Empty;

            if (timezoneService == null)
                return utcDateTime.Value.ToString(format);

            var localTime = timezoneService.ConvertToUserTime(utcDateTime.Value);
            return localTime.ToString(format);
        }
    }
}
