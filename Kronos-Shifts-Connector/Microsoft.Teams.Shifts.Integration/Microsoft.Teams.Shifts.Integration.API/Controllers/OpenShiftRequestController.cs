// <copyright file="OpenShiftRequestController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Reflection;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShifts;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.RequestModels;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;

    /// <summary>
    /// Open Shift Requests controller.
    /// </summary>
    [Route("api/OpenShiftRequests")]
    [ApiController]
    [Authorize(Policy = "AppID")]
    public class OpenShiftRequestController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IOpenShiftActivity openShiftActivity;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOpenShiftMappingEntityProvider openShiftMappingEntityProvider;
        private readonly string openShiftQueryDateSpan;
        private readonly Utility utility;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenShiftRequestController"/> class.
        /// </summary>
        /// <param name="appSettings">The key/value application settings DI.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="openShiftActivity">The open shift activity DI.</param>
        /// <param name="userMappingProvider">The user mapping provider DI.</param>
        /// <param name="teamDepartmentMappingProvider">The Team Department Mapping DI.</param>
        /// <param name="openShiftRequestMappingEntityProvider">The Open Shift Request Mapping DI.</param>
        /// <param name="httpClientFactory">http client.</param>
        /// <param name="openShiftMappingEntityProvider">The Open Shift Mapping DI.</param>
        /// <param name="utility">The common utility methods DI.</param>
        /// <param name="shiftMappingEntityProvider">Shift entity mapping provider DI.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public OpenShiftRequestController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IOpenShiftActivity openShiftActivity,
            IUserMappingProvider userMappingProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider,
            IHttpClientFactory httpClientFactory,
            IOpenShiftMappingEntityProvider openShiftMappingEntityProvider,
            Utility utility,
            IShiftMappingEntityProvider shiftMappingEntityProvider,
            BackgroundTaskWrapper taskWrapper)
        {
            if (appSettings is null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }

            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.openShiftActivity = openShiftActivity;
            this.userMappingProvider = userMappingProvider;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.openShiftRequestMappingEntityProvider = openShiftRequestMappingEntityProvider;
            this.openShiftMappingEntityProvider = openShiftMappingEntityProvider;
            this.httpClientFactory = httpClientFactory;
            this.openShiftQueryDateSpan = $"{this.appSettings.ShiftStartDate}-{this.appSettings.ShiftEndDate}";
            this.utility = utility;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// Method to submit the Open Shift Request in Kronos.
        /// </summary>
        /// <param name="request">The request object that is coming in.</param>
        /// <param name="teamsId">The Shifts team id.</param>
        /// <returns>Making sure to return a successful response code.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ShiftsIntegResponse> SubmitOpenShiftRequestToKronosAsync(
            Models.IntegrationAPI.OpenShiftRequestIS request, string teamsId)
        {
            ShiftsIntegResponse openShiftSubmitResponse;
            this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} starts at: {DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            GraphOpenShift graphOpenShift;
            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CallingMethod", "UpdateTeam" },
                { "OpenShiftId", request.OpenShiftId },
                { "OpenShiftRequestId", request.Id },
                { "RequesterId", request.SenderUserId },
            };

            // Prereq. Steps
            // Step 1 - Obtain the necessary prerequisites required.
            // Step 1a - Obtain the ConfigurationInfo entity.
            // Step 1b - Obtain the Team-Department Mapping.
            // Step 1c - Obtain the Graph Token and other prerequisite information.
            // Step 1d - Obtain the user from the user to user mapping table.
            // Step 1e - Login to Kronos.

            // Step 1c.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            var kronosTimeZoneId = this.appSettings.KronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZoneId);

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                // Step 1d.
                var userMappingRecord = await this.GetMappedUserDetailsAsync(
                    allRequiredConfigurations.WFIId,
                    request?.SenderUserId,
                    teamsId).ConfigureAwait(false);

                // Submit open shift request to Kronos if user and it's corresponding team is mapped correctly.
                if (string.IsNullOrEmpty(userMappingRecord.Error))
                {
                    var queryingOrgJobPath = userMappingRecord.OrgJobPath;
                    var teamDepartmentMapping = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(
                        allRequiredConfigurations.WFIId,
                        queryingOrgJobPath).ConfigureAwait(false);

                    telemetryProps.Add("TenantId", allRequiredConfigurations.TenantId);
                    telemetryProps.Add("WorkforceIntegrationId", allRequiredConfigurations.WFIId);

                    // Step 2 - Getting the Open Shift - the start date/time and end date/time are needed.
                    var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);
                    using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, "teams/" + teamDepartmentMapping.TeamId + "/schedule/openShifts/" + request.OpenShiftId))
                    {
                        var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                        if (response.IsSuccessStatusCode)
                        {
                            var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                            graphOpenShift = JsonConvert.DeserializeObject<GraphOpenShift>(responseContent);

                            // Logging the required Open Shift ID from the Graph call.
                            this.telemetryClient.TrackTrace($"OpenShiftRequestController - OpenShift Graph API call succeeded with getting the Open Shift: {graphOpenShift?.Id}");

                            var shiftStartDate = graphOpenShift.SharedOpenShift.StartDateTime.AddDays(
                                                -Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                                .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                            var shiftEndDate = graphOpenShift.SharedOpenShift.EndDateTime.AddDays(
                                               Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                               .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                            var openShiftReqQueryDateSpan = $"{shiftStartDate}-{shiftEndDate}";

                            // Builds out the Open Shift Segments prior to actual object being built.
                            // Take into account the activities of the open shift that has been retrieved
                            // from the Graph API call for open shift details.
                            var openShiftSegments = this.BuildKronosOpenShiftSegments(
                                    graphOpenShift.SharedOpenShift.Activities,
                                    queryingOrgJobPath,
                                    kronosTimeZoneInfo,
                                    graphOpenShift.Id);

                            // Step 3 - Create the necessary OpenShiftObj
                            // Having the open shift segments which have been retrieved.
                            var inputDraftOpenShiftRequest = new OpenShiftObj()
                            {
                                StartDayNumber = Constants.StartDayNumberString,
                                EndDayNumber = Constants.EndDayNumberString,
                                SegmentTypeName = Resource.DraftOpenShiftRequestSegmentTypeName,
                                StartTime = TimeZoneInfo.ConvertTime(graphOpenShift.SharedOpenShift.StartDateTime, kronosTimeZoneInfo).ToString("h:mm tt", CultureInfo.InvariantCulture),
                                EndTime = TimeZoneInfo.ConvertTime(graphOpenShift.SharedOpenShift.EndDateTime, kronosTimeZoneInfo).ToString("h:mm tt", CultureInfo.InvariantCulture),
                                ShiftDate = TimeZoneInfo.ConvertTime(graphOpenShift.SharedOpenShift.StartDateTime, kronosTimeZoneInfo).ToString(Constants.DateFormat, CultureInfo.InvariantCulture).Replace('-', '/'),
                                OrgJobPath = Utility.OrgJobPathKronosConversion(userMappingRecord?.OrgJobPath),
                                QueryDateSpan = openShiftReqQueryDateSpan,
                                PersonNumber = userMappingRecord?.KronosPersonNumber,
                                OpenShiftSegments = openShiftSegments,
                            };

                            // Step 4 - Submit over to Kronos WFC, Open Shift Request goes into DRAFT state.
                            var postDraftOpenShiftRequestResult = await this.openShiftActivity.PostDraftOpenShiftRequestAsync(
                                allRequiredConfigurations.TenantId,
                                allRequiredConfigurations.KronosSession,
                                inputDraftOpenShiftRequest,
                                new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

                            if (postDraftOpenShiftRequestResult?.Status == ApiConstants.Success)
                            {
                                this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} - Operation to submit the DRAFT request has succeeded with: {postDraftOpenShiftRequestResult?.Status}");

                                // Step 5 - Update the submitted Open Shift Request from DRAFT to SUBMITTED
                                // so that it renders inside of the Kronos WFC Request Manager for final approval/decline.
                                var postUpdateOpenShiftRequestStatusResult = await this.openShiftActivity.PostOpenShiftRequestStatusUpdateAsync(
                                    userMappingRecord.KronosPersonNumber,
                                    postDraftOpenShiftRequestResult.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.Id,
                                    openShiftReqQueryDateSpan,
                                    Resource.KronosOpenShiftStatusUpdateToSubmittedMessage,
                                    new Uri(allRequiredConfigurations.WfmEndPoint),
                                    allRequiredConfigurations.KronosSession).ConfigureAwait(false);

                                if (postUpdateOpenShiftRequestStatusResult?.Status == ApiConstants.Success)
                                {
                                    this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} - Operation to update the DRAFT request to SUBMITTED has succeeded with: {postUpdateOpenShiftRequestStatusResult?.Status}");

                                    var openShiftEntityWithKronosUniqueId = await this.openShiftMappingEntityProvider.GetOpenShiftMappingEntitiesAsync(
                                        request.OpenShiftId).ConfigureAwait(false);

                                    // Step 6 - Insert the submitted open shift request into Azure table storage.
                                    // Ensuring to pass the monthwise partition key from the Open Shift as the partition key for the Open Shift
                                    // Request mapping entity.
                                    var openShiftRequestMappingEntity = CreateOpenShiftRequestMapping(
                                        request?.OpenShiftId,
                                        request?.Id,
                                        request?.SenderUserId,
                                        userMappingRecord?.KronosPersonNumber,
                                        postDraftOpenShiftRequestResult.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.Id,
                                        ApiConstants.Submitted,
                                        openShiftEntityWithKronosUniqueId.FirstOrDefault().RowKey,
                                        openShiftEntityWithKronosUniqueId.FirstOrDefault().PartitionKey,
                                        ApiConstants.Pending);

                                    telemetryProps.Add(
                                        "KronosRequestId",
                                        postDraftOpenShiftRequestResult.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.Id);
                                    telemetryProps.Add(
                                        "KronosRequestStatus",
                                        postUpdateOpenShiftRequestStatusResult.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.StatusName);
                                    telemetryProps.Add("KronosOrgJobPath", Utility.OrgJobPathKronosConversion(userMappingRecord?.OrgJobPath));

                                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestMappingEntity).ConfigureAwait(false);

                                    this.telemetryClient.TrackTrace(Resource.SubmitOpenShiftRequestToKronosAsync, telemetryProps);

                                    openShiftSubmitResponse = new ShiftsIntegResponse()
                                    {
                                        Id = request.Id,
                                        Status = StatusCodes.Status200OK,
                                        Body = new Body
                                        {
                                            Error = null,
                                            ETag = GenerateNewGuid(),
                                        },
                                    };
                                }
                                else
                                {
                                    this.telemetryClient.TrackTrace(Resource.SubmitOpenShiftRequestToKronosAsync, telemetryProps);
                                    openShiftSubmitResponse = new ShiftsIntegResponse()
                                    {
                                        Id = request.Id,
                                        Status = StatusCodes.Status500InternalServerError,
                                        Body = new Body
                                        {
                                            Error = new ResponseError
                                            {
                                                Code = Resource.KronosWFCOpenShiftRequestErrorCode,
                                                Message = postUpdateOpenShiftRequestStatusResult?.Status,
                                            },
                                        },
                                    };
                                }
                            }
                            else
                            {
                                this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} - There was an error from Kronos WFC when updating the DRAFT request to SUBMITTED status: {postDraftOpenShiftRequestResult?.Status}");

                                openShiftSubmitResponse = new ShiftsIntegResponse()
                                {
                                    Id = request.Id,
                                    Status = StatusCodes.Status500InternalServerError,
                                    Body = new Body
                                    {
                                        Error = new ResponseError
                                        {
                                            Code = Resource.KronosWFCOpenShiftRequestErrorCode,
                                            Message = postDraftOpenShiftRequestResult?.Status,
                                        },
                                    },
                                };
                            }
                        }
                        else
                        {
                            this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} - There is an error when getting Open Shift: {request?.OpenShiftId} from Graph APIs: {response.StatusCode.ToString()}");

                            openShiftSubmitResponse = new ShiftsIntegResponse
                            {
                                Id = request.Id,
                                Status = StatusCodes.Status404NotFound,
                                Body = new Body
                                {
                                    Error = new ResponseError()
                                    {
                                        Code = Resource.OpenShiftNotFoundCode,
                                        Message = string.Format(CultureInfo.InvariantCulture, Resource.OpenShiftNotFoundMessage, request.OpenShiftId),
                                    },
                                },
                            };
                        }
                    }
                }

                // Either user or it's team is not mapped correctly.
                else
                {
                    openShiftSubmitResponse = new ShiftsIntegResponse
                    {
                        Id = request.Id,
                        Status = StatusCodes.Status500InternalServerError,
                        Body = new Body
                        {
                            Error = new ResponseError
                            {
                                Code = userMappingRecord.Error,
                                Message = userMappingRecord.Error,
                            },
                        },
                    };
                }
            }
            else
            {
                this.telemetryClient.TrackTrace(Resource.SubmitOpenShiftRequestToKronosAsync + "-" + Resource.SetUpNotDoneMessage);

                openShiftSubmitResponse = new ShiftsIntegResponse
                {
                    Id = request.Id,
                    Status = StatusCodes.Status500InternalServerError,
                    Body = new Body
                    {
                        Error = new ResponseError
                        {
                            Code = Resource.SetUpNotDoneCode,
                            Message = Resource.SetUpNotDoneMessage,
                        },
                    },
                };
            }

            this.telemetryClient.TrackTrace($"{Resource.SubmitOpenShiftRequestToKronosAsync} ends at: {DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");
            return openShiftSubmitResponse;
        }

        /// <summary>
        /// This method will Process the Open Shift requests accordingly.
        /// </summary>
        /// <param name="isRequestFromLogicApp">The value indicating whether or not the request is coming from logic app.</param>
        /// <returns>A unit of execution.</returns>
        internal async Task ProcessOpenShiftsRequests(string isRequestFromLogicApp)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} start at: {DateTime.Now.ToString("o", provider)}");
            this.utility.SetQuerySpan(Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture), out var openShiftStartDate, out var openShiftEndDate);

            var openShiftQueryDateSpan = $"{openShiftStartDate}-{openShiftEndDate}";

            // Get all the necessary prerequisites.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(openShiftStartDate, openShiftEndDate);

            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                // Get all of the mapped users from Azure table storage.
                var mappedUsers = await this.GetAllMappedUsersDetailsAsync(allRequiredConfigurations?.WFIId).ConfigureAwait(false);
                var mappedUsersList = mappedUsers?.ToList();

                foreach (var user in mappedUsersList)
                {
                    this.telemetryClient.TrackTrace($"Looking up user: {user.KronosPersonNumber}");
                    var approvedOrDeclinedOpenShiftRequests =
                        await this.openShiftActivity.GetApprovedOrDeclinedOpenShiftRequestsForUserAsync(
                            new Uri(allRequiredConfigurations.WfmEndPoint),
                            allRequiredConfigurations.KronosSession,
                            openShiftQueryDateSpan,
                            user.KronosPersonNumber).ConfigureAwait(false);

                    var responseItems = approvedOrDeclinedOpenShiftRequests.RequestMgmt.RequestItems.GlobalOpenShiftRequestItem;

                    if (approvedOrDeclinedOpenShiftRequests != null)
                    {
                        // Iterate over each of the responseItems.
                        // 1. Mark the KronosStatus accordingly.
                        // If approved, create a temp shift, calculate the KronosHash, then make a call to the approval endpoint.
                        // If retract, or refused, then make a call to the decline endpoint.
                        foreach (var item in responseItems)
                        {
                            // Update the status in Azure table storage.
                            var entityToUpdate = await this.openShiftRequestMappingEntityProvider.GetOpenShiftRequestMappingEntityByKronosReqIdAsync(
                                item.Id).ConfigureAwait(false);

                            if (entityToUpdate != null)
                            {
                                if (item.StatusName == ApiConstants.ApprovedStatus && entityToUpdate.ShiftsStatus == ApiConstants.Pending)
                                {
                                    // Update the KronosStatus to Approved here.
                                    entityToUpdate.KronosStatus = ApiConstants.ApprovedStatus;

                                    // Commit the change to the database.
                                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(entityToUpdate).ConfigureAwait(false);

                                    // Build the request to get the open shift from Graph.
                                    var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);
                                    using (var getOpenShiftRequestMessage = new HttpRequestMessage(HttpMethod.Get, "teams/" + user.ShiftTeamId + "/schedule/openShifts/" + entityToUpdate.TeamsOpenShiftId))
                                    {
                                        var getOpenShiftResponse = await httpClient.SendAsync(getOpenShiftRequestMessage).ConfigureAwait(false);
                                        if (getOpenShiftResponse.IsSuccessStatusCode)
                                        {
                                            // Calculate the expected Shift hash prior to making the approval call.
                                            // 1. Have to get the response string.
                                            var getOpenShiftResponseStr = await getOpenShiftResponse.Content.ReadAsStringAsync().ConfigureAwait(false);

                                            // 2. Deserialize the response string into the Open Shift object.
                                            var openShiftObj = JsonConvert.DeserializeObject<GraphOpenShift>(getOpenShiftResponseStr);

                                            // 3. Creating the expected shift hash from the above and the userId.
                                            var expectedShiftHash = this.utility.CreateUniqueId(openShiftObj, user.ShiftUserId);

                                            this.telemetryClient.TrackTrace($"Hash From OpenShiftRequestController: {expectedShiftHash}");

                                            // 4. Create the shift entity from the open shift details, the user Id, and having a RowKey of SHFT_PENDING.
                                            var expectedShift = Utility.CreateShiftMappingEntity(
                                                user.ShiftUserId,
                                                expectedShiftHash,
                                                user.KronosPersonNumber);

                                            // 5. Calculate the necessary monthwise partitions - this is the partition key for the shift entity mapping table.
                                            var actualStartDateTimeStr = this.utility.CalculateStartDateTime(openShiftObj.SharedOpenShift.StartDateTime).ToString("d", provider);
                                            var actualEndDateTimeStr = this.utility.CalculateEndDateTime(openShiftObj.SharedOpenShift.EndDateTime).ToString("d", provider);
                                            var monthPartitions = Utility.GetMonthPartition(actualStartDateTimeStr, actualEndDateTimeStr);
                                            var monthPartition = monthPartitions?.FirstOrDefault();

                                            var rowKey = $"SHFT_PENDING_{entityToUpdate.RowKey}";

                                            this.telemetryClient.TrackTrace("OpenShiftRequestId = " + rowKey);

                                            // 6. Insert into the Shift Mapping Entity table.
                                            await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(
                                                expectedShift,
                                                rowKey,
                                                monthPartition).ConfigureAwait(false);

                                            // 7. Having the necessary Graph API call made here - to the approval endpoint.
                                            var approvalMessageModel = new OpenShiftRequestApproval
                                            {
                                                Message = Resource.OpenShiftRequestApprovalMessage,
                                            };

                                            var approvalMessageModelStr = JsonConvert.SerializeObject(approvalMessageModel);
                                            var approvalHttpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                                            approvalHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);

                                            // Send Passthrough header to indicate the sender of request in outbound call.
                                            approvalHttpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfigurations.WFIId);
                                            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + user.ShiftTeamId + "/schedule/openshiftchangerequests/" + entityToUpdate.RowKey + "/approve")
                                            {
                                                Content = new StringContent(approvalMessageModelStr, Encoding.UTF8, "application/json"),
                                            })
                                            {
                                                var approvalHttpResponse = await approvalHttpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);
                                                if (approvalHttpResponse.IsSuccessStatusCode)
                                                {
                                                    this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} ShiftRequestId: {entityToUpdate.RowKey} KronosRequestId: {item.Id}");
                                                }
                                                else
                                                {
                                                    this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} -Error The service is acting on Open Shift Request: {entityToUpdate?.RowKey} which may have been approved");
                                                }
                                            }
                                        }
                                        else
                                        {
                                            // Output to AppInsights logging.
                                            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} - Getting Open Shift for {entityToUpdate.TeamsOpenShiftId} results in {getOpenShiftResponse.Content.ToString()}");
                                        }
                                    }
                                }

                                if ((item.StatusName == ApiConstants.Retract || item.StatusName == ApiConstants.Refused) && entityToUpdate.ShiftsStatus == ApiConstants.Pending)
                                {
                                    entityToUpdate.KronosStatus = item.StatusName;
                                    entityToUpdate.ShiftsStatus = item.StatusName;

                                    // Commit the change to the database.
                                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(entityToUpdate).ConfigureAwait(false);

                                    var declineMessageModel = new OpenShiftRequestApproval
                                    {
                                        Message = Resource.OpenShiftRequestDeclinedMessage,
                                    };

                                    var declineMessageModelStr = JsonConvert.SerializeObject(declineMessageModel);
                                    var declineHttpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                                    declineHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);

                                    // Send Passthrough header to indicate the sender of request in outbound call.
                                    declineHttpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfigurations.WFIId);
                                    using (var declineRequestMessage = new HttpRequestMessage(HttpMethod.Post, "teams/" + user.ShiftTeamId + "/schedule/openshiftchangerequests/" + entityToUpdate.RowKey + "/decline")
                                    {
                                        Content = new StringContent(declineMessageModelStr, Encoding.UTF8, "application/json"),
                                    })
                                    {
                                        var declineHttpResponse = await declineHttpClient.SendAsync(declineRequestMessage).ConfigureAwait(false);
                                        if (declineHttpResponse.IsSuccessStatusCode)
                                        {
                                            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests}- DeclinedShiftRequestId: {entityToUpdate.RowKey}, DeclinedKronosRequestId: {item.Id}");
                                        }
                                        else
                                        {
                                            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} -Error: The service is acting on Open Shift Request: {entityToUpdate?.RowKey} which may have been approved or declined.");
                                        }
                                    }
                                }
                                else
                                {
                                    this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests}: The {item.Id} Kronos Open Shift request is in {item.StatusName} status.");
                                }
                            }
                            else
                            {
                                this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests}-Error: The data for {item.Id} does not exist in the database.");
                                continue;
                            }
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format(CultureInfo.InvariantCulture, Resource.GenericNotAbleToRetrieveDataMessage, Resource.ProcessOpenShiftsAsync));
                    }
                }
            }
            else
            {
                this.telemetryClient.TrackTrace(Resource.ProcessOpenShiftsRequests + "-" + Resource.SetUpNotDoneMessage);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} end at: {DateTime.Now.ToString("O", provider)}");
        }

        /// <summary>
        /// This method returns a random string.
        /// </summary>
        /// <returns>A random GUID that would represent an eTag.</returns>
        private static string GenerateNewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// Method to create an Open Shift Mapping Entity to store in Azure Table storage.
        /// </summary>
        /// <param name="openShiftId">The Graph Open Shift ID.</param>
        /// <param name="shiftRequestId">The incoming Shift Request ID.</param>
        /// <param name="teamsUserAadObjectId">The User AAD Object ID that made the Open Shift Request.</param>
        /// <param name="kronosPersonNumber">The Kronos Person Number.</param>
        /// <param name="kronosRequestId">The Kronos Request ID.</param>
        /// <param name="kronosRequestStatus">The Kronos Request status.</param>
        /// <param name="kronosUniqueId">The system generated ID.</param>
        /// <param name="monthPartitionKey">The month partition key of the requested Open Shift.</param>
        /// <param name="openShiftRequestStatus">The Open Shift Request status from the incoming request.</param>
        /// <returns>An object of type <see cref="AllOpenShiftRequestMappingEntity"/>.</returns>
        private static AllOpenShiftRequestMappingEntity CreateOpenShiftRequestMapping(
            string openShiftId,
            string shiftRequestId,
            string teamsUserAadObjectId,
            string kronosPersonNumber,
            string kronosRequestId,
            string kronosRequestStatus,
            string kronosUniqueId,
            string monthPartitionKey,
            string openShiftRequestStatus)
        {
            // Forming the open shift request mapping entity with the parameters defined,
            // and using the month partition key of the open shift entity as the month
            // partition key of the open shift request entity.
            var openShiftRequestMappingEntity = new AllOpenShiftRequestMappingEntity()
            {
                TeamsOpenShiftId = openShiftId,
                RowKey = shiftRequestId,
                AadUserId = teamsUserAadObjectId,
                KronosPersonNumber = kronosPersonNumber,
                KronosOpenShiftRequestId = kronosRequestId,
                KronosStatus = kronosRequestStatus,
                KronosOpenShiftUniqueId = kronosUniqueId,
                ShiftsStatus = openShiftRequestStatus,
                PartitionKey = monthPartitionKey,
            };

            return openShiftRequestMappingEntity;
        }

        /// <summary>
        /// This method will build the Kronos Open Shift Segments from the activities of the Open Shift.
        /// </summary>
        /// <param name="activities">The list of open shift activities.</param>
        /// <param name="orgJobPath">The Kronos Org Job Path.</param>
        /// <param name="kronosTimeZoneInfo">The Kronos Time Zone Information.</param>
        /// <param name="openShiftId">The MS Graph Open Shift ID.</param>
        /// <returns>A list of <see cref="ShiftSegment"/>.</returns>
        private List<App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment> BuildKronosOpenShiftSegments(
            List<Activity> activities,
            string orgJobPath,
            TimeZoneInfo kronosTimeZoneInfo,
            string openShiftId)
        {
            this.telemetryClient.TrackTrace($"BuildKronosOpenShiftSegments start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            this.telemetryClient.TrackTrace($"BuildKronosOpenShiftSegments for the Org Job Path: {orgJobPath}, TeamsOpenShiftId = {openShiftId}");

            var shiftSegmentList = new List<App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment>();

            // Ensuring to format the org job path from database in Kronos WFC format.
            var kronosOrgJob = Utility.OrgJobPathKronosConversion(orgJobPath);

            foreach (var item in activities)
            {
                // The code below will construct an object of type ShiftSegment, and checks
                // if the current item in the iteration is a BREAK activity. If the current item
                // is a BREAK, then the Kronos Org Job Path should not be included.
                var segmentToAdd = new App.KronosWfc.Models.ResponseEntities.OpenShift.ShiftSegment
                {
                    OrgJobPath = item.DisplayName == "BREAK" ? null : kronosOrgJob,
                    EndDayNumber = "1",
                    StartDayNumber = "1",
                    StartTime = TimeZoneInfo.ConvertTime(item.StartDateTime, kronosTimeZoneInfo).ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    EndTime = TimeZoneInfo.ConvertTime(item.EndDateTime, kronosTimeZoneInfo).ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    SegmentTypeName = item.DisplayName,
                };

                shiftSegmentList.Add(segmentToAdd);
            }

            this.telemetryClient.TrackTrace($"BuildKronosOpenShiftSegments end at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return shiftSegmentList;
        }

        /// <summary>
        /// Gets a user model from Shifts in order to return the Org Job Path.
        /// </summary>
        /// <returns>A task.</returns>
        private async Task<UserDetailsModel> GetMappedUserDetailsAsync(
            string workForceIntegrationId,
            string userAadObjectId,
            string teamsId)
        {
            UserDetailsModel kronosUser;
            AllUserMappingEntity mappedUsersResult = await this.userMappingProvider.GetUserMappingEntityAsyncNew(userAadObjectId, teamsId).ConfigureAwait(false);
            if (mappedUsersResult != null)
            {
                var teamMappingEntity = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(
                       workForceIntegrationId,
                       mappedUsersResult.PartitionKey).ConfigureAwait(false);
                if (teamMappingEntity != null)
                {
                    kronosUser = new UserDetailsModel()
                    {
                        KronosPersonNumber = mappedUsersResult.RowKey,
                        ShiftUserId = mappedUsersResult.ShiftUserAadObjectId,
                        ShiftTeamId = teamsId,
                        ShiftScheduleGroupId = teamMappingEntity.TeamsScheduleGroupId,
                        OrgJobPath = mappedUsersResult.PartitionKey,
                    };
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Team id {teamsId} is not mapped.");

                    kronosUser = new UserDetailsModel()
                    {
                        Error = Resource.UserTeamNotExists,
                    };
                }
            }
            else
            {
                this.telemetryClient.TrackTrace($"User id {userAadObjectId} is not mapped.");

                kronosUser = new UserDetailsModel()
                {
                    Error = Resource.UserMappingNotFound,
                };
            }

            return kronosUser;
        }

        private async Task<List<UserDetailsModel>> GetAllMappedUsersDetailsAsync(
            string workForceIntegrationId)
        {
            List<UserDetailsModel> kronosUsers = new List<UserDetailsModel>();

            List<AllUserMappingEntity> mappedUsersResult = await this.userMappingProvider.GetAllMappedUserDetailsAsync().ConfigureAwait(false);

            foreach (var element in mappedUsersResult)
            {
                var teamMappingEntity = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(
                    workForceIntegrationId,
                    element.PartitionKey).ConfigureAwait(false);

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
                    });
                }
            }

            return kronosUsers;
        }
    }
}