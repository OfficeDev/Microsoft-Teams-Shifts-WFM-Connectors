// <copyright file="TimeOffRequestsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.ShiftsToKronos.CreateTimeOff;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using TimeOffRequestResponse = Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;

    /// <summary>
    /// Time Off Requests controller.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/TimeOffRequests")]
    [ApiController]
    public class TimeOffRequestsController : ControllerBase
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly ICreateTimeOffActivity createTimeOffActivity;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ITimeOffReasonProvider timeOffReasonProvider;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ITimeOffRequestProvider timeOffReqMappingEntityProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly Utility utility;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffRequestsController"/> class.
        /// </summary>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="userMappingProvider">User To User Mapping Provider.</param>
        /// <param name="createTimeOffActivity">Create time off activity.</param>
        /// <param name="timeOffReasonProvider">Paycodes to Time Off Reasons Mapping provider.</param>
        /// <param name="utility">The local Utility DI.</param>
        /// <param name="azureTableStorageHelper">The Azure table storage helper.</param>
        /// <param name="timeOffReqMappingEntityProvider">time off entity provider.</param>
        /// <param name="teamDepartmentMappingProvider">TeamDepartmentMapping provider DI.</param>
        /// <param name="httpClientFactory">http client.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public TimeOffRequestsController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            ICreateTimeOffActivity createTimeOffActivity,
            IUserMappingProvider userMappingProvider,
            ITimeOffReasonProvider timeOffReasonProvider,
            IAzureTableStorageHelper azureTableStorageHelper,
            ITimeOffRequestProvider timeOffReqMappingEntityProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            Utility utility,
            IHttpClientFactory httpClientFactory,
            BackgroundTaskWrapper taskWrapper)
        {
            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.createTimeOffActivity = createTimeOffActivity;
            this.userMappingProvider = userMappingProvider;
            this.timeOffReasonProvider = timeOffReasonProvider;
            this.azureTableStorageHelper = azureTableStorageHelper;
            this.timeOffReqMappingEntityProvider = timeOffReqMappingEntityProvider;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.utility = utility;
            this.httpClientFactory = httpClientFactory;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// start timeoffrequests sync from Kronos and push it to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">Checks if request is coming from logic app or portal.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task ProcessTimeOffRequestsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffRequetsAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)} for isRequestFromLogicApp: " + isRequestFromLogicApp);

            this.utility.SetQuerySpan(Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture), out string timeOffStartDate, out string timeOffEndDate);

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(timeOffStartDate, timeOffEndDate);

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                TimeOffRequestResponse.TimeOffRequestRes timeOffRequestContent = default(TimeOffRequestResponse.TimeOffRequestRes);

                // Get the mapped user details from user to user mapping table.
                var allUsers = await UsersHelper.GetAllMappedUserDetailsAsync(allRequiredConfigurations.WFIId, this.userMappingProvider, this.teamDepartmentMappingProvider, this.telemetryClient).ConfigureAwait(false);

                // Get distinct Teams.
                var allteamDetails = allUsers?.Select(x => x.ShiftTeamId).Distinct().ToList();

                // Get list of time off reasons from pay code to time off reason mapping table
                var timeOffReasons = await this.timeOffReasonProvider.GetTimeOffReasonsAsync().ConfigureAwait(false);

                var monthPartitions = Utility.GetMonthPartition(timeOffStartDate, timeOffEndDate);

                List<TimeOffRequestResponse.TimeOffRequestItem> timeOffRequestItems = new List<TimeOffRequestResponse.TimeOffRequestItem>();
                bool hasMoreTimeOffs = false;

                if (monthPartitions != null && monthPartitions.Count > 0)
                {
                    foreach (var monthPartitionKey in monthPartitions)
                    {
                        timeOffRequestItems.Clear();
                        hasMoreTimeOffs = false;
                        string queryStartDate, queryEndDate;
                        Utility.GetNextDateSpan(
                            monthPartitionKey,
                            monthPartitions.FirstOrDefault(),
                            monthPartitions.LastOrDefault(),
                            timeOffStartDate,
                            timeOffEndDate,
                            out queryStartDate,
                            out queryEndDate);

                        foreach (var team in allteamDetails)
                        {
                            timeOffRequestItems.Clear();
                            hasMoreTimeOffs = false;
                            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                            Uri requestUri = new Uri(this.appSettings.GraphApiUrl + "teams/" + team + "/schedule/timeoffrequests?$filter=state eq 'pending'");
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);
                            do
                            {
                                using (var httpRequestMessage = new HttpRequestMessage()
                                {
                                    Method = HttpMethod.Get,
                                    RequestUri = requestUri,
                                })
                                {
                                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                                    if (response.IsSuccessStatusCode)
                                    {
                                        timeOffRequestContent = await response.Content.ReadAsAsync<TimeOffRequestResponse.TimeOffRequestRes>().ConfigureAwait(false);
                                        timeOffRequestItems.AddRange(timeOffRequestContent.TORItem);
                                        if (timeOffRequestContent.NextLink != null)
                                        {
                                            hasMoreTimeOffs = true;
                                            requestUri = timeOffRequestContent.NextLink;
                                        }
                                        else
                                        {
                                            hasMoreTimeOffs = false;
                                        }
                                    }
                                    else
                                    {
                                        this.telemetryClient.TrackTrace("SyncTimeOffRequestsFromShiftsToKronos - " + response.StatusCode.ToString());
                                    }
                                }
                            }
                            while (hasMoreTimeOffs);

                            if (timeOffRequestItems?.Count > 0)
                            {
                                // get the team mappings for the team and pick the first because we need the Kronos Time Zone
                                var mappedTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsAsync(team).ConfigureAwait(false);
                                var mappedTeam = mappedTeams.FirstOrDefault();
                                var kronosTimeZone = string.IsNullOrEmpty(mappedTeam?.KronosTimeZone) ? this.appSettings.KronosTimeZone : mappedTeam.KronosTimeZone;

                                foreach (var item in timeOffRequestItems)
                                {
                                    var timeOffReqStartDate = this.utility.UTCToKronosTimeZone(item.StartDateTime, kronosTimeZone);
                                    if (timeOffReqStartDate < DateTime.ParseExact(queryStartDate, Common.Constants.DateFormat, CultureInfo.InvariantCulture)
                                        || timeOffReqStartDate > DateTime.ParseExact(queryEndDate, Common.Constants.DateFormat, CultureInfo.InvariantCulture).AddDays(1))
                                    {
                                        continue;
                                    }

                                    List<TimeOffMappingEntity> timeOffMappingEntity = await this.timeOffReqMappingEntityProvider.GetAllTimeOffReqMappingEntitiesAsync(
                                        monthPartitionKey,
                                        item.Id).ConfigureAwait(false);

                                    if (timeOffMappingEntity.Count == 0)
                                    {
                                        var timeOffReason = timeOffReasons.Find(t => t.TimeOffReasonId == item.TimeOffReasonId);

                                        var personDetails = allUsers.FirstOrDefault(u => u.ShiftUserId == Convert.ToString(item.SenderUserId, CultureInfo.InvariantCulture));

                                        // Get the Kronos WFC API Time Zone Info
                                        var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZone);

                                        // Create the Kronos Time Off Request.
                                        var timeOffResponse = await this.createTimeOffActivity.TimeOffRequestAsync(
                                            allRequiredConfigurations.KronosSession,
                                            item.StartDateTime,
                                            item.EndDateTime,
                                            personDetails?.KronosPersonNumber,
                                            timeOffReason?.RowKey,
                                            new Uri(allRequiredConfigurations.WfmEndPoint),
                                            kronosTimeZoneInfo).ConfigureAwait(false);

                                        // If there is an error from Kronos side.
                                        if (string.IsNullOrWhiteSpace(timeOffResponse?.Error?.Message))
                                        {
                                            var submitTimeOffResponse = await this.createTimeOffActivity.SubmitTimeOffRequestAsync(
                                                allRequiredConfigurations.KronosSession,
                                                personDetails.KronosPersonNumber,
                                                timeOffResponse?.EmployeeRequestMgm?.RequestItem?.GlobalTimeOffRequestItms?.FirstOrDefault()?.Id,
                                                queryStartDate,
                                                queryEndDate,
                                                new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

                                            TimeOffMappingEntity newTimeOffReq = new TimeOffMappingEntity();
                                            if (submitTimeOffResponse?.Status == ApiConstants.Failure)
                                            {
                                                newTimeOffReq.IsActive = false;
                                            }
                                            else
                                            {
                                                newTimeOffReq.IsActive = true;
                                            }

                                            newTimeOffReq.Duration = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().TimeOffPeriodsList.TimeOffPerd.FirstOrDefault().Duration;
                                            newTimeOffReq.EndDate = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().TimeOffPeriodsList.TimeOffPerd.FirstOrDefault().EndDate;
                                            newTimeOffReq.StartDate = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().TimeOffPeriodsList.TimeOffPerd.FirstOrDefault().StartDate;
                                            newTimeOffReq.StartTime = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().TimeOffPeriodsList.TimeOffPerd.FirstOrDefault().StartTime;
                                            newTimeOffReq.PayCodeName = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().TimeOffPeriodsList.TimeOffPerd.FirstOrDefault().PayCodeName;
                                            newTimeOffReq.KronosPersonNumber = timeOffResponse.EmployeeRequestMgm.Employees.PersonIdentity.PersonNumber;
                                            newTimeOffReq.PartitionKey = monthPartitionKey;
                                            newTimeOffReq.RowKey = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().Id;
                                            newTimeOffReq.ShiftsRequestId = item.Id;
                                            newTimeOffReq.KronosRequestId = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().Id;
                                            newTimeOffReq.StatusName = ApiConstants.SubmitRequests;

                                            this.AddorUpdateTimeOffMappingAsync(newTimeOffReq);
                                        }
                                        else
                                        {
                                            this.telemetryClient.TrackTrace(timeOffResponse.Error.DetailErrors.Error[0].Message);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace("SyncTimeOffRequestsFromShifsToKronos - " + Resource.NullMonthPartitionsMessage);
                }
            }
            else
            {
                this.telemetryClient.TrackTrace("SyncTimeOffRequestsFromShiftsToKronos - " + Resource.SetUpNotDoneMessage);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffRequetsAsync} ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// Method to either add or update the Time Off Entity Mapping.
        /// </summary>
        /// <param name="timeOffMappingEntity">A type of <see cref="TimeOffMappingEntity"/>.</param>
        private async void AddorUpdateTimeOffMappingAsync(TimeOffMappingEntity timeOffMappingEntity)
        {
            await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(timeOffMappingEntity, "TimeOffMapping").ConfigureAwait(false);
        }
    }
}