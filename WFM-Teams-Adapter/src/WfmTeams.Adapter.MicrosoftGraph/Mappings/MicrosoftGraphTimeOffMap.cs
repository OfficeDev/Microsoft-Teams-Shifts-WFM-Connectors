// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphTimeOffMap.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Mappings
{
    using System;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Models;

    public class MicrosoftGraphTimeOffMap : ITimeOffMap
    {
        private readonly MicrosoftGraphOptions _options;

        public MicrosoftGraphTimeOffMap(MicrosoftGraphOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public TimeOffItem MapTimeOff(TimeOffModel timeOff)
        {
            return new TimeOffItem
            {
                StartDateTime = timeOff.StartDate,
                EndDateTime = RoundEndDate(timeOff.EndDate),
                TimeOffReasonId = timeOff.TeamsTimeOffReasonId,
                Theme = _options.TimeOffTheme
            };
        }

        /// <summary>
        /// For end dates with 59 minutes, rounds the end date up to the complete hour and zeros the seconds.
        /// </summary>
        /// <param name="endDate">The end date to round.</param>
        /// <returns>The rounded end date.</returns>
        private DateTime RoundEndDate(DateTime endDate)
        {
            if (endDate.Minute == 59)
            {
                return endDate.AddMinutes(1).AddSeconds(-endDate.Second);
            }

            return endDate;
        }
    }
}
