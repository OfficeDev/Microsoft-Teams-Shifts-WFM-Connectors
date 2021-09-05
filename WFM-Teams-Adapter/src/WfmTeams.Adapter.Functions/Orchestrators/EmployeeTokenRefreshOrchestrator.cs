// ---------------------------------------------------------------------------
// <copyright file="EmployeeTokenRefreshOrchestrator.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Options;

    public class EmployeeTokenRefreshOrchestrator
    {
        private readonly TeamOrchestratorOptions _options;

        public EmployeeTokenRefreshOrchestrator(TeamOrchestratorOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public static string InstanceIdPattern => "{0}-CookieRefresh";

        [FunctionName(nameof(EmployeeTokenRefreshOrchestrator))]
        public async Task RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();

            try
            {
                await context.CallActivityAsync(nameof(EmployeeTokenRefreshActivity), teamModel);
            }
            catch (AggregateException aex)
            {
                log.LogAggregateOrchestrationError(aex, teamModel, nameof(EmployeeTokenRefreshOrchestrator));
            }
            catch (Exception ex)
            {
                log.LogOrchestrationError(ex, teamModel, nameof(EmployeeTokenRefreshOrchestrator));
            }
        }
    }
}
