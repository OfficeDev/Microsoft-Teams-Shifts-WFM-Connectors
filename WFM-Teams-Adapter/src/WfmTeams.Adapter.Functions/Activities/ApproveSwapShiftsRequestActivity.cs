// ---------------------------------------------------------------------------
// <copyright file="ApproveSwapShiftsRequestActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Services;

    public class ApproveSwapShiftsRequestActivity
    {
        private readonly ITeamsService _teamsService;

        public ApproveSwapShiftsRequestActivity(ITeamsService teamsService)
        {
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ApproveSwapShiftsRequestActivity))]
        public async Task Run([ActivityTrigger] DeferredActionModel delayedActionModel, ILogger log)
        {
            log.LogApproveSwapShiftsRequestActivity(delayedActionModel);
            try
            {
                if (delayedActionModel.ActionType == DeferredActionModel.DeferredActionType.ApproveSwapShiftsRequest)
                {
                    await _teamsService.ApproveSwapShiftsRequest(delayedActionModel.Message, delayedActionModel.RequestId, delayedActionModel.TeamId).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogApproveSwapShiftsRequestActivityFailure(ex, delayedActionModel);
            }
        }
    }
}
