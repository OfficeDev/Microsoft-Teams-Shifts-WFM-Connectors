// ---------------------------------------------------------------------------
// <copyright file="TimeOffReasonModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    public class TimeOffReasonModel
    {
        public string WfmTimeOffReasonId { get; set; }
        public string Reason { get; set; }
        public string TeamsTimeOffReasonId { get; set; }
    }
}
