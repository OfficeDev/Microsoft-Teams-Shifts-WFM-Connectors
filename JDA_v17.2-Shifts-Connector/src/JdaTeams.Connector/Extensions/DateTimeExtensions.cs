using System;
using System.Linq;

namespace JdaTeams.Connector.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var diff = (7 + (dt.DayOfWeek - startOfWeek)) % 7;
            return dt.AddDays(-1 * diff).Date;
        }

        public static DateTime AddWeeks(this DateTime dt, int value)
        {
            return dt.AddDays(7 * value);
        }

        public static DateTime AddWeek(this DateTime dt)
        {
            return dt.AddWeeks(1);
        }

        public static DateTime[] Range(this DateTime dt, int pastWeeks, int futureWeeks, DayOfWeek startOfWeek = DayOfWeek.Monday)
        {
            var weekStart = dt.StartOfWeek(startOfWeek);
            var totalWeeks = pastWeeks + futureWeeks + 1;

            return Enumerable.Range(-pastWeeks, totalWeeks)
                .Select(offset => weekStart.AddWeeks(offset))
                .ToArray();
        }

        public static DateTime ApplyTimeZoneOffset(this DateTime utc, string TimeZone)
        {
            var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(TimeZone);
            var offsetTimespan = timeZoneInfo.GetUtcOffset(utc);

            return utc - offsetTimespan;
        }

        public static string AsDateString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd");
        }

        public static string AsDateTimeString(this DateTime dt)
        {
            return dt.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
