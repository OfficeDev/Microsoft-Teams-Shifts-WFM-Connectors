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
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class ClearScheduleOrchestrator
    {
        private readonly TeamOrchestratorOptions _options;
        private readonly ITimeZoneHelper _timeZoneHelper;

        public ClearScheduleOrchestrator(TeamOrchestratorOptions options, ITimeZoneHelper timeZoneHelper)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeZoneHelper = timeZoneHelper ?? throw new ArgumentNullException(nameof(timeZoneHelper));
        }

        [FunctionName(nameof(ClearScheduleOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var clearScheduleModel = context.GetInput<ClearScheduleModel>();

            var pastWeeks = clearScheduleModel.PastWeeks ?? _options.PastWeeks;
            var futureWeeks = clearScheduleModel.FutureWeeks ?? _options.FutureWeeks;

            var timeZoneInfo = await _timeZoneHelper.GetAndUpdateTimeZone(clearScheduleModel.TeamId);

            clearScheduleModel.StartDate = context.CurrentUtcDateTime.Date
                .StartOfWeek(_options.StartDayOfWeek)
                .AddWeeks(-pastWeeks)
                .ApplyTimeZoneOffset(timeZoneInfo);

            clearScheduleModel.EndDate = context.CurrentUtcDateTime.Date
                .StartOfWeek(_options.StartDayOfWeek)
                .AddWeeks(futureWeeks + 1)
                .ApplyTimeZoneOffset(timeZoneInfo);

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