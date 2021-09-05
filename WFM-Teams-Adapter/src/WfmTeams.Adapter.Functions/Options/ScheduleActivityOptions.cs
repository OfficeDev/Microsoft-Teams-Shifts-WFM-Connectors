// ---------------------------------------------------------------------------
// <copyright file="ScheduleActivityOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    using System;
    using WfmTeams.Adapter.Options;

    public class ScheduleActivityOptions : ConnectorOptions
    {
        public int PollIntervalSeconds { get; set; } = 10;
        public int PollMaxAttempts { get; set; } = 20;

        public TimeSpan AsPollIntervalTimeSpan()
        {
            return TimeSpan.FromSeconds(PollIntervalSeconds);
        }
    }
}
