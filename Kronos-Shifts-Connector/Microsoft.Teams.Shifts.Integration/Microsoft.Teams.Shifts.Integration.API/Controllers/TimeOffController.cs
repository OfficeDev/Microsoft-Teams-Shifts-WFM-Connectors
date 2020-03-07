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
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Newtonsoft.Json;
    using TimeOffReq = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using TimeOffRequest = Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;

    /// <summary>
    /// Time off controller.
    /// </summary>
    [Route("api/TimeOff")]
    [Authorize(Policy = "AppID")]
    [ApiController]
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
                var allUsers = await this.GetAllMappedUserDetailsAsync(allRequiredConfigurations.WFIId).ConfigureAwait(false);

                var monthPartitions = Utility.GetMonthPartition(timeOffStartDate, timeOffEndDate);
                var processNumberOfUsersInBatch = this.appSettings.ProcessNumberOfUsersInBatch;
                var userCount = allUsers.Count();
                int userIteration = Utility.GetIterablesCount(Convert.ToInt32(processNumberOfUsersInBatch, CultureInfo.InvariantCulture), userCount);
                var graphClient = await this.CreateGraphClientWithDelegatedAccessAsync(allRequiredConfigurations.ShiftsAccessToken).ConfigureAwait(false);

                if (monthPartitions != null && monthPartitions.Count > 0)
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
                        var usersDetails = allUsers?.ToList();

                        // TODO #1 - Add the code for checking the dates.
                        foreach (var item in usersDetails)
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

                            var timeOffResponseDetails = timeOffDetails?.RequestMgmt?.RequestItems?.GlobalTimeOffRequestItem;

                            var timeOffLookUpEntriesFoundList = new List<TimeOffMappingEntity>();
                            var timeOffNotFoundList = new List<GlobalTimeOffRequestItem>();

                            var userModelList = new List<UserDetailsModel>();
                            var userModelNotFoundList = new List<UserDetailsModel>();
                            var kronosPayCodeList = new List<PayCodeToTimeOffReasonsMappingEntity>();
                            var timeOffRequestsPayCodeList = new List<PayCodeToTimeOffReasonsMappingEntity>();
                            var globalTimeOffRequestDetails = new List<GlobalTimeOffRequestItem>();

                            await this.ProcessTimeOffEntitiesBatchAsync(
                                allRequiredConfigurations.ShiftsAccessToken,
                                timeOffLookUpEntriesFoundList,
                                timeOffNotFoundList,
                                userModelList,
                                userModelNotFoundList,
                                kronosPayCodeList,
                                processKronosUsersQueueInBatch,
                                lookUpData,
                                timeOffResponseDetails,
                                timeOffReasons,
                                graphClient,
                                monthPartitionKey,
                                timeOffRequestsPayCodeList,
                                globalTimeOffRequestDetails).ConfigureAwait(false);
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
        /// This method processes all of the time offs in a batch manner.
        /// </summary>
        /// <param name="accessToken">The MS Graph Access token.</param>
        /// <param name="timeOffLookUpEntriesFoundList">The look up Time Off entries that are not found.</param>
        /// <param name="timeOffNotFoundList">The list of time offs that are not found.</param>
        /// <param name="userModelList">The list of users.</param>
        /// <param name="userModelNotFoundList">The list of users that are not found.</param>
        /// <param name="kronosPayCodeList">The Kronos pay code list.</param>
        /// <param name="processKronosUsersQueueInBatch">The users in batch.</param>
        /// <param name="lookUpData">The look up data.</param>
        /// <param name="timeOffResponseDetails">The time off response details.</param>
        /// <param name="timeOffReasons">The time off reasons.</param>
        /// <param name="graphClient">The MS Graph Service client.</param>
        /// <param name="monthPartitionKey">The montwise partition key.</param>
        /// <param name="timeOffRequestsPayCodeList">The list of time off request pay codes.</param>
        /// <param name="globalTimeOffRequestDetails">The time off request details.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ProcessTimeOffEntitiesBatchAsync(
            string accessToken,
            List<TimeOffMappingEntity> timeOffLookUpEntriesFoundList,
            List<GlobalTimeOffRequestItem> timeOffNotFoundList,
            List<UserDetailsModel> userModelList,
            List<UserDetailsModel> userModelNotFoundList,
            List<PayCodeToTimeOffReasonsMappingEntity> kronosPayCodeList,
            IEnumerable<UserDetailsModel> processKronosUsersQueueInBatch,
            List<TimeOffMappingEntity> lookUpData,
            List<GlobalTimeOffRequestItem> timeOffResponseDetails,
            List<PayCodeToTimeOffReasonsMappingEntity> timeOffReasons,
            GraphServiceClient graphClient,
            string monthPartitionKey,
            List<PayCodeToTimeOffReasonsMappingEntity> timeOffRequestsPayCodeList,
            List<GlobalTimeOffRequestItem> globalTimeOffRequestDetails)
        {
            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync started.");

            foreach (var item in processKronosUsersQueueInBatch)
            {
                foreach (var timeOffRequestItem in timeOffResponseDetails.Where(x => x.Employee.PersonIdentity.PersonNumber == item.KronosPersonNumber))
                {
                    if (timeOffRequestItem.StatusName.Equals(ApiConstants.ApprovedStatus, StringComparison.Ordinal))
                    {
                        this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync look up count: {lookUpData.Count} ");

                        if (lookUpData.Count == 0)
                        {
                            // Performing the query using the RowKey as the paycode name from Kronos; and the PartitionKey as the TeamID.
                            var timeOffReasonId = timeOffReasons.
                                Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == item.ShiftTeamId).FirstOrDefault();
                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync PaycodeName : {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, Kronos request ID : {timeOffRequestItem.Id}");
                            
                            // The null check is being performed on the timeOffReasonId because there may be chances where
                            // all of the Kronos paycodes have not been mapped to a specific team on the Shifts side.
                            if (timeOffReasonId != null)
                            {
                                timeOffNotFoundList.Add(timeOffRequestItem);
                                userModelNotFoundList.Add(item);
                                kronosPayCodeList.Add(timeOffReasonId);
                            }
                            else
                            {
                                // Track in telemetry saying that the TimeOffReason cannot be found.
                                this.telemetryClient.TrackTrace($"Cannot find the TimeOffReason corresponding to the Kronos paycode: {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, Kronos request ID: {timeOffRequestItem.Id}");
                            }
                        }
                        else
                        {
                            var kronosUniqueIdExists = lookUpData.Where(x => x.KronosRequestId == timeOffRequestItem.Id);
                            var monthPartitions = Utility.GetMonthPartition(
                                timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.StartDate, timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.EndDate);

                            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync PaycodeName : {timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName}, ReqId : {timeOffRequestItem.Id}");

                            if (kronosUniqueIdExists.Any() && kronosUniqueIdExists.FirstOrDefault().StatusName == ApiConstants.SubmitRequests)
                            {
                                // Again here, looking up the TimeOffReasons using the RowKey as the paycode name from Kronos;
                                // PartitionKey is the TeamID.
                                var timeOffReasonId = timeOffReasons.
                                    Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == item.ShiftTeamId).FirstOrDefault();

                                timeOffLookUpEntriesFoundList.Add(kronosUniqueIdExists.FirstOrDefault());
                                userModelList.Add(item);
                                timeOffRequestsPayCodeList.Add(timeOffReasonId);
                                globalTimeOffRequestDetails.Add(timeOffRequestItem);
                            }
                            else if (kronosUniqueIdExists.Any() || monthPartitions?.FirstOrDefault() != monthPartitionKey)
                            {
                                continue;
                            }
                            else
                            {
                                // Looking up the necessary TimeOffReason using the RowKey as the Kronos paycode name,
                                // PartitionKey as the TeamID.
                                var timeOffReasonId = timeOffReasons.
                                    Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == item.ShiftTeamId).FirstOrDefault();
                                if (timeOffReasonId != null)
                                {
                                    timeOffNotFoundList.Add(timeOffRequestItem);
                                    userModelNotFoundList.Add(item);
                                    kronosPayCodeList.Add(timeOffReasonId);
                                }
                            }
                        }
                    }

                    if (timeOffRequestItem.StatusName.Equals(ApiConstants.Refused, StringComparison.Ordinal)
                        || timeOffRequestItem.StatusName.Equals(ApiConstants.Retract, StringComparison.Ordinal))
                    {
                        var reqDetails = lookUpData.Where(c => c.KronosRequestId == timeOffRequestItem.Id).FirstOrDefault();
                        var timeOffReasonIdtoUpdate = timeOffReasons.Where(t => t.RowKey == timeOffRequestItem.TimeOffPeriods.TimeOffPeriod.PayCodeName && t.PartitionKey == item.ShiftTeamId).FirstOrDefault();
                        if (reqDetails != null && timeOffReasonIdtoUpdate != null && reqDetails.StatusName == ApiConstants.SubmitRequests)
                        {
                            await this.DeclineTimeOffRequestAsync(
                                    timeOffRequestItem,
                                    item,
                                    reqDetails.ShiftsRequestId,
                                    accessToken,
                                    monthPartitionKey).ConfigureAwait(false);
                        }
                    }
                }
            }

            await this.ApproveTimeOffRequestAsync(
                 timeOffLookUpEntriesFoundList,
                 userModelList,
                 timeOffRequestsPayCodeList,
                 accessToken,
                 monthPartitionKey,
                 globalTimeOffRequestDetails).ConfigureAwait(false);

            await this.AddTimeOffRequestAsync(
                graphClient,
                userModelNotFoundList,
                timeOffNotFoundList,
                kronosPayCodeList,
                monthPartitionKey).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"ProcessTimeOffEntitiesBatchAsync ended.");
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

            var timeOffRequests = await this.timeOffActivity.GetTimeOffRequestDetailsByBatchAsync(
                new Uri(kronosEndpoint),
                jsession,
                timeOffQueryDateSpan,
                employees).ConfigureAwait(false);

            return timeOffRequests;
        }

        /// <summary>
        /// Method that creates the Microsoft Graph Service client.
        /// </summary>
        /// <param name="token">The Graph Access token.</param>
        /// <returns>A type of <see cref="GraphServiceClient"/> contained in a unit of execution.</returns>
#pragma warning disable CS1998 // Async method lacks 'await' operators and will run synchronously
        private async Task<GraphServiceClient> CreateGraphClientWithDelegatedAccessAsync(
            string token)
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
                    return Task.FromResult(0);
                }));
            return graphClient;
        }

        /// <summary>
        /// Method that will calculate the end date accordingly.
        /// </summary>
        /// <param name="requestItem">An object of type <see cref="GlobalTimeOffRequestItem"/>.</param>
        /// <returns>A type of DateTimeOffset.</returns>
        private DateTimeOffset CalculateEndDate(GlobalTimeOffRequestItem requestItem)
        {
            var correctStartDate = this.utility.CalculateStartDateTime(requestItem);
            var provider = CultureInfo.InvariantCulture;
            var kronosTimeZoneId = this.appSettings.KronosTimeZone;
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
        /// <param name="graphClient">The MS Graph Client.</param>
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

            this.telemetryClient.TrackTrace("AddTimeOffRequestAsync started", telemetryProps);

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
                            StartDateTime = this.utility.CalculateStartDateTime(timeOffNotFoundList[i]),
                            EndDateTime = this.CalculateEndDate(timeOffNotFoundList[i]),
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
                        StatusName = timeOffNotFoundList[i].StatusName,
                        IsActive = true,
                    };

                    this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                }
                else
                {
                    telemetryProps.Add("TimeOffReason for " + timeOffNotFoundList[i].Id, "NotFound");
                }

                this.telemetryClient.TrackTrace("AddTimeOffRequestAsync ended", telemetryProps);
            }
        }

        /// <summary>
        /// Method that approves a time off request.
        /// </summary>
        /// <param name="timeOffLookUpEntriesFoundList">The time off look up entries that are found.</param>
        /// <param name="user">The user.</param>
        /// <param name="timeOffReasonId">The Shifts Time Off Reason ID.</param>
        /// <param name="accessToken">The MS Graph Access Token.</param>
        /// <param name="monthPartitionKey">The monthwise partition key.</param>
        /// <param name="globalTimeOffRequestDetails">The list of global time off request details.</param>
        /// <returns>A unit of execution.</returns>
        private async Task ApproveTimeOffRequestAsync(
            List<TimeOffMappingEntity> timeOffLookUpEntriesFoundList,
            List<UserDetailsModel> user,
            List<PayCodeToTimeOffReasonsMappingEntity> timeOffReasonId,
            string accessToken,
            string monthPartitionKey,
            List<GlobalTimeOffRequestItem> globalTimeOffRequestDetails)
        {
            this.telemetryClient.TrackTrace($"ApproveTimeOffRequestAsync started.");
            for (int i = 0; i < timeOffLookUpEntriesFoundList.Count; i++)
            {
                var timeOffReqCon = new TimeOffRequestItem
                {
                    Id = timeOffLookUpEntriesFoundList[i].ShiftsRequestId,
                    CreatedDateTime = DateTime.Now,
                    LastModifiedDateTime = DateTime.Now,
                    AssignedTo = ApiConstants.Manager,
                    State = ApiConstants.Pending,
                    SenderDateTime = DateTime.Now,
                    SenderMessage = globalTimeOffRequestDetails[i].Comments?.Comment.FirstOrDefault()?.CommentText,
                    SenderUserId = Guid.Parse(user[i].ShiftUserId),
                    ManagerActionDateTime = null,
                    ManagerActionMessage = null,
                    ManagerUserId = string.Empty,
                    StartDateTime = this.utility.CalculateStartDateTime(globalTimeOffRequestDetails[i]),
                    EndDateTime = this.CalculateEndDate(globalTimeOffRequestDetails[i]),
                    TimeOffReasonId = timeOffReasonId[i].TimeOffReasonId,
                    LastModifiedBy = new LastModifiedBy
                    {
                        Application = null,
                        Device = null,
                        Conversation = null,
                        User = new TimeOffRequest.User
                        {
                            Id = Guid.Parse(user[i].ShiftUserId),
                            DisplayName = user[i].ShiftUserDisplayName,
                        },
                    },
                };

                var requestString = JsonConvert.SerializeObject(timeOffReqCon);
                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
                            StatusName = ApiConstants.ApprovedStatus,
                        };

                        this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                    }
                }
            }

            this.telemetryClient.TrackTrace($"ApproveTimeOffRequestAsync ended.");
        }

        /// <summary>
        /// Method to decline the time off request.
        /// </summary>
        /// <param name="globalTimeOffRequestItem">The time off request item.</param>
        /// <param name="user">The user.</param>
        /// <param name="timeOffId">The time off Id from Kronos.</param>
        /// <param name="accessToken">The MS Graph Access token.</param>
        /// <param name="monthPartitionKey">The month wise partition key.</param>
        /// <returns>A unit of execution.</returns>
        private async Task DeclineTimeOffRequestAsync(
            GlobalTimeOffRequestItem globalTimeOffRequestItem,
            UserDetailsModel user,
            string timeOffId,
            string accessToken,
            string monthPartitionKey)
        {
            this.telemetryClient.TrackTrace($"DeclineTimeOffRequestAsync started.");
            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
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
                        StatusName = globalTimeOffRequestItem.StatusName,
                    };

                    this.AddorUpdateTimeOffMappingAsync(timeOffMappingEntity);
                }
            }

            this.telemetryClient.TrackTrace($"DeclineTimeOffRequestAsync ended.");
        }

        /// <summary>
        /// The method which will add or update a Time Off Mapping in Azure table storage.
        /// </summary>
        /// <param name="timeOffMappingEntity">The time off mapping entity to update or add.</param>
        private async void AddorUpdateTimeOffMappingAsync(TimeOffMappingEntity timeOffMappingEntity)
        {
            await this.azureTableStorageHelper.InsertOrMergeTableEntityAsync(timeOffMappingEntity, "TimeOffMapping").ConfigureAwait(false);
        }

        /// <summary>
        /// Get All users for time off.
        /// </summary>
        /// <returns>A task.</returns>
        private async Task<IEnumerable<UserDetailsModel>> GetAllMappedUserDetailsAsync(string workForceIntegrationId)
        {
            List<UserDetailsModel> kronosUsers = new List<UserDetailsModel>();

            List<AllUserMappingEntity> mappedUsersResult = await this.userMappingProvider.GetAllMappedUserDetailsAsync().ConfigureAwait(false);

            foreach (var element in mappedUsersResult)
            {
                var teamMappingEntity = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(workForceIntegrationId, element.PartitionKey).ConfigureAwait(false);

                // If team department mapping for a user not present. Skip the user.
                if (teamMappingEntity != null)
                {
                    kronosUsers.Add(new UserDetailsModel
                    {
                        KronosPersonNumber = element.RowKey,
                        ShiftUserId = element.ShiftUserAadObjectId,
                        ShiftTeamId = teamMappingEntity.TeamId,
                        ShiftScheduleGroupId = teamMappingEntity.TeamsScheduleGroupId,
                        OrgJobPath = element.PartitionKey,
                        ShiftUserDisplayName = element.ShiftUserDisplayName,
                    });
                }
                else
                {
                    // Throwing an exception if teamMappingEntity is null.
                    throw new Exception(string.Format(CultureInfo.InvariantCulture, Resource.GenericNotAbleToRetrieveDataMessage, "GetAllMappedUserDetailsAsync"));
                }
            }

            return kronosUsers;
        }
    }
}