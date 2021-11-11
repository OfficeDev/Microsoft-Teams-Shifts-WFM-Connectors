// ---------------------------------------------------------------------------
// <copyright file="DateTimeHelper.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Helpers
{
    using System;
    using System.Globalization;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Services;

    /// <summary>
    /// Provides a number of helper functions for manipulating datetime entities.
    /// </summary>
    public static class DateTimeHelper
    {
        public static string ConvertFromLocalTime(string time, string localTimeZone, ISystemTimeService timeService)
        {
            time = TrimFractionalSeconds(time);
            var localDateTime = DateTime.ParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture);

            if (timeService.Today != DateTime.Today)
            {
                // in a testing scenario where we are working to a date other than today it is
                // necessary to reformulate the localDateTime to the date specified for the test,
                // otherwise it defaults to today
                localDateTime = timeService.Today.AddHours(localDateTime.Hour).AddMinutes(localDateTime.Minute).AddSeconds(localDateTime.Second);
            }

            var dateTime = localDateTime.ConvertFromLocalTime(localTimeZone, timeService);
            return dateTime.AsTimeString();
        }

        public static string ConvertToLocalTime(string time, string localTimeZone, ISystemTimeService timeService)
        {
            var localDateTime = DateTime.ParseExact(time, "HH:mm:ss", CultureInfo.InvariantCulture);

            if (timeService.Today != DateTime.Today)
            {
                // in a testing scenario where we are working to a date other than today it is
                // necessary to reformulate the localDateTime to the date specified for the test,
                // otherwise it defaults to today
                localDateTime = timeService.Today.AddHours(localDateTime.Hour).AddMinutes(localDateTime.Minute).AddSeconds(localDateTime.Second);
            }

            var dateTime = localDateTime.ApplyTimeZoneOffset(localTimeZone);
            return dateTime.AsTimeString();
        }

        private static string TrimFractionalSeconds(string time)
        {
            var pos = time.IndexOf(".");
            if (pos > -1)
            {
                return time.Substring(0, pos);
            }

            return time;
        }
    }
}
