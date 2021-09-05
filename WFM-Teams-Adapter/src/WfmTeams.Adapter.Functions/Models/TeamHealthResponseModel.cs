// ---------------------------------------------------------------------------
// <copyright file="TeamHealthResponseModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System.Collections.Generic;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using WfmTeams.Adapter.Models;

    public class TeamHealthResponseModel
    {
        private const string UnknownStatus = "Unknown";
        public string AvailabilityOrchestratorRuntimeStatus => AvailabilityOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus AvailabilityOrchestratorStatus { get; internal set; }
        public List<ShiftModel> CachedShifts { get; set; } = new List<ShiftModel>();
        public string EmployeeCacheOrchestratorRuntimeStatus => EmployeeCacheOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus EmployeeCacheOrchestratorStatus { get; set; }
        public string EmployeeTokenRefreshOrchestratorRuntimeStatus => EmployeeTokenRefreshOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus EmployeeTokenRefreshOrchestratorStatus { get; set; }
        public List<EmployeeModel> MappedUsers { get; set; } = new List<EmployeeModel>();
        public List<ShiftModel> MissingShifts { get; set; } = new List<ShiftModel>();
        public List<EmployeeModel> MissingUsers { get; set; } = new List<EmployeeModel>();
        public string OpenShiftsOrchestratorRuntimeStatus => OpenShiftsOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus OpenShiftsOrchestratorStatus { get; internal set; }
        public string TeamId { get; set; }
        public string TeamOrchestratorRuntimeStatus => TeamOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus TeamOrchestratorStatus { get; set; }
        public string TimeOffOrchestratorRuntimeStatus => TimeOffOrchestratorStatus?.RuntimeStatus.ToString() ?? UnknownStatus;
        public DurableOrchestrationStatus TimeOffOrchestratorStatus { get; set; }
        public string WeekStartDate { get; set; }
    }
}
