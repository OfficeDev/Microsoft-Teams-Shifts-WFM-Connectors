using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Orchestrators
{
    public class TeamOrchestrator
    {
        private readonly TeamOrchestratorOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleSourceService _scheduleSourceService;
        private readonly ITimeZoneService _timeZoneService;

        public TeamOrchestrator(TeamOrchestratorOptions options, IScheduleConnectorService scheduleConnectorService, IScheduleSourceService scheduleSourceService, ITimeZoneService timeZoneService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
            _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        }

        [FunctionName(nameof(TeamOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();
            teamModel.TimeZoneInfoId ??= await TimeZoneHelper.GetAndUpdateTimeZoneAsync(teamModel.TeamId, _timeZoneService, _scheduleConnectorService, _scheduleSourceService);

            if (!teamModel.Initialized)
            {
                await context.CallSubOrchestratorWithRetryAsync(nameof(InitializeOrchestrator), _options.AsRetryOptions(), teamModel);

                teamModel.Initialized = true;
            }

            // if we haven't got a valid timezone then we cannot sync shifts because we will not be able to properly
            // convert the shift start and end times to UTC
            if (teamModel.TimeZoneInfoId != null)
            {
                try
                {
                    var weeks = context.CurrentUtcDateTime.Date
                        .Range(_options.PastWeeks, _options.FutureWeeks, _options.StartDayOfWeek);

                    var weekTasks = weeks
                        .Select(startDate => new WeekModel
                        {
                            StartDate = startDate,
                            StoreId = teamModel.StoreId,
                            TeamId = teamModel.TeamId,
                            TimeZoneInfoId = teamModel.TimeZoneInfoId
                        })
                        .Select(weekModel => context.CallSubOrchestratorWithRetryAsync(nameof(WeekOrchestrator), _options.AsRetryOptions(), weekModel));

                    await Task.WhenAll(weekTasks);
                }
                catch (Exception ex) when (_options.ContinueOnError)
                {
                    log.LogTeamError(ex, teamModel);
                }
            }
            else
            {
                log.LogMissingTimeZoneError(teamModel);
            }

            if (_options.FrequencySeconds < 0)
            {
                return;
            }

            var dueTime = context.CurrentUtcDateTime.AddSeconds(_options.FrequencySeconds);

            await context.CreateTimer(dueTime, CancellationToken.None);

            context.ContinueAsNew(teamModel);
        }
    }
}