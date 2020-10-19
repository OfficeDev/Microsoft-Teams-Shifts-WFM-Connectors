using System;
using System.Linq;
using System.Threading.Tasks;
using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Activities;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.JdaPersona.Options;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class ClearScheduleOrchestrator
    {
        private readonly TeamOrchestratorOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleSourceService _scheduleSourceService;

        public ClearScheduleOrchestrator(TeamOrchestratorOptions options, IScheduleConnectorService scheduleConnectorService, IScheduleSourceService scheduleSourceService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
        }

        [FunctionName(nameof(ClearScheduleOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var clearScheduleModel = context.GetInput<ClearScheduleModel>();

            var pastWeeks = clearScheduleModel.PastWeeks ?? _options.PastWeeks;
            var futureWeeks = clearScheduleModel.FutureWeeks ?? _options.FutureWeeks;

            var timeZoneInfoId = await TimeZoneHelper.GetAndUpdateTimeZoneAsync(clearScheduleModel.TeamId, _scheduleConnectorService, _scheduleSourceService);
            timeZoneInfoId ??= _options.TimeZone;

            clearScheduleModel.StartDate = context.CurrentUtcDateTime.Date
                .StartOfWeek(_options.StartDayOfWeek)
                .AddWeeks(-pastWeeks)
                .ApplyTimeZoneOffset(timeZoneInfoId);

            clearScheduleModel.EndDate = context.CurrentUtcDateTime.Date
                .StartOfWeek(_options.StartDayOfWeek)
                .AddWeeks(futureWeeks + 1)
                .ApplyTimeZoneOffset(timeZoneInfoId);

            if (!context.IsReplaying)
            {
                log.LogClearStart(clearScheduleModel);
            }

            var tasks = Enumerable.Range(0, clearScheduleModel.EndDate.Subtract(clearScheduleModel.StartDate).Days)
                .Select(offset => new ClearScheduleModel
                {
                    StartDate = clearScheduleModel.StartDate.AddDays(offset),
                    EndDate = clearScheduleModel.StartDate.AddDays(offset).AddHours(23).AddMinutes(59),
                    TeamId = clearScheduleModel.TeamId
                })
                .Select(model => context.CallSubOrchestratorAsync(nameof(ClearShiftsDayOrchestrator), model));

            await Task.WhenAll(tasks);

            // at this stage, we should have deleted all the shifts for each of the days in the period, apart from those
            // that span midnight on any day, so we need to execute a final ClearShiftsDayOrchestrator for the full date 
            // range plus 24 hours in order to include those remaining shifts
            clearScheduleModel.QueryEndDate = clearScheduleModel.EndDate.AddHours(24);
            await context.CallSubOrchestratorAsync(nameof(ClearShiftsDayOrchestrator), clearScheduleModel);

            if (clearScheduleModel.ClearSchedulingGroups)
            {
                await context.CallActivityAsync(nameof(ClearSchedulingGroupsActivity), clearScheduleModel.TeamId);
            }

            await context.CallActivityAsync(nameof(ClearCacheActivity), clearScheduleModel);
        }
    }
}