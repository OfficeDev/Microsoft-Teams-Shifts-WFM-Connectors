// ---------------------------------------------------------------------------
// <copyright file="DateTimeExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Extensions
{
    using System;
    using System.Collections.Concurrent;
    using System.Globalization;
    using System.Linq;
    using TimeZoneConverter;
    using WfmTeams.Adapter.Services;

    /// <summary>
    /// Defines DateTime extension methods.
    /// </summary>
    public static class DateTimeExtensions
    {
        private static readonly ConcurrentDictionary<string, DaylightTime> _daylightTimes = new ConcurrentDictionary<string, DaylightTime>();

        private static readonly DateTime _epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public static DateTime AddWeek(this DateTime dt)
        {
            return dt.AddWeeks(1);
        }

        public static DateTime AddWeeks(this DateTime dt, int value)
        {
            return dt.AddDays(7 * value);
        }

        public static DateTime ApplyTimeZoneOffset(this DateTime utc, string timeZone)
        {
            var timeZoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            var offsetTimespan = timeZoneInfo.GetUtcOffset(utc);

            return utc + offsetTimespan;
        }

        public static string AsDateString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        public static string AsDateString(this DateTime? dt)
        {
            if (dt.HasValue)
            {
                return dt.Value.AsDateString();
            }

            return string.Empty;
        }

        public static string AsDateTimeString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static string AsTimeString(this DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }

        public static DateTime ConvertFromLocalTime(this DateTime dt, string timeZone, ISystemTimeService timeService)
        {
            var timezoneInfo = TZConvert.GetTimeZoneInfo(timeZone);
            dt = AdjustForDaylightSaving(dt, timezoneInfo, timeService);
            var localDateTime = DateTime.SpecifyKind(dt, DateTimeKind.Unspecified);

            return TimeZoneInfo.ConvertTimeToUtc(localDateTime, timezoneInfo);
        }

        public static DateTime[] Range(this DateTime dt, int pastWeeks, int futureWeeks, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var weekStart = dt.StartOfWeek(startOfWeek);
            var totalWeeks = pastWeeks + futureWeeks + 1;

            return Enumerable.Range(-pastWeeks, totalWeeks)
                .Select(offset => weekStart.AddWeeks(offset))
                .ToArray();
        }

        public static DateTime[] Range(this DateTime startDate, DateTime endDate, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var weekStart = startDate.StartOfWeek(startOfWeek);
            var weekEnd = endDate.StartOfWeek(startOfWeek).AddDays(6);
            var totalWeeks = ((weekEnd - weekStart).Days + 1) / 7;

            return Enumerable.Range(0, totalWeeks)
                .Select(offset => weekStart.AddWeeks(offset))
                .ToArray();
        }

        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static long ToTimeStamp(this DateTime dt)
        {
            var elapsedTime = dt - _epoch;
            return (long)elapsedTime.TotalSeconds;
        }

        private static DateTime AdjustForDaylightSaving(DateTime dt, TimeZoneInfo timezoneInfo, ISystemTimeService timeService)
        {
            var key = $"{timezoneInfo.Id}_{dt.Year}";
            if (!_daylightTimes.ContainsKey(key))
            {
                _daylightTimes[key] = GetDaylightChanges(timezoneInfo, dt.Year, timeService);
            }

            var daylightTime = _daylightTimes[key];

            if (daylightTime != null && dt >= daylightTime.Start && dt < daylightTime.Start.Add(daylightTime.Delta))
            {
                // the datetime falls within the delta of time that will be skipped over when the
                // transition to daylight savings time occurs e.g. in the UK this is in period
                // 01:00:00 to 01:59:59, so shift the time forward by the delta e.g. in the UK a
                // time of 01:25 say will be shifted to 02:25
                return dt.Add(daylightTime.Delta);
            }

            return dt;
        }

        private static DaylightTime GetDaylightChanges(TimeZoneInfo timezoneInfo, int year, ISystemTimeService timeService)
        {
            var currentRules = timezoneInfo.GetAdjustmentRules().FirstOrDefault(rule => rule.DateStart <= timeService.Today && rule.DateEnd >= timeService.Today);

            if (currentRules != null)
            {
                var daylightStart = GetTransitionDate(currentRules.DaylightTransitionStart, year);

                var daylightEnd = GetTransitionDate(currentRules.DaylightTransitionEnd, year);

                return new DaylightTime(daylightStart, daylightEnd, currentRules.DaylightDelta);
            }

            return null;
        }

        private static DateTime GetNonFixedTransitionDate(TimeZoneInfo.TransitionTime transition, int year)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            int startOfWeek = transition.Week * 7 - 6;
            int firstDayOfWeek = (int)calendar.GetDayOfWeek(new DateTime(year, transition.Month, 1));

            int changeDayOfWeek = (int)transition.DayOfWeek;

            int transitionDay = (firstDayOfWeek <= changeDayOfWeek)
                ? startOfWeek + (changeDayOfWeek - firstDayOfWeek)
                : startOfWeek + (7 - firstDayOfWeek + changeDayOfWeek);

            if (transitionDay > calendar.GetDaysInMonth(year, transition.Month))
            {
                transitionDay -= 7;
            }

            return new DateTime(year, transition.Month, transitionDay, transition.TimeOfDay.Hour, transition.TimeOfDay.Minute, transition.TimeOfDay.Second);
        }

        /// <summary>
        /// Converts a transition time into a datetime depending on whether the transition time is a
        /// fixed date or a floating date e.g. in the UK daylight savings is a floating date.
        /// </summary>
        /// <param name="transition">The transition time value to convert to a datetime.</param>
        /// <param name="year">The year of the transition.</param>
        /// <see cref="https://docs.microsoft.com/en-us/dotnet/api/system.timezoneinfo.transitiontime.isfixeddaterule?view=netframework-4.8" />
        /// <returns>The transition as a datetime.</returns>
        private static DateTime GetTransitionDate(TimeZoneInfo.TransitionTime transition, int year)
        {
            return (transition.IsFixedDateRule)
                ? new DateTime(year, transition.Month, transition.Day,
                    transition.TimeOfDay.Hour, transition.TimeOfDay.Minute,
                    transition.TimeOfDay.Second)
                : GetNonFixedTransitionDate(transition, year);
        }
    }
}
