// ---------------------------------------------------------------------------
// <copyright file="UpdateScheduleModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    public class UpdateScheduleModel
    {
        public string TeamIds { get; set; }
        public bool UpdateAllTeams { get; set; }
    }
}