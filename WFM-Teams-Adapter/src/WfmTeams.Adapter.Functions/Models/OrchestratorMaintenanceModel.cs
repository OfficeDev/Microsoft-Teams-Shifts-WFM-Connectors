// ---------------------------------------------------------------------------
// <copyright file="OrchestratorMaintenanceModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System.Collections.Generic;

    public class OrchestratorMaintenanceModel
    {
        public int CancelledCount { get; internal set; }
        public int CompletedCount { get; internal set; }
        public int ContinuedAsNewCount { get; internal set; }
        public int FailedCount { get; internal set; }
        public List<OrchestratorModel> Orchestrators { get; } = new List<OrchestratorModel>();
        public int PendingCount { get; internal set; }
        public int RunningCount { get; internal set; }
        public int TerminatedCount { get; internal set; }
        public int UnnownCount { get; internal set; }
    }
}
