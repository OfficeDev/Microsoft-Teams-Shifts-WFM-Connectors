using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Activities
{
    public class ClearCacheActivity
    {
        private readonly TeamOrchestratorOptions _options;
        private readonly IScheduleCacheService _scheduleCacheService;
        private readonly IScheduleDestinationService _scheduleDestinationService;

        public ClearCacheActivity(TeamOrchestratorOptions options, IScheduleCacheService scheduleCacheService, IScheduleDestinationService scheduleDestinationService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
        }

        [FunctionName(nameof(ClearCacheActivity))]
        public async Task Run([ActivityTrigger] ClearScheduleModel clearScheduleModel, ILogger log)
        {
            var pastWeeks = clearScheduleModel.PastWeeks ?? _options.PastWeeks;
            var futureWeeks = clearScheduleModel.FutureWeeks ?? _options.FutureWeeks;

            var weeksRange = DateTime.Today
                .Range(pastWeeks, futureWeeks, _options.StartDayOfWeek);

            foreach (var week in weeksRange)
            {
                await _scheduleCacheService.DeleteScheduleAsync(clearScheduleModel.TeamId, week);
            }
        }
    }
}
