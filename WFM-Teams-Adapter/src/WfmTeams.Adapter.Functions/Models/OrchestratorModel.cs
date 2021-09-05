// ---------------------------------------------------------------------------
// <copyright file="OrchestratorModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;

    public class OrchestratorModel
    {
        public OrchestratorModel(DurableOrchestrationStatus status)
        {
            Name = status.Name;
            InstanceId = status.InstanceId;
            RuntimeStatus = status.RuntimeStatus.ToString();
            LastUpdatedTime = status.LastUpdatedTime.ToString("o");
        }

        public string InstanceId { get; set; }
        public string LastUpdatedTime { get; set; }
        public string Name { get; set; }
        public string RuntimeStatus { get; set; }
    }
}
