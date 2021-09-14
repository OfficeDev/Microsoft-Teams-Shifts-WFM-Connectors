// <copyright file="TimeOffController.cs" company="Microsoft">
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
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Graph;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Newtonsoft.Json;
    using TimeOffReq = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;

    /// <summary>
    /// Time off controller.
    /// </summary>
    [Route("api/TimeOff")]
    [Authorize(Policy = "AppID")]
    public class TimeOffController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ITimeOffActivity timeOffActivity;
        private readonly ITimeOffReasonProvider timeOffReasonProvider;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly ITimeOffMappingEntityProvider timeOffMappingEntityProvider;
        private readonly Utility utility;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffController"/> class.
        /// </summary>
        /// <param name="appSettings">Application Settings DI.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="userMappingProvider">The User Mapping Provider DI.</param>
        /// <param name="timeOffActivity">Time Off Activity DI.</param>
        /// <param name="timeOffReasonProvider">Time Off Reason Provider DI.</param>
        /// <param name="azureTableStorageHelper">Azure Storage Helper DI.</param>
        /// <param name="timeOffMappingEntityProvider">Time Off Mapping Provider DI.</param>
        /// <param name="utility">Utility DI.</param>
        /// <param name="teamDepartmentMappingProvider">Team Department Mapping Provider DI.</param>
        /// <param name="httpClientFactory">HttpClientFactory DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public TimeOffController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IUserMappingProvider userMappingProvider,
            ITimeOffActivity timeOffActivity,
            ITimeOffReasonProvider timeOffReasonProvider,
            IAzureTableStorageHelper azureTableStorageHelper,
            ITimeOffMappingEntityProvider timeOffMappingEntityProvider,
            Utility utility,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IHttpClientFactory httpClientFactory,
            BackgroundTaskWrapper taskWrapper)
        {
            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.userMappingProvider = userMappingProvider;
            this.timeOffActivity = timeOffActivity;
            this.timeOffReasonProvider = timeOffReasonProvider;
            this.azureTableStorageHelper = azureTableStorageHelper;
            this.timeOffMappingEntityProvider = timeOffMappingEntityProvider ?? throw new ArgumentNullException(nameof(timeOffMappingEntityProvider));
            this.utility = utility;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.httpClientFactory = httpClientFactory;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Get the list of time off details from Kronos and push it to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">True if request is coming from logic app.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        internal async Task ProcessTimeOffsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffsAsync} started, isRequestFromLogicApp: {isRequestFromLogicApp}");

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            var timeOffReasons = await this.timeOffReasonProvider.GetTimeOffReasonsAsync().ConfigureAwait(false);

            this.utility.SetQuerySpan(Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture), out var timeOffStartDate, out var timeOffEndDate);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(timeOffStartDate, timeOffEndDate);

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                // Get the mapped user details from user to user mapping table.
                var allUsers = await UsersHelper.GetAllMappedUserDetailsAsync(allRequiredConfigurations.WFIId, this.userMappingProvider, this.teamDepartmentMappingProvider, this.telemetryClient).ConfigureAwait(false);

                var monthPartitions = Utility.GetMonthPartition(timeOffStartDate, timeOffEndDate);
                var processNumberOfUsersInBatch = this.appSettings.ProcessNumberOfUsersInBatch;
                var userCount = allUsers.Count();
                int userIteration = Utility.GetIterablesCount(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture), userCount);
                var graphClient = await this.CreateGraphClientWithDelegatedAccessAsync(allRequiredConfigurations.ShiftsAccessToken, allRequiredConfigurations.WFIId).ConfigureAwait(false);

                if (monthPartitions?.Count > 0)
                {
                    foreach (var monthPartitionKey in monthPartitions)
                    {
                        Utility.GetNextDateSpan(
                            monthPartitionKey,
                            monthPartitions.FirstOrDefault(),
                            monthPartitions.LastOrDefault(),
                            timeOffStartDate,
                            timeOffEndDate,
                            out var queryStartDate,
                            out var queryEndDate);

                        var processUsersInBatchList = new List<ResponseHyperFindResult>();

                        // TODO #1 - Add the code for checking the dates.
                        foreach (var item in allUsers?.ToList())
                        {
                            processUsersInBatchList.Add(new ResponseHyperFindResult
                            {
                                PersonNumber = item.KronosPersonNumber,
                            });
                        }

                        var processBatchUsersQueue = new Queue<ResponseHyperFindResult>(processUsersInBatchList);
                        var processKronosUsersQueue = new Queue<UserDetailsModel>(allUsers);

                        for (int batchedUserCount = 0; batchedUserCount < userIteration; batchedUserCount++)
                        {
                            var processKronosUsersQueueInBatch = processKronosUsersQueue?.Skip(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture) * batchedUserCount).Take(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture));
                            var processBatchUsersQueueInBatch = processBatchUsersQueue?.Skip(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture) * batchedUserCount).Take(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture));

                            var lookUpData = await this.timeOffMappingEntityProvider.GetAllTimeOffMappingEntitiesAsync(
                                processKronosUsersQueueInBatch,
                                monthPartitionKey).ConfigureAwait(false);

                            var timeOffDetails = await this.GetTimeOffResultsByBatchAsync(
                                processBatchUsersQueueInBatch.ToList(),
                                allRequiredConfigurations.WfmEndPoint,
                                allRequiredConfigurations.WFIId,
                                allRequiredConfigurations.KronosSession,
                                queryStartDate,
                                queryEndDate).ConfigureAwait(false);

                            if (timeOffDetails?.RequestMgmt?.RequestItems?.GlobalTimeOffRequestItem is null)
                            {
                                continue;
                            }

                            await this.ProcessTimeOffEntitiesBatchAsync(
                                allRequiredConfigurations,
                                processKronosUsersQueueInBatch,
                                lookUpData,
                                timeOffDetails?.RequestMgmt?.RequestItems?.GlobalTimeOffRequestItem,
                                timeOffReasons,
                                graphClient,
                                monthPartitionKey).ConfigureAwait(false);
                        }
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace("SyncTimeOffsFromKronos - " + Resource.NullMonthPartitionsMessage);
                }
            }
            else
            {
                this.telemetryClient.TrackTrace("SyncTimeOffsFromKronos - " + Resource.SetUpNotDoneMessage);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffsAsync} ended; isRequestFromLogicApp: {isRequestFromLogicApp}");
        }

        /// <summary>
        /// Creates a time off request that was requested in Teams.
        /// </summary>
        /// <param name="user">The user details of the time off requestor.</param>
        /// <param name="timeOffEntity">The time off to be created.</param>
        /// <param name="timeOffReason">The time off reason.</param>
        /// <param name="allRequiredConfigurations">Setup details.</param>
        /// <param name="kronosTimeZone">The kronos timezone.</param>
        /// <returns>Whether the time off request was created successfully or not.</returns>
        internal async Task<bool> CreateTimeOffRequestInKronosAsync(
            UserDetailsModel user,
            TimeOffRequestItem timeOffEntity,
            PayCodeToTimeOffReasonsMappingEntity timeOffReason,
            SetupDetails allRequiredConfigurations,
            string kronosTimeZone)
        {
            // Teams provides date times in UTC so convert to the local time.
            var localStartDateTime = this.utility.UTCToKronosTimeZone(timeOffEntity.StartDateTime, kronosTimeZone);
            var localEndDateTime = this.utility.UTCToKronosTimeZone(timeOffEntity.EndDateTime, kronosTimeZone);

            // Construct the query date span for the Kronos request
            var queryStartDate = localStartDateTime.AddDays(
                                                -Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                                .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

            var queryEndDate = localEndDateTime.AddDays(
                               Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                               .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

            var timeOffReqQueryDateSpan = $"{queryStartDate}-{queryEndDate}";

            var comments = XmlHelper.GenerateKronosComments(timeOffEntity.SenderMessage, this.appSettings.SenderTimeOffRequestCommentText);

            // Create the Kronos Time Off Request.
            var timeOffResponse = await this.timeOffActivity.CreateTimeOffRequestAsync(
                allRequiredConfigurations.KronosSession,
                localStartDateTime,
                localEndDateTime,
                timeOffReqQueryDateSpan,
                user.KronosPersonNumber,
                timeOffReason.RowKey,
                comments,
                new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

            if (!string.IsNullOrWhiteSpace(timeOffResponse?.Error?.Message))
            {
                this.telemetryClient.TrackTrace($"Could not create the time off request : {timeOffResponse?.Error?.Message} ");
                return false;
            }

            var submitTimeOffResponse = await this.timeOffActivity.SubmitTimeOffRequestAsync(
                    allRequiredConfigurations.KronosSession,
                    user.KronosPersonNumber,
                    timeOffResponse?.EmployeeRequestMgm?.RequestItem?.GlobalTimeOffRequestItms?.FirstOrDefault()?.Id,
                    timeOffReqQueryDateSpan,
                    new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

            TimeOffMappingEntity newTimeOffReq = new TimeOffMappingEntity();

            // IsActive represents whether the time off was successfully created.
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
            newTimeOffReq.PartitionKey = $"{localStartDateTime.Month}_{localStartDateTime.Year}";
            newTimeOffReq.RowKey = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().Id;
            newTimeOffReq.ShiftsRequestId = timeOffEntity.Id;
            newTimeOffReq.KronosRequestId = timeOffResponse.EmployeeRequestMgm.RequestItem.GlobalTimeOffRequestItms.FirstOrDefault().Id;
            newTimeOffReq.KronosStatus = ApiConstants.Submitted;
            newTimeOffReq.ShiftsStatus = ApiConstants.Pending;

            this.AddorUpdateTimeOffMappingAsync(newTimeOffReq);

            // If isActive is false time off request was not submitted so return false and vice versa.
            return newTimeOffReq.IsActive;
        }

        /// <summary>
        /// Cancels a time off request that was  in Teams.
        /// </summary>
        /// <param name="timeOffRequestMapping">The mapping for the time off request.</param>
        /// <returns>Whether the time off request was cancelled successfully or not.</returns>
        internal async Task<bool> CancelTimeOffRequestInKronosAsync(TimeOffMappingEntity timeOffRequestMapping)
        {
            var timeOffRequestQueryDateSpan = $"{timeOffRequestMapping.StartDate}-{timeOffRequestMapping.EndDate}";

            // Get all the necessary prerequisites.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            var kronosUserId = timeOffRequestMapping.KronosPersonNumber;
            var kronosRequestId = timeOffRequestMapping.KronosRequestId;

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "KronosPersonNumber", $"{kronosUserId}" },
                { "KronosTimeOffRequestId", $"{kronosRequestId}" },
                { "Configured correctly", $"{allRequiredConfigurations.IsAllSetUpExists}" },
                { "Date range", $"{timeOffRequestQueryDateSpan}" },
            };

            if (allRequiredConfigurations.IsAllSetUpExists)
            {
                var response =
                    await this.timeOffActivity.CancelTimeOffRequestAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        timeOffRequestQueryDateSpan,
                        kronosUserId,
                        kronosRequestId).ConfigureAwait(false);

                data.Add("ResponseStatus", $"{response.Status}");

                if (response.Status == "Success")
                {
                    this.telemetryClient.TrackTrace($"Update table for cancellation of time off request: {kronosRequestId}", data);
                    timeOffRequestMapping.KronosStatus = ApiConstants.Retracted;
                    timeOffRequestMapping.ShiftsStatus = ApiConstants.Retracted;
                    await this.timeOffMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(timeOffRequestMapping).ConfigureAwait(false);
                    return true;
                }
            }

            this.telemetryClient.TrackTrace("CancelTimeOffRequestInKronos Failed", data);
            return false;
        }

        /// <summary>
        /// Creates and sends the relevant request to approve or deny a time off request.
        /// </summary>
        /// <param name="kronosReqId">The Kronos request id for the time off request.</param>
        /// <param name="kronosUserId">The Kronos user id for the assigned user.</param>
        /// <param name="teamsTimeOffEntity">The Teams time off entity.</param>
        /// <param name="timeOffRequestMapping">The mapping for the time off request.</param>
        /// <param name="managerMessage">The manager action message from Teams.</param>
        /// <param name="approved">Whether the request should be approved (true) or denied (false).</param>
        /// <param name="kronosTimeZone">The Kronos timezone.</param>
        /// <returns>Returns a bool that represents whether the request was a success (true) or not (false).</returns>
        internal async Task<bool> ApproveOrDenyTimeOffRequestInKronos(
            string kronosReqId,
            string kronosUserId,
            TimeOffRequestItem teamsTimeOffEntity,
            TimeOffMappingEntity timeOffRequestMapping,
            string managerMessage,
            bool approved,
            string kronosTimeZone)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace($"{Resource.ProcessTimeOffRequestsAsync} start at: {DateTime.Now.ToString("o", provider)}");

            // Teams provides date times in UTC so convert to the local time.
            var localStartDateTime = this.utility.UTCToKronosTimeZone(teamsTimeOffEntity.StartDateTime, kronosTimeZone);
            var localEndDateTime = this.utility.UTCToKronosTimeZone(teamsTimeOffEntity.EndDateTime, kronosTimeZone);

            var queryDateSpanStart = localStartDateTime.ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);
            var queryDateSpanEnd = localEndDateTime.ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

            var timeOffRequestQueryDateSpan = $"{queryDateSpanStart}-{queryDateSpanEnd}";

            // Get all the necessary prerequisites.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "KronosPersonNumber", $"{kronosUserId}" },
                { "KronosTimeOffRequestId", $"{kronosReqId}" },
                { "Approved", $"{approved}" },
                { "Configured correctly", $"{allRequiredConfigurations.IsAllSetUpExists}" },
                { "Date range", $"{timeOffRequestQueryDateSpan}" },
            };

            if (allRequiredConfigurations.IsAllSetUpExists)
            {
                // There is a bug in Teams where a managers notes(managerActionMessage) are not added to
                // the WFI request body. Until this is fixed syncing of manager notes from Teams to Kronos
                // is not possible. Uncommenting this code once fixed should get manager note syncing to work.

                /*
                // Get the existing time off request entity so we can add to existing notes
                var usersTimeOffRequestDetails = await this.timeOffActivity.GetTimeOffRequestDetailsAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        timeOffRequestQueryDateSpan,
                        kronosUserId,
                        kronosReqId).ConfigureAwait(false);

                if (usersTimeOffRequestDetails.Status != "Success")
                {
                    this.telemetryClient.TrackTrace($"Could not find the time off request with id: {kronosReqId}", data);
                    return false;
                }

                // There is a chance the previous request will return multiple time off entities so select the correct one
                var timeOffRequest = usersTimeOffRequestDetails.RequestMgmt.RequestItems.GlobalTimeOffRequestItem.SingleOrDefault(x => x.Id == kronosReqId);
                if (timeOffRequest == null)
                {
                    this.telemetryClient.TrackTrace($"Could not find the time off request with id: {kronosReqId}", data);
                    return false;
                }

                var comments = XmlHelper.GenerateKronosComments(managerMessage, this.appSettings.ManagerTimeOffRequestCommentText, timeOffRequest.Comments);

                // Add the comments to the time off request entity
                var addCommentsResponse = await this.timeOffActivity.AddManagerCommentsToTimeOffRequestAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        kronosReqId,
                        localStartDateTime,
                        localEndDateTime,
                        timeOffRequestQueryDateSpan,
                        kronosUserId,
                        timeOffRequest.TimeOffPeriods.TimeOffPeriod.PayCodeName,
                        comments).ConfigureAwait(false);

                if (addCommentsResponse.Status != "Success")
                {
                    this.telemetryClient.TrackTrace($"Failed to add the manager notes to the time off request: {kronosReqId}", data);
                    return false;
                }
                */

                var response =
                    await this.timeOffActivity.ApproveOrDenyTimeOffRequestAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        timeOffRequestQueryDateSpan,
                        kronosUserId,
                        approved,
                        kronosReqId).ConfigureAwait(false);

                data.Add("ResponseStatus", $"{response.Status}");

                if (response.Status == "Success" && approved)
                {
                    this.telemetryClient.TrackTrace($"Update table for approval of time off request: {kronosReqId}", data);
                    timeOffRequestMapping.KronosStatus = ApiConstants.ApprovedStatus;
                    await this.timeOffMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(timeOffRequestMapping).ConfigureAwait(false);
                    return true;
                }

                if (response.Status == "Success" && !approved)
                {
                    this.telemetryClient.TrackTrace($"Update table for refusal of time off request: {kronosReqId}", data);
                    timeOffRequestMapping.KronosStatus = ApiConstants.Refused;
                    await this.timeOffMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(timeOffRequestMapping).ConfigureAwait(false);
                    return true;
                }
            }

            this.telemetryClient.TrackTrace("ApproveOrDenyTimeOffRequestInKronos - Configuration incorrect", data);
            return false;
        }

        /// <summary>
        /// This method processes all of the time offs in a batch manner.
        /// </summary>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="processKronosUsersQueueInBatch">The users in batch.</param>
        /// <param name="lookUpData">The look up data.</param>
        /// <param name="timeOffResponseDetails">The time off response details.</param>
        /// <param name="timeOffReasons">The time off reasons.</param>
        /// <param name="graphClient">The MS Graph Service client.</param>
        /// <param name="monthPartitionKey">The montwise partition key.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ProcessTimeOffEntitiesBatchAsync(
            SetupDetails configurationDetails,
            IEnumerable<UserDetailsModel> processKronosUsersQueueInBatch,
            List<TimeOffMappingEntity> lookUpData,
            List<GlobalTimeOffRequestItem> timeOffResponseDetails,
            List<PayCodeToTimeOffReasonsMappingEntity> timeOffReasons,
            GraphServiceClient graphClient,
            string monthPartitionKey)
        {
            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            var timeOffLookUpEntriesFoundList = new List<TimeOffMappingEntity>();
            var timeOffNotFoundList = new List<GlobalTimeOffRequestItem>();
            var userModelList = new List<UserDetailsModel>();
            var userModelNotFoundList = new List<UserDetailsModel>();
            var kronosPayCodeList = new List<PayCodeToTimeOffReasonsMappingEntity>();
            var timeOffRequestsPayCodeList = new List<PayCodeToTimeOffReasonsMappingEntity>();
            var globalTimeOffRequestDetails = new List<GlobalTimeOffRequestItem>();

            foreach (var user in processKronosUsersQueueInBatch)
            {
                foreach (var timeOffRequestItem in timeOffResponseDetails.Where(x => x.Employee.PersonIdentity.PersonNumber == user.KronosPersonNumber))
                {
                    if (timeOffRequestItem.StatusName.Equals(ApiConstants.ApprovedStatus, StringComparison.Ordinal))
                    {
                        this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync look up count: {lookUpData.Count} ");

                        if (lookUpData.Count == 0)
                        {
                            // Getting a TimeOffReasonId object based on the TimeOff paycode from Kronos and the team ID in Shifts.
                            var timeOffReasonId = timeOffReasons.
                                Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == user.ShiftTeamId).FirstOrDefault();
                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync PaycodeName : {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName} ");
                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync ReqId : {timeOffRequestItem.Id} ");
                            if (timeOffReasonId != null)
                            {
                                timeOffNotFoundList.Add(timeOffRequestItem);
                                userModelNotFoundList.Add(user);
                                kronosPayCodeList.Add(timeOffReasonId);
                            }
                            else
                            {
                                // Track in telemetry saying that the TimeOffReasonId cannot be found.
                                this.telemetryClient.TrackTrace($"Cannot find the TimeOffReason corresponding to the Kronos paycode: {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, Kronos request ID: {timeOffRequestItem.Id}");
                            }
                        }
                        else
                        {
                            var kronosUniqueIdExists = lookUpData.Where(x => x.KronosRequestId == timeOffRequestItem.Id);
                            var monthPartitions = Utility.GetMonthPartition(
                                timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.StartDate, timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.EndDate);

                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync PaycodeName : {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName} ");
                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync ReqId : {timeOffRequestItem.Id} ");

                            if (kronosUniqueIdExists.Any() && kronosUniqueIdExists.FirstOrDefault().KronosStatus == ApiConstants.Submitted)
                            {
                                // Getting a TimeOffReasonId object based on the TimeOff paycode from Kronos and the team ID in Shifts.
                                var timeOffReasonId = timeOffReasons.
                                    Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == user.ShiftTeamId).FirstOrDefault();

                                // Kronos API does not return all the PayCodes present in Kronos UI. For such cases TimeOffReason mapping
                                // will be null and that TimeOffs will not be synced.
                                if (timeOffReasonId != null)
                                {
                                    timeOffLookUpEntriesFoundList.Add(kronosUniqueIdExists.FirstOrDefault());
                                    userModelList.Add(user);
                                    timeOffRequestsPayCodeList.Add(timeOffReasonId);
                                    globalTimeOffRequestDetails.Add(timeOffRequestItem);
                                }
                                else
                                {
                                    // Track in telemetry saying that the TimeOffReasonId cannot be found.
                                    this.telemetryClient.TrackTrace($"Cannot find the TimeOffReason corresponding to the Kronos paycode: {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, Kronos request ID: {timeOffRequestItem.Id}");
                                }
                            }
                            else if (kronosUniqueIdExists.Any() || monthPartitions?.FirstOrDefault() != monthPartitionKey)
                            {
                                continue;
                            }
                            else
                            {
                                // Getting a TimeOffReasonId object based on the TimeOff paycode from Kronos and the team ID in Shifts.
                                var timeOffReasonId = timeOffReasons.
                                    Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == user.ShiftTeamId).FirstOrDefault();

                                // Kronos API does not return all the PayCodes present in Kronos UI. For such cases TimeOffReason mapping
                                // will be null and that TimeOffs will not be synced.
                                if (timeOffReasonId != null)
                                {
                                    timeOffNotFoundList.Add(timeOffRequestItem);
                                    userModelNotFoundList.Add(user);
                                    kronosPayCodeList.Add(timeOffReasonId);
                                }
                                else
                                {
                                    // Track in telemetry saying that the TimeOffReasonId cannot be found.
                                    this.telemetryClient.TrackTrace($"Cannot find the TimeOffReason corresponding to the Kronos paycode: {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, Kronos request ID: {timeOffRequestItem.Id}");
                                }
                            }
                        }
                    }

                    if (timeOffRequestItem.StatusName.Equals(ApiConstants.Refused, StringComparison.Ordinal)
                        || timeOffRequestItem.StatusName.Equals(ApiConstants.Retract, StringComparison.Ordinal))
                    {
                        var reqDetails = lookUpData.Where(c => c.KronosRequestId == timeOffRequestItem.Id).FirstOrDefault();
                        var timeOffReasonIdtoUpdate = timeOffReasons.Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == user.ShiftTeamId).FirstOrDefault();
                        if (reqDetails != null && timeOffReasonIdtoUpdate != null && reqDetails.KronosStatus == ApiConstants.Submitted)
                        {
                            await this.DeclineTimeOffRequestAsync(
                                    timeOffRequestItem,
                                    user,
                                    reqDetails.ShiftsRequestId,
                                    configurationDetails,
                                    monthPartitionKey).ConfigureAwait(false);
                        }
                        else
                        {
                            this.telemetryClient.TrackTrace($"The declined timeoff request {timeOffRequestItem.Id} is not submitted from Shifts or timeOff reason selected while submitting" +
                                "is not a valid paycode. Hence mapping is not present for TimeOffReason and Paycode.");
                        }
                    }
                }
            }

            await this.ApproveTimeOffRequestAsync(
                 timeOffLookUpEntriesFoundList,
                 userModelList,
                 configurationDetails,
                 monthPartitionKey,
                 globalTimeOffRequestDetails).ConfigureAwait(false);

            await this.AddTimeOffRequestAsync(
                graphClient,
                userModelNotFoundList,
                timeOffNotFoundList,
                kronosPayCodeList,
                monthPartitionKey).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync ended for {monthPartitionKey}.");
        }

        /// <summary>
        /// Retrieves time off results in a batch manner.
        /// </summary>
        /// <param name="employees">The list of employees.</param>
        /// <param name="kronosEndpoint">The Kronos WFC API endpoint.</param>
        /// <param name="workforceIntegrationId">The Workforce Integration ID.</param>
        /// <param name="jsession">The Kronos Jsession.</param>
        /// <param name="queryStartDate">The query start date.</param>
        /// <param name="queryEndDate">The query end date.</param>
        /// <returns>Returns a unit of execution that contains the type of timeOff.Response.</returns>
        private async Task<TimeOffReq.Response> GetTimeOffResultsByBatchAsync(
            List<ResponseHyperFindResult> employees,
            string kronosEndpoint,
            string workforceIntegrationId,
            string jsession,
            string queryStartDate,
            string queryEndDate)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "PersonNumber", employees.FirstOrDefault().PersonNumber },
                { "WorkforceIntegrationId", workforceIntegrationId },
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            var timeOffQueryDateSpan = queryStartDate + "-" + queryEndDate;

            return await this.timeOffActivity.GetTimeOffRequestDetailsByBatchAsync(
                new Uri(kronosEndpoint),
                jsession,
                timeOffQueryDateSpan,
                employees).ConfigureAwait(false);
        }

        /// <summary>
        /// Method that creates the Microsoft Graph Service client.
        /// </summary>
        /// <param name="token">The Graph Access token.</param>
        /// <returns>A type of <see cref="GraphServiceClient"/> contained in a unit of execution.</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously

        private async Task<GraphServiceClient> CreateGraphClientWithDelegatedAccessAsync(
            string token,
            string workforceIntegrationId)
#pragma warning restore CS1998 // Async method lacks 'await' operators and will run synchronously
        {
            if (string.IsNullOrEmpty(token))
            {
                throw new ArgumentNullException(token);
            }

            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace($"CreateGraphClientWithDelegatedAccessAsync-TimeController called at {DateTime.Now.ToString("o", provider)}");

            var graphClient = new GraphServiceClient(
            new DelegateAuthenticationProvider(
                (requestMessage) =>
                {
                    requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                    requestMessage.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                    requestMessage.Headers.Add("X-MS-WFMPassthrough", workforceIntegrationId);
                    return Task.FromResult(0);
                }));
            return graphClient;
        }

        /// <summary>
        /// Method that will calculate the end date accordingly.
        /// </summary>
        /// <param name="requestItem">An object of type <see cref="GlobalTimeOffRequestItem"/>.</param>
        /// <param name="timeZone">The Kronos time zone to use when converting times to and from UTC.</param>
        /// <returns>A type of DateTimeOffset.</returns>
        private DateTimeOffset CalculateEndDate(GlobalTimeOffRequestItem requestItem, string timeZone)
        {
            var correctStartDate = this.utility.CalculateStartDateTime(requestItem, timeZone);
            var provider = CultureInfo.InvariantCulture;
            var kronosTimeZoneId = string.IsNullOrEmpty(timeZone) ? this.appSettings.KronosTimeZone : timeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            var telemetryProps = new Dictionary<string, string>()
            {
                { "CorrectedStartDate", correctStartDate.ToString("o", provider) },
                { "TimeOffPeriodDuration", requestItem.TimeOffPeriods.TimeOffPeriod.Duration.ToString(CultureInfo.InvariantCulture) },
            };

            if (requestItem.TimeOffPeriods.TimeOffPeriod.Duration == Resource.DurationInFullDay)
            {
                // If the time off request is for a full-day.
                // Add 24 hours to the correct start date.
                telemetryProps.Add("TimeOffPeriodLength", "24");
                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);
                var dateTimeEnd = DateTime.ParseExact(requestItem.TimeOffPeriods.TimeOffPeriod.EndDate, "M/dd/yyyy", CultureInfo.InvariantCulture);
                var dateTimeStart = DateTime.ParseExact(requestItem.TimeOffPeriods.TimeOffPeriod.StartDate, "M/dd/yyyy", CultureInfo.InvariantCulture);
                if (dateTimeEnd.Equals(dateTimeStart))
                {
                    return correctStartDate.AddHours(24);
                }
                else
                {
                    return TimeZoneInfo.ConvertTimeToUtc(dateTimeEnd, kronosTimeZoneInfo).AddHours(24);
                }
            }
            else if (requestItem.TimeOffPeriods.TimeOffPeriod.Duration == Resource.DurationInHour)
            {
                // If the time off has a number of hours attached to it.
                // Take the necessary start date and start time, and add the hours to the time off.
                var timeSpan = TimeSpan.Parse(requestItem.TimeOffPeriods.TimeOffPeriod.Length, CultureInfo.InvariantCulture);
                telemetryProps.Add("TimeOffPeriodLength", timeSpan.Hours.ToString(CultureInfo.InvariantCulture));
                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

                return correctStartDate.AddHours(timeSpan.Hours);
            }
            else
            {
                // If the time off is either a first-half day, second-half day, or half-day.
                // Add 12 hours to the correct start date.
                return correctStartDate.AddHours(12);
            }
        }

        /// <summary>
        /// Method that will add a time off request.
        /// </summary>
        /// <param name="graphClient">The MS Graph client.</param>
        /// <param name="userModelNotFoundList">The list of users that are not found.</param>
        /// <param name="timeOffNotFoundList">This list of time off records that are not found.</param>
        /// <param name="kronosPayCodeList">The list of Kronos WFC Paycodes.</param>
        /// <param name="monthPartitionKey">The month partition key.</param>
        /// <returns>A unit of execution.</returns>
        private async Task AddTimeOffRequestAsync(
            GraphServiceClient graphClient,
            List<UserDetailsModel> userModelNotFoundList,
            List<GlobalTimeOffRequestItem> timeOffNotFoundList,
            List<PayCodeToTimeOffReasonsMappingEntity> kronosPayCodeList,
            string monthPartitionKey)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace($"AddTimeOffRequestAsync started for {monthPartitionKey}.", telemetryProps);

            // create entries from not found list
            for (int i = 0; i < timeOffNotFoundList.Count && kronosPayCodeList?.Count > 0; i++)
            {
                if (kronosPayCodeList[i]?.TimeOffReasonId != null)
                {
                    var timeOff = new TimeOff
                    {
                        UserId = userModelNotFoundList[i].ShiftUserId,
                        SharedTimeOff = new TimeOffItem
                        {
                            TimeOffReasonId = kronosPayCodeList[i].TimeOffReasonId,
                            StartDateTime = this.utility.CalculateStartDateTime(timeOffNotFoundList[i], userModelNotFoundList[i].KronosTimeZone),
                            EndDateTime = this.CalculateEndDate(timeOffNotFoundList[i], userModelNotFoundList[i].KronosTimeZone),
                            Theme = ScheduleEntityTheme.White,
                        },
                    };

                    var timeOffs = await graphClient.Teams[userModelNotFoundList[i].ShiftTeamId].Schedule.TimesOff
                        .Request()
                        .AddAsync(timeOff).ConfigureAwait(false);

                    TimeOffMappingEntity timeOffMappingEntity = new TimeOffMappingEntity
                    {
                        Duration = timeOffNotFoundList[i].TimeOffPeriods.TimeOffPeriod.Duration,
                        EndDate = timeOffNotFoundList[i].TimeOffPeriods.TimeOffPeriod.EndDate,
                        StartDate = timeOffNotFoundList[i].TimeOffPeriods.TimeOffPeriod.StartDate,
                        StartTime = timeOffNotFoundList[i].TimeOffPeriods.TimeOffPeriod.StartTime,
                        PayCodeName = timeOffNotFoundList[i].TimeOffPeriods.TimeOffPeriod.PayCodeName,
                        KronosPersonNumber = timeOffNotFoundList[i].Employee.PersonIdentity.PersonNumber,
                        PartitionKey = monthPartitionKey,
                        RowKey = timeOffNotFoundList[i].Id,
                        ShiftsRequestId = timeOffs.Id,
                        KronosRequestId = timeOffNotFoundList[i].Id,
                        ShiftsStatus = ApiConstants.Pending,
                        KronosStatus = ApiConstants.Submitted,
                        IsActive = true,
                    };

                    this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                }
                else
                {
                    telemetryProps.Add("TimeOffReason for " + timeOffNotFoundList[i].Id, "NotFound");
                }

                this.telemetryClient.TrackTrace($"AddTimeOffRequestAsync ended for {monthPartitionKey}.", telemetryProps);
            }
        }

        /// <summary>
        /// Method that approves a time off request.
        /// </summary>
        /// <param name="timeOffLookUpEntriesFoundList">The time off look up entries that are found.</param>
        /// <param name="user">The user.</param>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="monthPartitionKey">The monthwise partition key.</param>
        /// <param name="globalTimeOffRequestDetails">The list of global time off request details.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ApproveTimeOffRequestAsync(
            List<TimeOffMappingEntity> timeOffLookUpEntriesFoundList,
            List<UserDetailsModel> user,
            SetupDetails configurationDetails,
            string monthPartitionKey,
            List<GlobalTimeOffRequestItem> globalTimeOffRequestDetails)
        {
            this.telemetryClient.TrackTrace($"ApproveTimeOffRequestAsync started for {monthPartitionKey}.");

            for (int i = 0; i < timeOffLookUpEntriesFoundList.Count; i++)
            {
                var timeOffReqCon = new
                {
                    Message = string.Empty,
                };

                var requestString = JsonConvert.SerializeObject(timeOffReqCon);
                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", configurationDetails.ShiftsAccessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Send Passthrough header to indicate the sender of request in outbound call.
                httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", configurationDetails.WFIId);

                using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + user[i].ShiftTeamId + "/schedule/timeOffRequests/" + timeOffLookUpEntriesFoundList[i].ShiftsRequestId + "/approve")
                {
                    Content = new StringContent(requestString, Encoding.UTF8, "application/json"),
                })
                {
                    var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        TimeOffMappingEntity timeOffMappingEntity = new TimeOffMappingEntity
                        {
                            Duration = globalTimeOffRequestDetails[i].TimeOffPeriods.TimeOffPeriod.Duration,
                            EndDate = globalTimeOffRequestDetails[i].TimeOffPeriods.TimeOffPeriod.EndDate,
                            StartDate = globalTimeOffRequestDetails[i].TimeOffPeriods.TimeOffPeriod.StartDate,
                            StartTime = globalTimeOffRequestDetails[i].TimeOffPeriods.TimeOffPeriod.StartTime,
                            PayCodeName = globalTimeOffRequestDetails[i].TimeOffPeriods.TimeOffPeriod.PayCodeName,
                            KronosPersonNumber = globalTimeOffRequestDetails[i].Employee.PersonIdentity.PersonNumber,
                            PartitionKey = monthPartitionKey,
                            RowKey = globalTimeOffRequestDetails[i].Id,
                            ShiftsRequestId = timeOffLookUpEntriesFoundList[i].ShiftsRequestId,
                            IsActive = true,
                            KronosRequestId = globalTimeOffRequestDetails[i].Id,
                            ShiftsStatus = ApiConstants.ApprovedStatus,
                            KronosStatus = ApiConstants.ApprovedStatus,
                        };

                        this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                    }
                }
            }

            this.telemetryClient.TrackTrace($"ApproveTimeOffRequestAsync ended for {monthPartitionKey}.");
        }

        /// <summary>
        /// Method to decline the time off request.
        /// </summary>
        /// <param name="globalTimeOffRequestItem">The time off request item.</param>
        /// <param name="user">The user.</param>
        /// <param name="timeOffId">The time off Id from Kronos.</param>
        /// <param name="configurationDetails">The configuration details.</param>
        /// <param name="monthPartitionKey">The month wise partition key.</param>
        /// <returns>A unit of execution.</returns>
        private async Task DeclineTimeOffRequestAsync(
            GlobalTimeOffRequestItem globalTimeOffRequestItem,
            UserDetailsModel user,
            string timeOffId,
            SetupDetails configurationDetails,
            string monthPartitionKey)
        {
            this.telemetryClient.TrackTrace($"DeclineTimeOffRequestAsync started for time off id {timeOffId}.");
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", configurationDetails.ShiftsAccessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // Send Passthrough header to indicate the sender of request in outbound call.
            httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", configurationDetails.WFIId);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + user.ShiftTeamId + "/schedule/timeOffRequests/" + timeOffId + "/decline"))
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    TimeOffMappingEntity timeOffMappingEntity = new TimeOffMappingEntity
                    {
                        Duration = globalTimeOffRequestItem.TimeOffPeriods.TimeOffPeriod.Duration,
                        EndDate = globalTimeOffRequestItem.TimeOffPeriods.TimeOffPeriod.EndDate,
                        StartDate = globalTimeOffRequestItem.TimeOffPeriods.TimeOffPeriod.StartDate,
                        StartTime = globalTimeOffRequestItem.TimeOffPeriods.TimeOffPeriod.StartTime,
                        PayCodeName = globalTimeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName,
                        KronosPersonNumber = globalTimeOffRequestItem.Employee.PersonIdentity.PersonNumber,
                        PartitionKey = monthPartitionKey,
                        RowKey = globalTimeOffRequestItem.Id,
                        ShiftsRequestId = timeOffId,
                        IsActive = true,
                        KronosRequestId = globalTimeOffRequestItem.Id,
                        ShiftsStatus = globalTimeOffRequestItem.StatusName,
                        KronosStatus = ApiConstants.Refused,
                    };

                    this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                }
            }

            this.telemetryClient.TrackTrace($"DeclineTimeOffRequestAsync ended for time off id {timeOffId}.");
        }

        /// <summary>
        /// The method which will add or update a Time Off Mapping in Azure table storage.
        /// </summary>
        /// <param name="timeOffMappingEntity">The time off mapping entity to update or add.</param>
        private async void AddorUpdateTimeOffMappingAsync(TimeOffMappingEntity timeOffMappingEntity)
        {
            await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(timeOffMappingEntity, "TimeOffMapping").ConfigureAwait(false);
        }
    }
}