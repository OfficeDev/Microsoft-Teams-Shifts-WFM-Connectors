// ---------------------------------------------------------------------------
// <copyright file="ConnectorHealthTimerTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.MicrosoftGraph.Exceptions;
    using WfmTeams.Adapter.Services;

    public class ConnectorHealthTimerTrigger
    {
        private readonly ConnectorHealthOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly ITeamsService _teamsService;

        public ConnectorHealthTimerTrigger(ConnectorHealthOptions options, IScheduleConnectorService scheduleConnectorService, ITeamsService teamsService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
        }

        [FunctionName(nameof(ConnectorHealthTimerTrigger))]
        public async Task Run([TimerTrigger("%ConnectorHealthScheduleExpression%")] TimerInfo timerInfo,
            ILogger log)
        {
            try
            {
                await CheckConnectedTeams(log).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Unexpected error in {triggerName}", nameof(ConnectorHealthTimerTrigger));
            }
        }

        private async Task CheckConnectedTeams(ILogger log)
        {
            // get the list of connected teams from Storage
            var connections = await _scheduleConnectorService.ListConnectionsAsync().ConfigureAwait(false);
            foreach (var connection in connections)
            {
                // ensure that the team still exists in Teams by attempting to fetch it
                try
                {
                    var response = await _teamsService.GetTeamAsync(connection.TeamId).ConfigureAwait(false);
                }
                catch (MicrosoftGraphException ex)
                {
                    if (ex.Error.Code.Equals(_options.MissingTeamErrorCode, StringComparison.OrdinalIgnoreCase))
                    {
                        log.LogMissingTeam(connection);
                    }
                }
            }
        }
    }
}
