// ---------------------------------------------------------------------------
// <copyright file="ScheduleActivity.cs" company="Microsoft">
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
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ScheduleActivity
    {
        private readonly ScheduleActivityOptions _options;
        private readonly ITeamsService _teamsService;
        private readonly WorkforceIntegrationOptions _wfiOptions;

        public ScheduleActivity(ScheduleActivityOptions options, WorkforceIntegrationOptions wfiOptions, ITeamsService teamsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _wfiOptions = wfiOptions ?? throw new ArgumentNullException(nameof(wfiOptions));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ScheduleActivity))]
        public async Task Run([ActivityTrigger] TeamModel teamModel, ILogger log)
        {
            var schedule = await _teamsService.GetScheduleAsync(teamModel.TeamId);

            if (schedule.IsUnavailable)
            {
                var scheduleModel = ScheduleModel.Create(_options, _wfiOptions.WorkforceIntegrationId, teamModel.TimeZoneInfoId);
                await _teamsService.CreateScheduleAsync(teamModel.TeamId, scheduleModel).ConfigureAwait(false);
            }
            else if (schedule.IsProvisioned)
            {
                var changed = false;

                if (string.IsNullOrEmpty(_wfiOptions.WorkforceIntegrationId) && schedule.WorkforceIntegrationIds.Count > 0)
                {
                    // currently Teams only supports a single integration and therefore this is
                    // valid, but if this should change then we need to remove only the integration
                    // that we previoulsy added and preserve the rest
                    schedule.WorkforceIntegrationIds.Clear();
                    changed = true;
                }
                else if (!string.IsNullOrEmpty(_wfiOptions.WorkforceIntegrationId) && !schedule.WorkforceIntegrationIds.Contains(_wfiOptions.WorkforceIntegrationId))
                {
                    schedule.WorkforceIntegrationIds.Add(_wfiOptions.WorkforceIntegrationId);
                    changed = true;
                }

                if (schedule.TimeClockEnabled != _options.TimeClockEnabled)
                {
                    schedule.TimeClockEnabled = _options.TimeClockEnabled;
                    changed = true;
                }

                if (schedule.OpenShiftsEnabled != _options.OpenShiftsEnabled)
                {
                    schedule.OpenShiftsEnabled = _options.OpenShiftsEnabled;
                    changed = true;
                }

                if (schedule.SwapShiftsRequestsEnabled != _options.SwapShiftsRequestsEnabled)
                {
                    schedule.SwapShiftsRequestsEnabled = _options.SwapShiftsRequestsEnabled;
                    changed = true;
                }

                if (schedule.OfferShiftRequestsEnabled != _options.OfferShiftRequestsEnabled)
                {
                    schedule.OfferShiftRequestsEnabled = _options.OfferShiftRequestsEnabled;
                    changed = true;
                }

                if (schedule.TimeOffRequestsEnabled != _options.TimeOffRequestsEnabled)
                {
                    schedule.TimeOffRequestsEnabled = _options.TimeOffRequestsEnabled;
                    changed = true;
                }

                if (changed)
                {
                    await _teamsService.UpdateScheduleAsync(teamModel.TeamId, schedule).ConfigureAwait(false);
                }

                log.LogSchedule(teamModel, schedule);

                return;
            }

            for (var i = 0; i < _options.PollMaxAttempts; i++)
            {
                await Task.Delay(_options.AsPollIntervalTimeSpan());

                schedule = await _teamsService.GetScheduleAsync(teamModel.TeamId).ConfigureAwait(false);

                log.LogSchedule(teamModel, schedule);

                if (schedule.IsProvisioned)
                {
                    // we have observed and reported a bug in Teams where supplying the workforce
                    // integration id in the initial create is ignored, so this is a workaround - to
                    // set it after it has been provisioned
                    if (!string.IsNullOrEmpty(_wfiOptions.WorkforceIntegrationId) && (schedule.WorkforceIntegrationIds.Count == 0 || !schedule.WorkforceIntegrationIds.Contains(_wfiOptions.WorkforceIntegrationId)))
                    {
                        schedule.WorkforceIntegrationIds.Add(_wfiOptions.WorkforceIntegrationId);
                        await _teamsService.UpdateScheduleAsync(teamModel.TeamId, schedule).ConfigureAwait(false);
                    }
                    return;
                }
            }

            throw new TimeoutException();
        }
    }
}
