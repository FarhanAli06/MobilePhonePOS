using System;
using Microsoft.AspNetCore.Http;

namespace Empire.Web.Services
{
    /// <summary>
    /// Service for handling timezone conversions based on user's browser timezone
    /// </summary>
    public class TimezoneService : ITimezoneService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private const string TimezoneHeaderKey = "X-Timezone";
        private const string TimezoneOffsetHeaderKey = "X-Timezone-Offset";
        private const string TimezoneCookieKey = "UserTimezone";
        private const string TimezoneOffsetCookieKey = "UserTimezoneOffset";

        public TimezoneService(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Get the user's timezone from HTTP headers or default to UTC
        /// The client sends timezone info via JavaScript in headers
        /// </summary>
        public TimeZoneInfo GetUserTimeZone()
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null)
                    return TimeZoneInfo.Utc;

                // 1. Try to get timezone from header (AJAX requests)
                if (httpContext.Request.Headers.TryGetValue(TimezoneHeaderKey, out var timezoneId))
                {
                    try
                    {
                        return TimeZoneInfo.FindSystemTimeZoneById(timezoneId.ToString());
                    }
                    catch { /* Ignore invalid timezone ID */ }
                }

                // 2. Try to get timezone from cookie (Page loads)
                if (httpContext.Request.Cookies.TryGetValue(TimezoneCookieKey, out var cookieTimezoneId))
                {
                    try
                    {
                        return TimeZoneInfo.FindSystemTimeZoneById(cookieTimezoneId);
                    }
                    catch { /* Ignore invalid timezone ID */ }
                }

                // 3. Try to get timezone offset from header (AJAX fallback)
                if (httpContext.Request.Headers.TryGetValue(TimezoneOffsetHeaderKey, out var offsetString))
                {
                    if (int.TryParse(offsetString, out int offsetMinutes))
                    {
                        return CreateCustomTimeZoneFromOffset(offsetMinutes);
                    }
                }

                // 4. Try to get timezone offset from cookie (Page load fallback)
                if (httpContext.Request.Cookies.TryGetValue(TimezoneOffsetCookieKey, out var cookieOffsetString))
                {
                    if (int.TryParse(cookieOffsetString, out int offsetMinutes))
                    {
                        return CreateCustomTimeZoneFromOffset(offsetMinutes);
                    }
                }

                // Default to UTC if no timezone information is available
                return TimeZoneInfo.Utc;
            }
            catch
            {
                return TimeZoneInfo.Utc;
            }
        }

        private TimeZoneInfo CreateCustomTimeZoneFromOffset(int offsetMinutes)
        {
            // JavaScript getTimezoneOffset() returns negative values for positive offsets
            // e.g., PST (UTC-8) returns +480 minutes
            // We need to negate it to get the correct offset
            var offset = TimeSpan.FromMinutes(-offsetMinutes);
            return TimeZoneInfo.CreateCustomTimeZone(
                $"Custom{offsetMinutes}",
                offset,
                $"(UTC{(offset.Hours >= 0 ? "+" : "")}{offset.Hours:D2}:{offset.Minutes:D2})",
                $"Custom Timezone"
            );
        }

        /// <summary>
        /// Convert UTC DateTime to user's local timezone
        /// </summary>
        public DateTime ConvertToUserTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // If not UTC, assume it is and convert
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            var userTimeZone = GetUserTimeZone();
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, userTimeZone);
        }

        /// <summary>
        /// Convert user's local DateTime to UTC
        /// </summary>
        public DateTime ConvertToUtc(DateTime localDateTime)
        {
            var userTimeZone = GetUserTimeZone();
            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, userTimeZone);
        }

        /// <summary>
        /// Get current time in user's timezone
        /// </summary>
        public DateTime GetUserNow()
        {
            return ConvertToUserTime(DateTime.UtcNow);
        }
    }
}
