// ---------------------------------------------------------------------------
// <copyright file="ClearSchedulingGroupsActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Services;

    public class ClearSchedulingGroupsActivity
    {
        private readonly ITeamsService _teamsService;

        public ClearSchedulingGroupsActivity(ITeamsService teamsService)
        {
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ClearSchedulingGroupsActivity))]
        public async Task Run([ActivityTrigger] string teamId, ILogger log)
        {
            var groupIds = await _teamsService.ListActiveSchedulingGroupIdsAsync(teamId).ConfigureAwait(false);
            var updateTasks = new List<Task>();
            foreach (var groupId in groupIds)
            {
                updateTasks.Add(_teamsService.RemoveUsersFromSchedulingGroupAsync(teamId, groupId));
            }
            await Task.WhenAll(updateTasks).ConfigureAwait(false);
        }
    }
}
