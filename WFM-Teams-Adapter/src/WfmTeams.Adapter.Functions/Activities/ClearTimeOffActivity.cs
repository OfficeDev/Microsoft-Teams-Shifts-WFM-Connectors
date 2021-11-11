// ---------------------------------------------------------------------------
// <copyright file="ClearTimeOffActivity.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Activities
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ClearTimeOffActivity
    {
        private readonly ClearScheduleOptions _options;

        private readonly ITeamsService _teamsService;

        public ClearTimeOffActivity(ClearScheduleOptions options, ITeamsService teamsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ClearTimeOffActivity))]
        public async Task Run([ActivityTrigger] ClearScheduleModel clearScheduleModel, ILogger log)
        {
            try
            {
                var timeOffRecs = await _teamsService.ListTimeOffAsync(clearScheduleModel.TeamId, clearScheduleModel.UtcStartDate, clearScheduleModel.QueryEndDate ?? clearScheduleModel.UtcEndDate, _options.ClearScheduleBatchSize).ConfigureAwait(false);

                // restrict the time off to delete those that actually started between the start and
                // end dates
                timeOffRecs = timeOffRecs.Where(s => s.StartDate < clearScheduleModel.UtcEndDate).ToList();
                if (timeOffRecs.Count > 0)
                {
                    var tasks = timeOffRecs
                        .Select(timeOff => TryDeleteTimeOffAsync(clearScheduleModel, timeOff, log))
                        .ToArray();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogTimeOffError(ex, clearScheduleModel, nameof(_teamsService.ListTimeOffAsync));
            }
        }

        private async Task<bool> TryDeleteTimeOffAsync(ClearScheduleModel clearScheduleModel, TimeOffModel timeOff, ILogger log)
        {
            try
            {
                await _teamsService.DeleteTimeOffAsync(clearScheduleModel.TeamId, timeOff, true).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                log.LogTimeOffError(ex, clearScheduleModel, nameof(_teamsService.DeleteTimeOffAsync), timeOff);
                return false;
            }
        }
    }
}
