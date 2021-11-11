// ---------------------------------------------------------------------------
// <copyright file="DeferredActionOrchestrator.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Orchestrators
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Activities;
    using WfmTeams.Adapter.Functions.Models;

    public class DeferredActionOrchestrator
    {
        [FunctionName(nameof(DeferredActionOrchestrator))]
        public async Task Run([OrchestrationTrigger] IDurableOrchestrationContext context,
            ILogger log)
        {
            var actionModel = context.GetInput<DeferredActionModel>();
            var shareModel = new ShareModel
            {
                TeamId = actionModel.TeamId,
                StartDate = actionModel.ShareStartDate,
                EndDate = actionModel.ShareEndDate
            };

            DateTime startTime = context.CurrentUtcDateTime.Add(TimeSpan.FromSeconds(actionModel.DelaySeconds));
            await context.CreateTimer(startTime, CancellationToken.None);

            switch (actionModel.ActionType)
            {
                case DeferredActionModel.DeferredActionType.ApproveOpenShiftRequest:
                case DeferredActionModel.DeferredActionType.DeclineOpenShiftRequest:
                {
                    await context.CallActivityAsync(nameof(ReviewOpenShiftRequestActivity), actionModel);
                    break;
                }
                case DeferredActionModel.DeferredActionType.ApproveSwapShiftsRequest:
                {
                    await context.CallActivityAsync(nameof(ApproveSwapShiftsRequestActivity), actionModel);
                    break;
                }
                case DeferredActionModel.DeferredActionType.ShareTeamSchedule:
                {
                    await context.CallActivityAsync(nameof(ShareActivity), shareModel);
                    break;
                }
                default:
                {
                    break;
                }
            }
        }
    }
}
