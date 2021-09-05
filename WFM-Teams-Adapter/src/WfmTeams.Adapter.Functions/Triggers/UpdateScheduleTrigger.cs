// ---------------------------------------------------------------------------
// <copyright file="UpdateScheduleTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Services;

    public class UpdateScheduleTrigger
    {
        private readonly IScheduleConnectorService _scheduleConnectorService;

        public UpdateScheduleTrigger(IScheduleConnectorService scheduleConnectorService)
        {
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(UpdateScheduleTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "updateschedule")] UpdateScheduleModel updateScheduleModel,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            log.LogUpdateSchedule(updateScheduleModel, nameof(UpdateScheduleTrigger));

            if (updateScheduleModel.UpdateAllTeams)
            {
                var connections = await _scheduleConnectorService.ListConnectionsAsync().ConfigureAwait(false);
                var teamIds = new List<string>();
                foreach (var connection in connections)
                {
                    teamIds.Add(connection.TeamId);
                }
                updateScheduleModel.TeamIds = string.Join(",", teamIds);
            }

            if (await starter.TryStartSingletonAsync(nameof(UpdateScheduleOrchestrator), UpdateScheduleOrchestrator.InstanceId, updateScheduleModel).ConfigureAwait(false))
            {
                return new OkResult();
            }
            else
            {
                return new ConflictResult();
            }
        }
    }
}
