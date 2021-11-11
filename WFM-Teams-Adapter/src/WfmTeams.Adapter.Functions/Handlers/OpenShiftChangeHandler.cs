// ---------------------------------------------------------------------------
// <copyright file="OpenShiftChangeHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Services;

    public class OpenShiftChangeHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate OpenShiftChangeUriTemplate = new UriTemplate("/openshifts/{id}");

        public OpenShiftChangeHandler(IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
        }

        public override HandlerType ChangeHandlerType => HandlerType.Teams;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            return CanHandleChangeRequest(changeRequest, OpenShiftChangeUriTemplate, out changeItemRequest) && changeRequest.Requests.Length == 1;
        }

        public override Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string teamId, ILogger log, IDurableOrchestrationClient starter)
        {
            return Task.FromResult<IActionResult>(new ChangeErrorResult(changeResponse, ErrorCodes.UnsupportedOperation, _stringLocalizer[ErrorCodes.UnsupportedOperation], HttpStatusCode.Forbidden));
        }
    }
}
