using System;

namespace Empire.Web.Services
{
    /// <summary>
    /// Service for handling timezone conversions
    /// </summary>
    public interface ITimezoneService
    {
        /// <summary>
        /// Get the user's timezone (from HTTP context)
        /// </summary>
        TimeZoneInfo GetUserTimeZone();

        /// <summary>
        /// Convert UTC DateTime to user's local timezone
        /// </summary>
        DateTime ConvertToUserTime(DateTime utcDateTime);

        /// <summary>
        /// Convert user's local DateTime to UTC
        /// </summary>
        DateTime ConvertToUtc(DateTime localDateTime);

        /// <summary>
        /// Get current time in user's timezone
        /// </summary>
        DateTime GetUserNow();
    }
}
