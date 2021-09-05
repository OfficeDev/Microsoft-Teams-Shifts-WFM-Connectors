// ---------------------------------------------------------------------------
// <copyright file="ClearScheduleTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Models;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Orchestrators;
    using WfmTeams.Adapter.Services;

    public class ClearScheduleTrigger
    {
        private readonly TeamOrchestratorOptions _options;

        private readonly IScheduleConnectorService _scheduleConnectorService;

        private readonly ISystemTimeService _timeService;

        public ClearScheduleTrigger(TeamOrchestratorOptions options, ISystemTimeService timeService, IScheduleConnectorService scheduleConnectorService)
        {
            _options = options ?? throw new System.ArgumentNullException(nameof(options));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
        }

        [FunctionName(nameof(ClearScheduleTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "clearschedule")] ClearScheduleModel clearScheduleModel,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            if (!clearScheduleModel.IsValid())
            {
                return new BadRequestResult();
            }

            // ensure that the team's orchestrators will not execute by disabling them
            await _scheduleConnectorService.UpdateEnabledAsync(clearScheduleModel.TeamId, false).ConfigureAwait(false);

            // get the connection model as we need the time zone information for the team
            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(clearScheduleModel.TeamId).ConfigureAwait(false);

            try
            {
                SetStartAndEndDates(clearScheduleModel, connectionModel.TimeZoneInfoId);
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }

            if (await starter.TryStartSingletonAsync(nameof(ClearScheduleOrchestrator), ClearScheduleOrchestrator.InstanceId(clearScheduleModel.TeamId), clearScheduleModel).ConfigureAwait(false))
            {
                return new OkResult();
            }
            else
            {
                return new ConflictResult();
            }
        }

        /// <summary>
        /// The purpose of this method is to set the StartDate and EndDate values on the
        /// ClearScheduleModel thus defining the full range of the dates in complete weeks that will
        /// be cleared.
        /// </summary>
        /// <param name="clearScheduleModel">The model sent by the caller.</param>
        /// <remarks>
        /// Users can supply these dates in which case we need to ensure that whatever they entered,
        /// the start date is the first day of that week and the end date is the last day of that
        /// week. If the dates are not supplied by the user but they did supply values for the
        /// PastWeeks and FutureWeeks then we will compute the start and end dates on the basis of
        /// those. Finally if the user supplied neither start and end dates nor past and future
        /// weeks then we will use the past and future weeks configured for the syncs to compute the
        /// dates. In addition to the start and end dates, this method also sets the utc start and
        /// end times of the date range because we have to use utc dates when calling the graph api
        /// to get the list of items to be deleted.
        /// </remarks>
        private void SetStartAndEndDates(ClearScheduleModel clearScheduleModel, string timeZoneInfoId)
        {
            var pastWeeks = clearScheduleModel.PastWeeks ?? _options.PastWeeks;
            var futureWeeks = clearScheduleModel.FutureWeeks ?? _options.FutureWeeks;

            if (clearScheduleModel.StartDate == default)
            {
                // if the start and end dates have not been explicitly supplied then we must compute
                // them from the past and future weeks values relative to today
                clearScheduleModel.StartDate = _timeService.UtcNow
                    .StartOfWeek(_options.StartDayOfWeek)
                    .AddWeeks(-pastWeeks)
                    .Date;

                clearScheduleModel.EndDate = _timeService.UtcNow
                    .StartOfWeek(_options.StartDayOfWeek)
                    .AddWeeks(futureWeeks + 1)
                    .AddDays(-1)
                    .Date;
            }
            else
            {
                // ensure that the start date is the start of the week of the start date because we
                // cannot handle periods of less than 1 full week because of the clearcacheactivity
                // which deletes the cache for the whole week
                clearScheduleModel.StartDate = clearScheduleModel.StartDate.StartOfWeek(_options.StartDayOfWeek);

                // because a start date has been specified, we need to ensure that an end date has
                // also been supplied or if not use the start date to default it to the end date of
                // the same week
                if (clearScheduleModel.EndDate == default)
                {
                    // e.g. startdate = 23/08/2020 then end date = 29/08/2020
                    clearScheduleModel.EndDate = clearScheduleModel.StartDate.AddDays(6);
                }
                else
                {
                    // make sure the end date is the end date for the week it appears within
                    clearScheduleModel.EndDate = clearScheduleModel.EndDate.StartOfWeek(_options.StartDayOfWeek).AddDays(6);
                }
            }

            // validate the start date is < end date
            if (clearScheduleModel.StartDate > clearScheduleModel.EndDate)
            {
                throw new ArgumentException("End date must be after start date.", nameof(clearScheduleModel));
            }

            // ensure that the end date is the end of the day i.e. 23:59:59 and not the start i.e. 00:00:00
            clearScheduleModel.EndDate = clearScheduleModel.EndDate.AddDays(1).AddSeconds(-1);

            clearScheduleModel.UtcStartDate = clearScheduleModel.StartDate.ConvertFromLocalTime(timeZoneInfoId, _timeService);
            clearScheduleModel.UtcEndDate = clearScheduleModel.EndDate.ConvertFromLocalTime(timeZoneInfoId, _timeService);
        }
    }
}
