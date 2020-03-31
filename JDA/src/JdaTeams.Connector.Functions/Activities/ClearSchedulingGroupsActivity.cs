using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Activities
{
    public class ClearSchedulingGroupsActivity
    {
        private readonly IScheduleDestinationService _scheduleDestinationService;

        public ClearSchedulingGroupsActivity(IScheduleDestinationService scheduleDestinationService)
        {
             _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
        }

        [FunctionName(nameof(ClearSchedulingGroupsActivity))]
        public async Task Run([ActivityTrigger] string teamId, ILogger log)
        {
            var groupIds = await _scheduleDestinationService.ListActiveSchedulingGroupIdsAsync(teamId);
            var updateTasks = new List<Task>();
            foreach (var groupId in groupIds)
            {
                updateTasks.Add(_scheduleDestinationService.RemoveUsersFromSchedulingGroupAsync(teamId, groupId));
            }
            await Task.WhenAll(updateTasks);
        }
    }
}
