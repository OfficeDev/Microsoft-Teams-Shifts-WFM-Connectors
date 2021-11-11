// ---------------------------------------------------------------------------
// <copyright file="SwapRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Localization;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Services;

    public abstract class SwapRequestHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate SwapRequestUriTemplate = new UriTemplate("/swapRequests/{id}");

        protected readonly ICacheService _cacheService;
        protected readonly FeatureOptions _featureOptions;
        protected readonly IScheduleCacheService _scheduleCacheService;
        protected readonly TeamOrchestratorOptions _teamOptions;
        protected readonly IWfmActionService _wfmActionService;

        protected SwapRequestHandler(TeamOrchestratorOptions teamOptions, FeatureOptions featureOptions, IScheduleConnectorService scheduleConnectorService, IScheduleCacheService scheduleCacheService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _teamOptions = teamOptions ?? throw new ArgumentNullException(nameof(teamOptions));
            _featureOptions = featureOptions ?? throw new ArgumentNullException(nameof(featureOptions));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmActionService = wfmActionService ?? throw new ArgumentNullException(nameof(wfmActionService));
        }

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            return CanHandleChangeRequest(changeRequest, SwapRequestUriTemplate, out changeItemRequest);
        }

        protected static string GetSwapRequestCacheId(SwapRequest swapRequest)
        {
            return $"{swapRequest.SenderShiftId}_{swapRequest.RecipientShiftId}";
        }

        protected async Task DeleteChangeDataAsync(SwapRequest swapRequest)
        {
            var cacheId = GetSwapRequestCacheId(swapRequest);
            await _cacheService.DeleteKeyAsync(ApplicationConstants.TableNameChangeData, cacheId).ConfigureAwait(false);
        }

        protected async Task<ChangeData> ReadChangeDataAsync(SwapRequest swapRequest)
        {
            var cacheId = GetSwapRequestCacheId(swapRequest);
            var changeData = await _cacheService.GetKeyAsync<ChangeData>(ApplicationConstants.TableNameChangeData, cacheId).ConfigureAwait(false);
            if (changeData == null)
            {
                changeData = new ChangeData();
            }
            return changeData;
        }

        protected async Task SaveChangeDataAsync(SwapRequest swapRequest, ChangeData changeData)
        {
            var cacheId = GetSwapRequestCacheId(swapRequest);
            await _cacheService.SetKeyAsync(ApplicationConstants.TableNameChangeData, cacheId, changeData, true).ConfigureAwait(false);
        }

        protected abstract Task SaveChangeResultAsync(SwapRequest swapRequest, HttpStatusCode statusCode = HttpStatusCode.OK, string errorCode = "", string errorMessage = "");

        protected async Task<ChangeData> WaitAndReadChangeDataAsync(SwapRequest swapRequest)
        {
            // wait for the configured number of seconds
            await Task.Delay(_teamOptions.ChangeRequestWaitSeconds * 1000).ConfigureAwait(false);
            return await ReadChangeDataAsync(swapRequest).ConfigureAwait(false);
        }
    }
}
