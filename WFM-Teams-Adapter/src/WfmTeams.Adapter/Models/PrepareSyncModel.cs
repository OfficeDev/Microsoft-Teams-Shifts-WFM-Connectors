// ---------------------------------------------------------------------------
// <copyright file="PrepareSyncModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    public enum SyncType
    {
        Shifts,
        OpenShifts,
        TimeOff,
        Availability,
        Users
    }

    /// <summary>
    /// Defines the model passed to WFM Connector prior to the sync of any given entity to allow the
    /// WFM Connector the opportunity to optimise the sync by pre-fetching data etc.
    /// </summary>
    public class PrepareSyncModel
    {
        public SyncType Type { get; set; }
        public string TeamId { get; set; }
        public string WfmId { get; set; }
        public DateTime FirstWeekStartDate { get; set; }
        public DateTime LastWeekStartDate { get; set; }
        public string TimeZoneInfoId { get; set; }
    }
}
