// ---------------------------------------------------------------------------
// <copyright file="OpenShiftsWeekOrchestrator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Orchestrators
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Activities;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Options;

    public class OpenShiftsWeekOrchestrator
    {
        private readonly ConnectorOptions _options;

        public OpenShiftsWeekOrchestrator(ConnectorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(OpenShiftsWeekOrchestrator))]
        public async Task<bool> Run([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var weekModel = context.GetInput<WeekModel>();

            var resultModel = new ResultModel();

            try
            {
                resultModel = await context.CallActivityAsync<ResultModel>(nameof(OpenShiftsWeekActivity), weekModel);
            }
            finally
            {
                if (!context.IsReplaying && resultModel.TotalCount > 0)
                {
                    log.LogMetrics(weekModel.AsDimensions("OpenShifts"), resultModel, "OpenShifts");
                }
            }

            return resultModel.HasChanges;
        }
    }
}
