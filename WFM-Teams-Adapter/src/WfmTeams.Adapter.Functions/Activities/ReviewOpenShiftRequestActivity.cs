// ---------------------------------------------------------------------------
// <copyright file="ReviewOpenShiftRequestActivity.cs" company="Microsoft">
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

    public class ReviewOpenShiftRequestActivity
    {
        private readonly ITeamsService _teamsService;

        public ReviewOpenShiftRequestActivity(ITeamsService teamsService)
        {
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ReviewOpenShiftRequestActivity))]
        public async Task Run([ActivityTrigger] DeferredActionModel delayedActionModel, ILogger log)
        {
            log.LogReviewOpenShiftRequestActivity(delayedActionModel);
            try
            {
                if (delayedActionModel.ActionType == DeferredActionModel.DeferredActionType.ApproveOpenShiftRequest)
                {
                    await _teamsService.ApproveOpenShiftRequest(delayedActionModel.RequestId, delayedActionModel.TeamId).ConfigureAwait(false);
                }
                else if (delayedActionModel.ActionType == DeferredActionModel.DeferredActionType.DeclineOpenShiftRequest)
                {
                    await _teamsService.DeclineOpenShiftRequest(delayedActionModel.RequestId, delayedActionModel.TeamId, delayedActionModel.Message).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                // the automated approval could fail because all slots have already been allocated
                // to other users by the time the approval is processed
                log.LogReviewOpenShiftRequestActivityFailure(ex, delayedActionModel);
            }
        }
    }
}
