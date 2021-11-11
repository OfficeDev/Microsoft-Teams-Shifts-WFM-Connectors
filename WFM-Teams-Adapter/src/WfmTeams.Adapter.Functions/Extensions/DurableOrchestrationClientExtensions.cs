// ---------------------------------------------------------------------------
// <copyright file="DurableOrchestrationClientExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Extensions
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;

    public static class DurableOrchestrationClientExtensions
    {
        public static async Task<bool> TryStartSingletonAsync(this IDurableOrchestrationClient starter, string orchestrationName, string instanceId, object input)
        {
            var inactiveStatuses = new OrchestrationRuntimeStatus[]
            {
                OrchestrationRuntimeStatus.Terminated,
                OrchestrationRuntimeStatus.Failed,
                OrchestrationRuntimeStatus.Canceled,
                OrchestrationRuntimeStatus.Completed
            };
            var instance = await starter.GetStatusAsync(instanceId);

            if (instance == null || inactiveStatuses.Contains(instance.RuntimeStatus))
            {
                await starter.StartNewAsync(orchestrationName, instanceId, input);

                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
