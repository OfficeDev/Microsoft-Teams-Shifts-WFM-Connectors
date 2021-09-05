// ---------------------------------------------------------------------------
// <copyright file="SubscribeTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Collections.Generic;
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
    using WfmTeams.Adapter.Http;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class SubscribeTrigger
    {
        private readonly FeatureOptions _featureOptions;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly MicrosoftGraphOptions _options;
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly ITeamsService _teamsService;
        private readonly IWfmDataService _wfmDataService;
        private readonly ISecretsService _secretsService;
        private readonly ISystemTimeService _systemTimeService;
        private readonly TeamOrchestratorOptions _teamOrchestratorOptions;
        private readonly ITimeZoneService _timeZoneService;

        public SubscribeTrigger(TeamOrchestratorOptions teamOrchestratorOptions, MicrosoftGraphOptions options,
            FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IWfmDataService wfmDataService,
            ITeamsService teamsService, ISecretsService secretsService, IHttpClientFactory httpClientFactory,
            ISystemTimeService systemTimeService, ITimeZoneService timeZoneService)
        {
            _teamOrchestratorOptions = teamOrchestratorOptions ?? throw new ArgumentNullException(nameof(teamOrchestratorOptions));
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
            _teamsService = teamsService ?? throw new ArgumentNullException(nameof(teamsService));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _systemTimeService = systemTimeService ?? throw new ArgumentNullException(nameof(systemTimeService));
            _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        }

        [FunctionName(nameof(SubscribeTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] SubscribeModel subscribeModel,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // validate model
            if (!subscribeModel.IsValid())
            {
                log.LogError("Validating model failed.");
                return new BadRequestResult();
            }

            // validate with WFM Provider
            BusinessUnitModel businessUnit;
            try
            {
                businessUnit = await _wfmDataService.GetBusinessUnitAsync(subscribeModel.WfmBuId, log).ConfigureAwait(false);
            }
            catch (ArgumentException e)
            {
                log.LogError(e, "Subscribe failed - business unit id invalid.");
                return new BadRequestResult();
            }
            catch (KeyNotFoundException e)
            {
                log.LogError(e, "Subscribe failed - business unit not found.");
                return new NotFoundResult();
            }
            catch (UnauthorizedAccessException e)
            {
                log.LogError(e, "Subscribe failed - invalid credentials.");
                return new UnauthorizedResult();
            }

            // get the team from Teams
            GroupModel team;
            try
            {
                team = await _teamsService.GetTeamAsync(subscribeModel.TeamId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.LogError(e, "Subscribe failed - Not authorized to access details for the team.");
                return new ForbidResult();
            }

            var teamModel = subscribeModel.AsTeamModel();
            teamModel.TimeZoneInfoId = businessUnit.TimeZoneInfoId;

            var connectionModel = subscribeModel.AsConnectionModel();
            connectionModel.TimeZoneInfoId = businessUnit.TimeZoneInfoId;
            connectionModel.WfmBuName = businessUnit.WfmBuName;
            connectionModel.TeamName = team.Name;
            connectionModel.Enabled = true;

            try
            {
                // ensure that if the team is re-subscribing, that they haven't changed the business
                // unit that they are connecting to
                var existingModel = await _scheduleConnectorService.GetConnectionAsync(subscribeModel.TeamId).ConfigureAwait(false);
                if (connectionModel.WfmBuId != existingModel.WfmBuId)
                {
                    log.LogError("Re-subscribe failed - WFM business unit id changed.");
                    return new BadRequestResult();
                }
            }
            catch
            {
                // as this is a new subscription, we need to initialize the team with a new schedule
                await starter.StartNewAsync(nameof(InitializeOrchestrator), teamModel).ConfigureAwait(false);
                // and delay the first execution of the sync orchestrators by at least 5 minutes
                DelayOrchestratorsFirstExecution(connectionModel, 5);
            }

            // save connection settings
            await _scheduleConnectorService.SaveConnectionAsync(connectionModel).ConfigureAwait(false);

            log.LogSubscribeTeam(connectionModel);

            return new OkObjectResult(new BusinessUnitModel
            {
                WfmBuId = connectionModel.WfmBuId,
                WfmBuName = connectionModel.WfmBuName
            });
        }

        private void DelayOrchestratorsFirstExecution(ConnectionModel connectionModel, int delayMinutes)
        {
            connectionModel.LastSOExecution = _teamOrchestratorOptions.ShiftsFrequencyMinutes >= delayMinutes ? _systemTimeService.UtcNow.AddMinutes(-(_teamOrchestratorOptions.ShiftsFrequencyMinutes - delayMinutes)) : _systemTimeService.UtcNow.AddMinutes(delayMinutes - _teamOrchestratorOptions.ShiftsFrequencyMinutes);

            if (_featureOptions.EnableAvailabilitySync)
            {
                connectionModel.LastAOExecution = _teamOrchestratorOptions.AvailabilityFrequencyMinutes >= delayMinutes ? _systemTimeService.UtcNow.AddMinutes(-(_teamOrchestratorOptions.AvailabilityFrequencyMinutes - delayMinutes)) : _systemTimeService.UtcNow.AddMinutes(delayMinutes - _teamOrchestratorOptions.AvailabilityFrequencyMinutes);
            }

            if (_featureOptions.EnableOpenShiftSync)
            {
                connectionModel.LastOSOExecution = _teamOrchestratorOptions.OpenShiftsFrequencyMinutes >= delayMinutes ? _systemTimeService.UtcNow.AddMinutes(-(_teamOrchestratorOptions.OpenShiftsFrequencyMinutes - delayMinutes)) : _systemTimeService.UtcNow.AddMinutes(delayMinutes - _teamOrchestratorOptions.OpenShiftsFrequencyMinutes);
            }

            if (_featureOptions.EnableTimeOffSync)
            {
                connectionModel.LastTOOExecution = _teamOrchestratorOptions.TimeOffFrequencyMinutes >= delayMinutes ? _systemTimeService.UtcNow.AddMinutes(-(_teamOrchestratorOptions.TimeOffFrequencyMinutes - delayMinutes)) : _systemTimeService.UtcNow.AddMinutes(delayMinutes - _teamOrchestratorOptions.TimeOffFrequencyMinutes);
            }
        }
    }
}
