// ---------------------------------------------------------------------------
// <copyright file="ShiftPreferenceChangeRequestHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Mappings;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ShiftPreferenceChangeRequestHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate ShiftPreferenceRequestUriTemplate = new UriTemplate("/settings/shiftpreferences");

        private readonly IAvailabilityMap _availabilityMap;
        private readonly ICacheService _cacheService;
        private readonly IWfmActionService _wfmActionService;

        public ShiftPreferenceChangeRequestHandler(IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, ICacheService cacheService, IWfmActionService wfmActionService, IAvailabilityMap availabilityMap)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmActionService = wfmActionService ?? throw new ArgumentNullException(nameof(wfmActionService));
            _availabilityMap = availabilityMap ?? throw new ArgumentNullException(nameof(availabilityMap));
        }

        public override HandlerType ChangeHandlerType => HandlerType.Users;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            changeItemRequest = null;
            if (changeRequest.Requests.Length == 1 && ShiftPreferenceRequestUriTemplate.TryMatch(changeRequest.Requests[0].Url, out _))
            {
                changeItemRequest = changeRequest.Requests[0];
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string userId, ILogger log, IDurableOrchestrationClient starter)
        {
            var preferenceRequest = changeItemRequest.Body.ToObject<ShiftPreferenceResponse>();
            if (preferenceRequest == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.ChangeRequestNotFound, _stringLocalizer[ErrorCodes.ChangeRequestNotFound]);
            }

            var employee = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, userId).ConfigureAwait(false);
            if (employee == null)
            {
                return new ChangeErrorResult(changeResponse, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound]);
            }

            // get the conection object which contains TimeZoneInfoId to populate below availabilityModel.
            var connection = await _scheduleConnectorService.GetConnectionAsync(employee.TeamIds[0]).ConfigureAwait(false);

            // map the preference request to an availability model
            var availabilityModel = _availabilityMap.MapAvailability(preferenceRequest.Availability, userId);
            availabilityModel.WfmEmployeeId = employee.WfmEmployeeId;
            availabilityModel.TimeZoneInfoId = connection.TimeZoneInfoId;

            var wfmResponse = await _wfmActionService.UpdateEmployeeAvailabilityAsync(availabilityModel, log).ConfigureAwait(false);

            return WfmResponseToActionResult(wfmResponse, changeItemRequest, changeResponse);
        }
    }
}
