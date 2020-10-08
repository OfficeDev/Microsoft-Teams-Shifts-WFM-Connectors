using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
using JdaTeams.Connector.Options;
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
        private readonly ConnectorOptions _connectorOptions;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleSourceService _scheduleSourceService;

        public TeamOrchestrator(TeamOrchestratorOptions options, IScheduleSourceService scheduleSourceService, IScheduleConnectorService scheduleConnectorService, ConnectorOptions connectorOptions)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _connectorOptions = connectorOptions ?? throw new ArgumentNullException(nameof(connectorOptions));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
        }

        [FunctionName(nameof(TeamOrchestrator))]
        public async Task Run([OrchestrationTrigger] DurableOrchestrationContext context,
            ILogger log)
        {
            var teamModel = context.GetInput<TeamModel>();

            if (!teamModel.Initialized)
            {
                await context.CallSubOrchestratorWithRetryAsync(nameof(InitializeOrchestrator), _options.AsRetryOptions(), teamModel);

                teamModel.Initialized = true;
            }

            try
            {
                var weeks = context.CurrentUtcDateTime.Date
                    .Range(_options.PastWeeks, _options.FutureWeeks, _options.StartDayOfWeek);

                var timeZoneHelper = new TimeZoneHelper(_scheduleSourceService, _scheduleConnectorService, _connectorOptions);

                var weekTasks = weeks
                    .Select(async startDate => new WeekModel
                    {
                        StartDate = startDate,
                        StoreId = teamModel.StoreId,
                        TeamId = teamModel.TeamId,
                        TimeZoneInfoId = await timeZoneHelper.GetAndUpdateTimeZone(teamModel.TeamId)
                    })
                    .Select(weekModel => context.CallSubOrchestratorWithRetryAsync(nameof(WeekOrchestrator), _options.AsRetryOptions(), weekModel));

                await Task.WhenAll(weekTasks);
            }
            catch (Exception ex) when (_options.ContinueOnError)
            {
                log.LogTeamError(ex, teamModel);
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