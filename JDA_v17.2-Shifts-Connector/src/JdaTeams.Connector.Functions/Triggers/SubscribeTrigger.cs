using JdaTeams.Connector.Extensions;
using JdaTeams.Connector.Functions.Extensions;
using JdaTeams.Connector.Functions.Helpers;
using JdaTeams.Connector.Functions.Models;
using JdaTeams.Connector.Functions.Orchestrators;
using JdaTeams.Connector.Http;
using JdaTeams.Connector.MicrosoftGraph.Extensions;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Options;
using JdaTeams.Connector.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Web.Http;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class SubscribeTrigger
    {
        private readonly IScheduleConnectorService _scheduleConnectorService;
        private readonly IScheduleSourceService _scheduleSourceService;
        private readonly IScheduleDestinationService _scheduleDestinationService;
        private readonly ISecretsService _secretsService;
        private readonly MicrosoftGraphOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ITimeZoneService _timeZoneService;

        public SubscribeTrigger(MicrosoftGraphOptions options, IScheduleConnectorService scheduleConnectorService, IScheduleSourceService scheduleSourceService, IScheduleDestinationService scheduleDestinationService, ISecretsService secretsService, IHttpClientFactory httpClientFactory, ITimeZoneService timeZoneService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _scheduleSourceService = scheduleSourceService ?? throw new ArgumentNullException(nameof(scheduleSourceService));
            _scheduleDestinationService = scheduleDestinationService ?? throw new ArgumentNullException(nameof(scheduleDestinationService));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _timeZoneService = timeZoneService ?? throw new ArgumentNullException(nameof(timeZoneService));
        }

        [FunctionName(nameof(SubscribeTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "subscribe")] SubscribeModel subscribeModel,
            [OrchestrationClient] DurableOrchestrationClient starter,
            ILogger log)
        {
            // validate model
            if (!subscribeModel.IsValid())
            {
                log.LogError("Validating model failed.");
                return new BadRequestResult();
            }

            // validate with JDA
            var credentials = subscribeModel.AsCredentialsModel();

            _scheduleSourceService.SetCredentials(subscribeModel.TeamId, credentials);

            StoreModel store;
            try
            {
                store = await _scheduleSourceService.GetStoreAsync(subscribeModel.TeamId, subscribeModel.StoreId).ConfigureAwait(false);
            }
            catch (ArgumentException e)
            {
                log.LogError(e, "Subscribe failed - JDA store id incorrect.");
                return new BadRequestResult();
            }
            catch (KeyNotFoundException e)
            {
                log.LogError(e, "Subscribe failed - JDA store not found.");
                return new NotFoundResult();
            }
            catch (UnauthorizedAccessException e)
            {
                log.LogError(e, "Subscribe failed - Invalid url or credentials.");
                return new UnauthorizedResult();
            }

            // ensure that we can map the timezone for the store
            var timeZoneInfoId = await TimeZoneHelper.GetTimeZoneAsync(subscribeModel.TeamId, store.TimeZoneId, _timeZoneService, _scheduleSourceService, _scheduleConnectorService, log).ConfigureAwait(false);
            if (timeZoneInfoId == null)
            {
                log.LogError($"Subscribe failed - No time zone mapping found for store TimeZoneId={store.TimeZoneId}.");
                return new InternalServerErrorResult();
            }

            // exchange and save access token
            if (!string.IsNullOrEmpty(subscribeModel.AuthorizationCode))
            {
                var tokenResponse = await _httpClientFactory.Client.RequestTokenAsync(_options, subscribeModel.RedirectUri, subscribeModel.AuthorizationCode).ConfigureAwait(false);

                if (tokenResponse.IsError)
                {
                    log.LogError("Subscribe failed - Invalid authorization code.");
                    return new ForbidResult();
                }

                var tokenModel = tokenResponse.AsTokenModel();

                await _secretsService.SaveTokenAsync(subscribeModel.TeamId, tokenModel).ConfigureAwait(false);
            }
            else if (!string.IsNullOrEmpty(subscribeModel.AccessToken))
            {
                var tokenModel = subscribeModel.AsTokenModel();

                await _secretsService.SaveTokenAsync(subscribeModel.TeamId, tokenModel).ConfigureAwait(false);
            }

            // save JDA creds
            await _secretsService.SaveCredentialsAsync(subscribeModel.TeamId, credentials).ConfigureAwait(false);

            // get the team from Teams
            GroupModel team;
            try
            {
                team = await _scheduleDestinationService.GetTeamAsync(subscribeModel.TeamId).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                log.LogError(e, "Subscribe failed - Not authorized to access details for the team.");
                return new ForbidResult();
            }

            var teamModel = subscribeModel.AsTeamModel();
            teamModel.TimeZoneInfoId = timeZoneInfoId;

            var connectionModel = subscribeModel.AsConnectionModel();
            connectionModel.TimeZoneInfoId = timeZoneInfoId;
            connectionModel.StoreName = store.StoreName;
            connectionModel.TeamName = team.Name;

            try
            {
                // ensure that if the team is re-subscribing, that they haven't changed the store
                // that they are connecting to
                var existingModel = await _scheduleConnectorService.GetConnectionAsync(subscribeModel.TeamId).ConfigureAwait(false);
                if (connectionModel.StoreId != existingModel.StoreId)
                {
                    log.LogError("Re-subscribe failed - JDA store id changed.");
                    return new BadRequestResult();
                }
                else
                {
                    // as the team is re-subscribing, ensure that the schedule is not re-initialized
                    teamModel.Initialized = true;
                }
            }
            catch { /* nothing to do - new subscription */ }

            // save connection settings
            await _scheduleConnectorService.SaveConnectionAsync(connectionModel).ConfigureAwait(false);

            // start singleton team orchestrator
            await starter.TryStartSingletonAsync(nameof(TeamOrchestrator), teamModel.TeamId, teamModel).ConfigureAwait(false);

            return new OkObjectResult(new StoreModel
            {
                StoreId = connectionModel.StoreId,
                StoreName = connectionModel.StoreName
            });
        }
    }
}