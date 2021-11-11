// ---------------------------------------------------------------------------
// <copyright file="WeekActivityOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    using WfmTeams.Adapter.Options;

    public class WeekActivityOptions : ConnectorOptions
    {
        public bool AbortSyncOnZeroSourceRecords { get; set; } = true;
        public int MaximumDelta { get; set; } = 100;
        public bool NotifyTeamOnChange { get; set; } = false;
    }
}
