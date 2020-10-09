using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Options;
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
        private readonly ITimeZoneHelper _timeZoneHelper;

        public TeamOrchestrator(TeamOrchestratorOptions options, ITimeZoneHelper timeZoneHelper)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeZoneHelper = timeZoneHelper ?? throw new ArgumentNullException(nameof(timeZoneHelper));
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

                var weekTasks = weeks
                    .Select(async startDate => new WeekModel
                    {
                        StartDate = startDate,
                        StoreId = teamModel.StoreId,
                        TeamId = teamModel.TeamId,
                        TimeZoneInfoId = await _timeZoneHelper.GetAndUpdateTimeZone(teamModel.TeamId)
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