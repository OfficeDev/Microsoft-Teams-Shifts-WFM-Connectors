// ---------------------------------------------------------------------------
// <copyright file="ConnectionModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System;

    /// <summary>
    /// The model defining the data that is stored about each Teams team to WFM business unit connection.
    /// </summary>
    public class ConnectionModel
    {
        /// <summary>
        /// Used to stop all syncs for the team
        /// </summary>
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// Last AvailabilityOrchestratorExecution
        /// </summary>
        public DateTime? LastAOExecution { get; set; }

        /// <summary>
        /// Last EmployeeCacheOrchestratorExecution
        /// </summary>
        public DateTime? LastECOExecution { get; set; }

        /// <summary>
        /// Last EmployeeTokenRefreshOrchestratorExecution
        /// </summary>
        public DateTime? LastETROExecution { get; set; }

        /// <summary>
        /// Last OpenShiftOrchestratorExecution
        /// </summary>
        public DateTime? LastOSOExecution { get; set; }

        /// <summary>
        /// Last ShiftsOrchestratorExecution
        /// </summary>
        public DateTime? LastSOExecution { get; set; }

        /// <summary>
        /// Last TimeOffOrchestratorExecution
        /// </summary>
        public DateTime? LastTOOExecution { get; set; }

        public string WfmBuId { get; set; }
        public string WfmBuName { get; set; }
        public string TeamId { get; set; }
        public string TeamName { get; set; }

        /// <summary>
        /// Used to flag when an orchestrator has been started and one or more of the last execution
        /// dates has been updated
        /// </summary>
        public bool Updated { get; set; }

        public string TimeZoneInfoId { get; set; }
    }
}
