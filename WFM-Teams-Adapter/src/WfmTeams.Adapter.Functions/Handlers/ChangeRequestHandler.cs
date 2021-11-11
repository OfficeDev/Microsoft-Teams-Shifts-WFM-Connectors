// ---------------------------------------------------------------------------
// <copyright file="ChangeRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Microsoft.WindowsAzure.Storage;
    using Polly;
    using Polly.Retry;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public enum HandlerType
    {
        Teams,
        Users,
        EligibilityFilter
    }

    public abstract class ChangeRequestHandler : IChangeRequestHandler
    {
        protected readonly IRequestCacheService _requestCacheService;
        protected readonly IScheduleConnectorService _scheduleConnectorService;
        protected readonly ISecretsService _secretsService;
        protected readonly IStringLocalizer<ChangeRequestTrigger> _stringLocalizer;

        protected ChangeRequestHandler(IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer)
        {
            _scheduleConnectorService = scheduleConnectorService ?? throw new ArgumentNullException(nameof(scheduleConnectorService));
            _requestCacheService = requestCacheService ?? throw new ArgumentNullException(nameof(requestCacheService));
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
            _stringLocalizer = stringLocalizer ?? throw new ArgumentNullException(nameof(stringLocalizer));
        }

        public abstract HandlerType ChangeHandlerType { get; }

        public abstract bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest);

        public abstract Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string entityId, ILogger log, IDurableOrchestrationClient starter);

        protected static bool CanHandleChangeRequest(ChangeRequest changeRequest, UriTemplate uriTemplate, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            foreach (var request in changeRequest.Requests)
            {
                if (uriTemplate.TryMatch(request.Url, out var changeItemParams))
                {
                    changeItemRequest = request;
                    break;
                }
            }

            return changeItemRequest != null;
        }

        protected static AsyncRetryPolicy GetConflictRetryPolicy(int retryCount, int retryInterval)
        {
            return Policy
                .Handle<StorageException>(h => h.RequestInformation?.HttpStatusCode == (int)HttpStatusCode.Conflict)
                .WaitAndRetryAsync(retryCount, retryAttempt => TimeSpan.FromSeconds(retryInterval));
        }

        protected static string GetTargetLoginName(string loginName)
        {
            int pos = loginName.IndexOf("@");
            if (pos > 0)
            {
                return loginName.Substring(0, pos);
            }

            return loginName;
        }

        protected async Task<T> ReadRequestObjectAsync<T>(ChangeItemRequest changeItemRequest, string teamId) where T : IHandledRequest
        {
            try
            {
                if (changeItemRequest.Body != null)
                {
                    var request = changeItemRequest.Body.ToObject<T>();
                    var cachedRequest = await _requestCacheService.LoadRequestAsync<T>(teamId, changeItemRequest.Id).ConfigureAwait(false);
                    if (cachedRequest != null)
                    {
                        request.FillTargetIds(cachedRequest);
                    }
                    return request;
                }
                else
                {
                    return await _requestCacheService.LoadRequestAsync<T>(teamId, changeItemRequest.Id).ConfigureAwait(false);
                }
            }
            catch
            {
                return default;
            }
        }

        protected static IActionResult WfmResponseToActionResult(WfmResponse wfmResponse, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse)
        {
            if (wfmResponse.Success)
            {
                return new ChangeSuccessResult(changeResponse, changeItemRequest);
            }

            return WfmErrorToActionResult(wfmResponse.Error, changeItemRequest, changeResponse);
        }

        protected static IActionResult WfmErrorToActionResult(WfmError wfmError, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse)
        {
            return new ChangeErrorResult(changeResponse, changeItemRequest, wfmError.Code, wfmError.Message, HttpStatusCode.FailedDependency);
        }
    }
}
