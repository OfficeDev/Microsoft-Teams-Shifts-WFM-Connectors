// ---------------------------------------------------------------------------
// <copyright file="StopTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Services;

    public class StopTrigger
    {
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public StopTrigger(IScheduleConnectorService scheduleConnectorService)
        {
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        public static async Task StopRunningOrchestratorsAsync(string teamId, IDurableOrchestrationClient starter)
        {
            await StopRunningOrchestratorAsync(EmployeeTokenRefreshOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
            await StopRunningOrchestratorAsync(AvailabilityOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
            await StopRunningOrchestratorAsync(TimeOffOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
            await StopRunningOrchestratorAsync(OpenShiftsOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
            await StopRunningOrchestratorAsync(ShiftsOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
            await StopRunningOrchestratorAsync(EmployeeCacheOrchestrator.InstanceIdPattern, teamId, starter).ConfigureAwait(false);
        }

        [FunctionName(nameof(StopTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "stop/{teamId}")] HttpRequest req,
            [DurableClient] IDurableOrchestrationClient starter,
            string teamId,
            ILogger log)
        {
            await _scheduleConnectorService.UpdateEnabledAsync(teamId, false).ConfigureAwait(false);
            await StopRunningOrchestratorsAsync(teamId, starter).ConfigureAwait(false);
            log.LogDisableOrchestrators(teamId);

            return new OkResult();
        }

        private static async Task StopRunningOrchestratorAsync(string instanceIdPattern, string teamId, IDurableOrchestrationClient starter)
        {
            var instanceId = string.Format(instanceIdPattern, teamId);
            var status = await starter.GetStatusAsync(instanceId).ConfigureAwait(false);
            if (status?.RuntimeStatus == OrchestrationRuntimeStatus.Running || status?.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
            {
                await starter.TerminateAsync(instanceId, nameof(StopTrigger)).ConfigureAwait(false);
            }
        }
    }
}
