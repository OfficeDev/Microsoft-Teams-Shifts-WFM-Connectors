// ---------------------------------------------------------------------------
// <copyright file="DeferredActionModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System;

    public class DeferredActionModel
    {
        public enum DeferredActionType
        {
            None,
            ApproveOpenShiftRequest,
            DeclineOpenShiftRequest,
            ShareTeamSchedule,
            ApproveSwapShiftsRequest
        }

        public DeferredActionType ActionType { get; set; } = DeferredActionType.None;
        public int DelaySeconds { get; set; }
        public string Message { get; set; }
        public string RequestId { get; set; }
        public DateTime ShareEndDate { get; set; }
        public DateTime ShareStartDate { get; set; }
        public string TeamId { get; set; }
    }
}
