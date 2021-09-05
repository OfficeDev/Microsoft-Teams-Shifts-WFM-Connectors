// ---------------------------------------------------------------------------
// <copyright file="ClearShiftsActivity.cs" company="Microsoft">
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

    public class ClearShiftsActivity
    {
        private readonly ClearScheduleOptions _options;

        private readonly ITeamsService _teamsService;

        public ClearShiftsActivity(ClearScheduleOptions options, ITeamsService teamsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ClearShiftsActivity))]
        public async Task Run([ActivityTrigger] ClearScheduleModel clearScheduleModel, ILogger log)
        {
            try
            {
                // Retrieve all shifts for this day using paging
                var shifts = await _teamsService.ListShiftsAsync(clearScheduleModel.TeamId, clearScheduleModel.UtcStartDate, clearScheduleModel.QueryEndDate ?? clearScheduleModel.UtcEndDate, _options.ClearScheduleBatchSize).ConfigureAwait(false);

                // restrict the shifts to delete to those that actually started between the start
                // and end dates
                shifts = shifts.Where(s => s.StartDate < clearScheduleModel.UtcEndDate).ToList();
                if (shifts.Count > 0)
                {
                    var tasks = shifts
                        .Select(shift => TryDeleteShiftAsync(clearScheduleModel, shift, log))
                        .ToArray();

                    await Task.WhenAll(tasks).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                log.LogShiftError(ex, clearScheduleModel, nameof(_teamsService.ListShiftsAsync));
            }
        }

        private async Task<bool> TryDeleteShiftAsync(ClearScheduleModel clearScheduleModel, ShiftModel shift, ILogger log)
        {
            try
            {
                await _teamsService.DeleteShiftAsync(clearScheduleModel.TeamId, shift, true).ConfigureAwait(false);
                return true;
            }
            catch (Exception ex)
            {
                log.LogShiftError(ex, clearScheduleModel, nameof(_teamsService.DeleteShiftAsync), shift);
                return false;
            }
        }
    }
}
