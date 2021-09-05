// ---------------------------------------------------------------------------
// <copyright file="ShiftSwapFilterHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Handlers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs.Extensions.DurableTask;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using Tavis.UriTemplates;
    using WfmTeams.Adapter.Exceptions;
    using WfmTeams.Adapter.Extensions;
    using WfmTeams.Adapter.Functions.ChangeRequests;
    using WfmTeams.Adapter.Functions.Extensions;
    using WfmTeams.Adapter.Functions.Options;
    using WfmTeams.Adapter.Functions.Triggers;
    using WfmTeams.Adapter.MicrosoftGraph.Models;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class ShiftSwapFilterHandler : ChangeRequestHandler
    {
        public static readonly UriTemplate ShiftSwapFilterRequestUriTemplate = new UriTemplate("/shifts/{id}/requestableShifts{?requestType,startTime,endTime}");

        protected readonly IScheduleCacheService _scheduleCacheService;
        protected readonly TeamOrchestratorOptions _teamOptions;
        private readonly ICacheService _cacheService;
        private readonly IWfmDataService _wfmDataService;

        public ShiftSwapFilterHandler(TeamOrchestratorOptions teamOptions, IScheduleConnectorService scheduleConnectorService, IRequestCacheService requestCacheService, ISecretsService secretsService, IStringLocalizer<ChangeRequestTrigger> stringLocalizer, IScheduleCacheService scheduleCacheService, ICacheService cacheService, IWfmDataService wfmDataService)
            : base(scheduleConnectorService, requestCacheService, secretsService, stringLocalizer)
        {
            _teamOptions = teamOptions ?? throw new ArgumentNullException(nameof(teamOptions));
            _scheduleCacheService = scheduleCacheService ?? throw new ArgumentNullException(nameof(scheduleCacheService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
            _wfmDataService = wfmDataService ?? throw new ArgumentNullException(nameof(wfmDataService));
        }

        public override HandlerType ChangeHandlerType => HandlerType.EligibilityFilter;

        public override bool CanHandleChangeRequest(ChangeRequest changeRequest, out ChangeItemRequest changeItemRequest)
        {
            // we cannot use the base class method here as we need to handle the invalid url's that
            // Teams is sending with this request
            changeItemRequest = null;
            if (changeRequest.Requests.Length == 1)
            {
                // the url as supplied by Teams looks something like the following:
                //"/shifts/SHFT_bff7961c-1bff-4c39-b2b1-7808eca233de/requestableShifts?requestType=SwapRequest&startTime=5/31/2020 11:00:00 PM +00:00&endTime=6/30/2020 10:59:59 PM +00:00"
                // because the datetime values are not valid in a url we are having to strip them so that the url parser
                // does not fail in the TryMatch method
                var request = changeRequest.Requests[0];
                var url = request.Url.Substring(0, request.Url.IndexOf("&"));
                if (ShiftSwapFilterRequestUriTemplate.TryMatch(url, out var changeItemParams))
                {
                    if (changeItemParams.ContainsKey("requestType") && changeItemParams["requestType"].ToString().Equals("SwapRequest", StringComparison.OrdinalIgnoreCase))
                    {
                        changeItemRequest = request;
                    }
                }
            }

            return changeItemRequest != null;
        }

        public override async Task<IActionResult> HandleRequest(ChangeRequest changeRequest, ChangeItemRequest changeItemRequest, ChangeResponse changeResponse, string entityId, ILogger log, IDurableOrchestrationClient starter)
        {
            var connectionModel = await _scheduleConnectorService.GetConnectionAsync(entityId).ConfigureAwait(false);

            var loadScheduleTasks = DateTime.UtcNow
                .Range(0, _teamOptions.FutureWeeks, _teamOptions.StartDayOfWeek) // pastWeek = 0 as we don't swap shifts from previous weeks
                .Select(w => _scheduleCacheService.LoadScheduleAsync(entityId, w));

            var cacheModels = await Task.WhenAll(loadScheduleTasks).ConfigureAwait(false);

            var fromShift = cacheModels.SelectMany(c => c.Tracked).FirstOrDefault(s => s.TeamsShiftId == changeItemRequest.Id);
            if (fromShift == null)
            {
                return new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.SenderShiftNotFound, _stringLocalizer[ErrorCodes.SenderShiftNotFound], true);
            }

            var user = await _cacheService.GetKeyAsync<EmployeeModel>(ApplicationConstants.TableNameEmployees, fromShift.WfmEmployeeId).ConfigureAwait(false);
            if (user == null)
            {
                return new ChangeErrorResult(changeResponse, changeItemRequest, ErrorCodes.UserCredentialsNotFound, _stringLocalizer[ErrorCodes.UserCredentialsNotFound], true);
            }

            try
            {
                var wfmMatches = await _wfmDataService.GetEligibleTargetsForShiftSwap(fromShift, user, connectionModel.WfmBuId).ConfigureAwait(false);

                var matches = new List<string>();

                foreach (var wfmShiftId in wfmMatches)
                {
                    var swappableTeamsShift = cacheModels.SelectMany(c => c.Tracked).FirstOrDefault(s => s.WfmShiftId == wfmShiftId);
                    if (swappableTeamsShift != null)
                    {
                        matches.Add(swappableTeamsShift.TeamsShiftId);
                    }
                }

                return new ChangeSuccessResult(changeResponse, changeItemRequest, matches);
            }
            catch (WfmException wex)
            {
                return WfmErrorToActionResult(wex.Error, changeItemRequest, changeResponse);
            }
        }
    }
}
