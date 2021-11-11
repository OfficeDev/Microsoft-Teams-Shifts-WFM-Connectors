// ---------------------------------------------------------------------------
// <copyright file="SchedulingGroupsActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Services;

    public class SchedulingGroupsActivity
    {
        private readonly ITeamsService _teamsService;
        private readonly IWfmDataService _wfmDataService;

        public SchedulingGroupsActivity(ITeamsService teamsService, IWfmDataService wfmDataService)
        {
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
        }

        [FunctionName(nameof(SchedulingGroupsActivity))]
        public async Task Run([ActivityTrigger] TeamModel teamModel)
        {
            var scheduleGroups = await _teamsService.GetSchedulingGroupsAsync(teamModel.TeamId);
            var departments = await _wfmDataService.GetDepartmentsAsync(teamModel.TeamId, teamModel.WfmBuId);

            // grab groups which aren't in Teams Schedule Groups but are in WFM Departments
            var result = departments.Where(p => scheduleGroups.All(p2 => !p2.Name.Equals(p, StringComparison.OrdinalIgnoreCase)));
            // Empty list of userIds to pass into create group function
            List<string> userIds = new List<string>();
            foreach (var group in result)
            {
                await _teamsService.CreateSchedulingGroupAsync(teamModel.TeamId, group, userIds);
            }
        }
    }
}
