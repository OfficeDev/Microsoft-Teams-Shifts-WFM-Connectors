// <copyright file="OpenShiftRequestController.cs" company="Microsoft">
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
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShifts;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.RequestModels;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using OpenShiftRequest = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest;
    using OpenShiftResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift;

    /// <summary>
    /// Open Shift Requests controller.
    /// </summary>
    [Route("api/OpenShiftRequests")]
    [ApiController]
    [Authorize(Policy = "AppID")]
    public class OpenShiftRequestController : Controller
    {
        private const string OPENSHIFTCHANGEREQUESTAPPROVEURL = "teams/{0}/schedule/openshiftchangerequests/{1}/approve";
        private const string OPENSHIFTCHANGEREQUESTDECLINEURL = "teams/{0}/schedule/openshiftchangerequests/{1}/decline";

        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IOpenShiftActivity openShiftActivity;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IOpenShiftMappingEntityProvider openShiftMappingEntityProvider;
        private readonly Utility utility;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;

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
            IShiftMappingEntityProvider shiftMappingEntityProvider)
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
            this.utility = utility;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
        }

        /// <summary>
        /// Method to submit the Open Shift Request in Kronos.
        /// </summary>
        /// <param name="request">The request object that is coming in.</param>
        /// <param name="teamId">The Shifts team id.</param>
        /// <returns>Making sure to return a successful response code.</returns>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ShiftsIntegResponse> ProcessCreateOpenShiftRequestFromTeamsAsync(Models.IntegrationAPI.OpenShiftRequestIS request, string teamId)
        {
            ShiftsIntegResponse openShiftSubmitResponse;
            this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} starts at: {DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");

            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            var telemetryProps = new Dictionary<string, string>()
            {
                { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
                { "CallingMethod", "UpdateTeam" },
                { "OpenShiftId", request.OpenShiftId },
                { "OpenShiftRequestId", request.Id },
                { "RequesterId", request.SenderUserId },
            };

            var mappedTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsAsync(teamId).ConfigureAwait(false);
            var mappedTeam = mappedTeams.FirstOrDefault();

            var kronosTimeZone = string.IsNullOrEmpty(mappedTeam?.KronosTimeZone) ? this.appSettings.KronosTimeZone : mappedTeam.KronosTimeZone;
            var kronosTimeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(kronosTimeZone);

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            if (allRequiredConfigurations?.IsAllSetUpExists == false)
            {
                this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - {Resource.SetUpNotDoneMessage}");

                return ResponseHelper.CreateResponse(request.Id, StatusCodes.Status500InternalServerError, Resource.SetUpNotDoneCode, Resource.SetUpNotDoneMessage);
            }

            telemetryProps.Add("TenantId", allRequiredConfigurations.TenantId);
            telemetryProps.Add("WorkforceIntegrationId", allRequiredConfigurations.WFIId);

            var userMappingRecord = await this.GetMappedUserDetailsAsync(allRequiredConfigurations.WFIId, request?.SenderUserId, teamId).ConfigureAwait(false);
            if (string.IsNullOrEmpty(userMappingRecord.Error))
            {
                this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - {Resource.UserMappingNotFound}");

                return ResponseHelper.CreateResponse(request.Id, StatusCodes.Status500InternalServerError, userMappingRecord.Error, userMappingRecord.Error);
            }

            var teamDepartmentMapping = await this.teamDepartmentMappingProvider.GetTeamMappingForOrgJobPathAsync(allRequiredConfigurations.WFIId, userMappingRecord.OrgJobPath).ConfigureAwait(false);

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, $"teams/{teamDepartmentMapping.TeamId}/schedule/openShifts/{request.OpenShiftId}"))
            {
                var response = await httpClient.SendAsync(httpRequestMessage).ConfigureAwait(false);

                // Get the open shift entity from Teams as we need the start and end time as well
                // as all the activities.
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var graphOpenShift = JsonConvert.DeserializeObject<GraphOpenShift>(responseContent);

                    this.telemetryClient.TrackTrace($"OpenShiftRequestController - OpenShift Graph API call succeeded with getting the Open Shift: {graphOpenShift?.Id}");

                    var queryStartDate = graphOpenShift.SharedOpenShift.StartDateTime.AddDays(-100).ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);
                    var queryEndDate = graphOpenShift.SharedOpenShift.EndDateTime.AddDays(100).ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);
                    var openShiftReqQueryDateSpan = $"{queryStartDate}-{queryEndDate}";

                    // Create the open shift request in Kronos in draft state.
                    var createDraftOpenShiftRequestResponse = await this.CreateDraftOpenShiftRequestInKronos(graphOpenShift, userMappingRecord, kronosTimeZoneInfo, openShiftReqQueryDateSpan, allRequiredConfigurations).ConfigureAwait(false);

                    if (createDraftOpenShiftRequestResponse?.Status != ApiConstants.Success)
                    {
                        this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - Error creating the draft open shift request in Kronos: {createDraftOpenShiftRequestResponse?.Status}");

                        return ResponseHelper.CreateResponse(request.Id, StatusCodes.Status400BadRequest, Resource.KronosWFCOpenShiftRequestErrorCode, createDraftOpenShiftRequestResponse?.Status);
                    }

                    this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - Draft open shift request created: {createDraftOpenShiftRequestResponse?.Status}");

                    var kronosRequestId = createDraftOpenShiftRequestResponse.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.Id;

                    // Update the submitted Open Shift Request from DRAFT to SUBMITTED
                    var submitOpenShiftRequestResponse = await this.openShiftActivity.PostOpenShiftRequestStatusUpdateAsync(
                        userMappingRecord.KronosPersonNumber,
                        kronosRequestId,
                        openShiftReqQueryDateSpan,
                        Resource.KronosOpenShiftStatusUpdateToSubmittedMessage,
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession).ConfigureAwait(false);

                    if (submitOpenShiftRequestResponse?.Status != ApiConstants.Success)
                    {
                        this.telemetryClient.TrackTrace(Resource.ProcessCreateOpenShiftRequestFromTeamsAsync, telemetryProps);

                        return ResponseHelper.CreateResponse(request.Id, StatusCodes.Status400BadRequest, Resource.KronosWFCOpenShiftRequestErrorCode, submitOpenShiftRequestResponse?.Status);
                    }

                    this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - Open shift request submitted: {submitOpenShiftRequestResponse?.Status}");

                    var openShiftEntityWithKronosUniqueId = await this.openShiftMappingEntityProvider.GetOpenShiftMappingEntitiesAsync(request.OpenShiftId).ConfigureAwait(false);

                    // Insert the submitted open shift request into Azure table storage.
                    var openShiftRequestMappingEntity = CreateOpenShiftRequestMapping(request, userMappingRecord?.KronosPersonNumber, kronosRequestId, openShiftEntityWithKronosUniqueId.FirstOrDefault());

                    telemetryProps.Add("KronosRequestId", kronosRequestId);
                    telemetryProps.Add("KronosRequestStatus", submitOpenShiftRequestResponse.EmployeeRequestMgmt?.RequestItems?.EmployeeGlobalOpenShiftRequestItem?.StatusName);
                    telemetryProps.Add("KronosOrgJobPath", Utility.OrgJobPathKronosConversion(userMappingRecord?.OrgJobPath));

                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestMappingEntity).ConfigureAwait(false);

                    openShiftSubmitResponse = ResponseHelper.CreateSuccessResponse(request.Id, StatusCodes.Status200OK);
                }
                else
                {
                    this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} - There is an error when getting Open Shift: {request?.OpenShiftId} from Graph APIs: {response.StatusCode}");

                    return ResponseHelper.CreateResponse(request.Id, StatusCodes.Status404NotFound, Resource.OpenShiftNotFoundCode, $"The open shift no longer exists.");
                }
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessCreateOpenShiftRequestFromTeamsAsync} ends at: {DateTime.Now.ToString("O", CultureInfo.InvariantCulture)}");
            return openShiftSubmitResponse;
        }

        /// <summary>
        /// This method processes an approval that happens in the Shifts app
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="updateProps">A dictionary of string, string that will be logged to ApplicationInsights.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <param name="approved">Whether the </param>
        /// <returns>A unit of execution.</returns>
        public async Task<ShiftsIntegResponse> ProcessOpenShiftRequestApprovalFromTeams(
            RequestModel jsonModel,
            Dictionary<string, string> updateProps,
            string kronosTimeZone,
            bool approved)
        {
            this.telemetryClient.TrackTrace("Processing approval of OpenShiftRequests received from Shifts app", updateProps);

            var openShiftRequest = this.GetRequest<OpenShiftRequestIS>(jsonModel, "/openshiftrequests/", approved);
            var success = false;

            try
            {
                var openShiftRequestMapping = await this.openShiftRequestMappingEntityProvider.
                    GetOpenShiftRequestMappingEntityByOpenShiftRequestIdAsync(openShiftRequest.OpenShiftId, openShiftRequest.Id).ConfigureAwait(false);
                var kronosReqId = openShiftRequestMapping.KronosOpenShiftRequestId;
                var kronosUserId = openShiftRequestMapping.KronosPersonNumber;

                updateProps.Add("KronosPersonNumber", kronosUserId);
                updateProps.Add("OpenShiftRequestID", openShiftRequest.Id);
                updateProps.Add("KronosOpenShiftRequestId", kronosReqId);

                if (!approved)
                {
                    this.telemetryClient.TrackTrace($"Process denial of {openShiftRequest.Id}", updateProps);

                    // Deny in Kronos, Update mapping for Teams.
                    success = await this.ApproveOrDenyOpenShiftRequestInKronos(kronosReqId, kronosUserId, openShiftRequestMapping, approved).ConfigureAwait(false);
                    if (!success)
                    {
                        this.telemetryClient.TrackTrace($"Process failure of denial of {openShiftRequest.Id}", updateProps);
                        responseModelList.Add(GenerateResponse(openShiftRequest.Id, HttpStatusCode.BadRequest, null, null));
                        return responseModelList;
                    }

                    responseModelList.Add(GenerateResponse(openShiftRequest.Id, HttpStatusCode.OK, null, null));
                    openShiftRequestMapping.ShiftsStatus = ApiConstants.Refused;
                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestMapping).ConfigureAwait(false);
                    this.telemetryClient.TrackTrace($"Finished denial of {openShiftRequest.Id}", updateProps);
                    return responseModelList;
                }

                this.telemetryClient.TrackTrace($"Process approval of {openShiftRequest.Id}", updateProps);

                // approve in kronos
                success = await this.ApproveOrDenyOpenShiftRequestInKronos(kronosReqId, kronosUserId, openShiftRequestMapping, approved).ConfigureAwait(false);
                updateProps.Add("SuccessfullyApprovedInKronos", $"{success}");

                if (success)
                {
                    responseModelList.Add(GenerateResponse(openShiftRequest.Id, HttpStatusCode.OK, null, null));
                    var shift = this.Get<Shift>(jsonModel, "/shifts/", approved);
                    var shiftsTemp = await this.shiftController.GetShiftsForUser(kronosUserId, openShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                    var date = this.utility.UTCToKronosTimeZone(shift.SharedShift.StartDateTime, kronosTimeZone).ToString("d", CultureInfo.InvariantCulture);

                    // confirm new shift exists on kronos
                    var newKronosShift = shiftsTemp
                        .Schedule.ScheduleItems.ScheduleShift
                        .FirstOrDefault(x => x.Employee.FirstOrDefault().PersonNumber == kronosUserId && x.StartDate == date);

                    if (newKronosShift != null)
                    {
                        var openShift = this.Get<OpenShiftIS>(jsonModel, "/openshifts/", approved);
                        var kronosUniqueId = this.utility.CreateUniqueId(shift, kronosTimeZone);
                        var newShiftLinkEntity = this.shiftController.CreateNewShiftMappingEntity(shift, kronosUniqueId, kronosUserId);
                        await this.ApproveOpenShiftRequestInTables(openShiftRequest, openShift, responseModelList).ConfigureAwait(false);
                        await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(newShiftLinkEntity, shift.Id, openShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"Error during approval of {openShiftRequest.Id}", updateProps);
                        responseModelList.Add(GenerateResponse(openShiftRequest.Id, HttpStatusCode.NotFound, null, null));
                        return responseModelList;
                    }
                }

                this.telemetryClient.TrackTrace($"Finished approval of {openShiftRequest.Id}", updateProps);
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    this.telemetryClient.TrackTrace($"Shift mapping has failed for {openShiftRequest.Id}: " + ex.InnerException.ToString());
                }

                this.telemetryClient.TrackTrace($"Shift mapping has resulted in some type of error with the following: {ex.StackTrace.ToString(CultureInfo.InvariantCulture)}");
                throw;
            }

            this.telemetryClient.TrackTrace("Finished approval of processing OpenShiftRequests received from Shifts app", updateProps);
            return responseModelList;
        }

        /// <summary>
        /// Creates and sends the relevant request to approve or deny an open shift request.
        /// </summary>
        /// <param name="kronosReqId">The Kronos request id for the open shift request.</param>
        /// <param name="kronosUserId">The Kronos user id for the assigned user.</param>
        /// <param name="openShiftRequestMapping">The mapping for the open shift request.</param>
        /// <param name="approved">Whether the open shift should be approved (true) or denied (false).</param>
        /// <returns>Returns a bool that represents whether the request was a success (true) or not (false).</returns>
        internal async Task<bool> ApproveOrDenyOpenShiftRequestInKronos(
            string kronosReqId,
            string kronosUserId,
            AllOpenShiftRequestMappingEntity openShiftRequestMapping,
            bool approved)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests} start at: {DateTime.Now.ToString("o", provider)}");
            this.utility.SetQuerySpan(true, out var openShiftStartDate, out var openShiftEndDate);

            var openShiftQueryDateSpan = $"{openShiftStartDate}-{openShiftEndDate}";

            // Get all the necessary prerequisites.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(openShiftStartDate, openShiftEndDate);

            Dictionary<string, string> data = new Dictionary<string, string>
            {
                { "KronosPersonNumber", $"{kronosUserId}" },
                { "KronosOpenShiftRequestId", $"{kronosReqId}" },
                { "Approved", $"{approved}" },
                { "Configured correctly", $"{allRequiredConfigurations.IsAllSetUpExists}" },
                { "Correct date range", $"{isCorrectDateRange}" },
                { "Date range", $"{openShiftQueryDateSpan}" },
            };

            if ((bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                var response =
                    await this.openShiftActivity.ApproveOrDenyOpenShiftRequestsForUserAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        openShiftQueryDateSpan,
                        kronosUserId,
                        approved,
                        kronosReqId).ConfigureAwait(false);

                data.Add("ResponseStatus", $"{response.Status}");

                if (response.Status == "Success" && approved)
                {
                    this.telemetryClient.TrackTrace($"Update table for approval of {kronosReqId}", data);
                    openShiftRequestMapping.KronosStatus = ApiConstants.ApprovedStatus;
                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestMapping).ConfigureAwait(false);
                    return true;
                }

                if (response.Status == "Success" && !approved)
                {
                    this.telemetryClient.TrackTrace($"Update table for refusal of {kronosReqId}", data);
                    openShiftRequestMapping.KronosStatus = ApiConstants.Refused;
                    await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestMapping).ConfigureAwait(false);
                    return true;
                }
            }

            this.telemetryClient.TrackTrace("ApproveOrDenyOpenShiftRequestInKronos - Configuration incorrect", data);
            return false;
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

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                // Get all of the mapped users from Azure table storage.
                var mappedUsers = await this.GetAllMappedUsersDetailsAsync(allRequiredConfigurations?.WFIId).ConfigureAwait(false);
                foreach (var user in mappedUsers?.ToList())
                {
                    this.telemetryClient.TrackTrace($"Looking up user: {user.KronosPersonNumber}");
                    var approvedOrDeclinedOpenShiftRequests =
                        await this.openShiftActivity.GetApprovedOrDeclinedOpenShiftRequestsForUserAsync(
                            new Uri(allRequiredConfigurations.WfmEndPoint),
                            allRequiredConfigurations.KronosSession,
                            openShiftQueryDateSpan,
                            user.KronosPersonNumber).ConfigureAwait(false);

                    if (approvedOrDeclinedOpenShiftRequests == null)
                    {
                        throw new Exception(string.Format(CultureInfo.InvariantCulture, Resource.GenericNotAbleToRetrieveDataMessage, Resource.ProcessOpenShiftsAsync));
                    }
                    else if (approvedOrDeclinedOpenShiftRequests?.RequestMgmt?.RequestItems?.GlobalOpenShiftRequestItem == null)
                    {
                        // no open shift requests were returned for this user so skip them
                        continue;
                    }

                    // Iterate over each of the responseItems.
                    // 1. Mark the KronosStatus accordingly.
                    // If approved, create a temp shift, calculate the KronosHash, then make a call to the approval endpoint.
                    // If retract, or refused, then make a call to the decline endpoint.
                    foreach (var item in approvedOrDeclinedOpenShiftRequests.RequestMgmt.RequestItems.GlobalOpenShiftRequestItem)
                    {
                        // Update the status in Azure table storage.
                        var entityToUpdate = await this.openShiftRequestMappingEntityProvider.GetOpenShiftRequestMappingEntityByKronosReqIdAsync(
                            item.Id).ConfigureAwait(false);

                        if (entityToUpdate != null)
                        {
                            if (item.StatusName.Equals(ApiConstants.ApprovedStatus, StringComparison.OrdinalIgnoreCase) && entityToUpdate.ShiftsStatus.Equals(ApiConstants.Pending, StringComparison.OrdinalIgnoreCase))
                            {
                                // Update the KronosStatus to Approved here.
                                entityToUpdate.KronosStatus = ApiConstants.ApprovedStatus;

                                // Commit the change to the database.
                                await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(entityToUpdate).ConfigureAwait(false);

                                // Build the request to get the open shift from Graph.
                                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.ShiftsAccessToken);
                                var getUrl = string.Format(CultureInfo.InvariantCulture, OPENSHIFTURL, user.ShiftTeamId, entityToUpdate.TeamsOpenShiftId);
                                using (var getOpenShiftRequestMessage = new HttpRequestMessage(HttpMethod.Get, getUrl))
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
                                        var expectedShiftHash = this.utility.CreateUniqueId(openShiftObj, user.ShiftUserId, user.KronosTimeZone);

                                        this.telemetryClient.TrackTrace($"Hash From OpenShiftRequestController: {expectedShiftHash}");

                                        // 4. Create the shift entity from the open shift details, the user Id, and having a RowKey of SHFT_PENDING.
                                        var expectedShift = Utility.CreateShiftMappingEntity(
                                            user.ShiftUserId,
                                            expectedShiftHash,
                                            user.KronosPersonNumber);

                                        // 5. Calculate the necessary monthwise partitions - this is the partition key for the shift entity mapping table.
                                        var actualStartDateTimeStr = this.utility.CalculateStartDateTime(openShiftObj.SharedOpenShift.StartDateTime, user.KronosTimeZone).ToString("d", provider);
                                        var actualEndDateTimeStr = this.utility.CalculateEndDateTime(openShiftObj.SharedOpenShift.EndDateTime, user.KronosTimeZone).ToString("d", provider);
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
                                        var postUrl = string.Format(CultureInfo.InvariantCulture, OPENSHIFTCHANGEREQUESTAPPROVEURL, user.ShiftTeamId, entityToUpdate.RowKey);
                                        using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, postUrl)
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

                            if ((item.StatusName.Equals(ApiConstants.Retract, StringComparison.OrdinalIgnoreCase) || item.StatusName.Equals(ApiConstants.Refused, StringComparison.OrdinalIgnoreCase)) && entityToUpdate.ShiftsStatus.Equals(ApiConstants.Pending, StringComparison.OrdinalIgnoreCase))
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
                                var postUrl = string.Format(CultureInfo.InvariantCulture, OPENSHIFTCHANGEREQUESTDECLINEURL, user.ShiftTeamId, entityToUpdate.RowKey);
                                using (var declineRequestMessage = new HttpRequestMessage(HttpMethod.Post, postUrl)
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
                            this.telemetryClient.TrackTrace($"{Resource.ProcessOpenShiftsRequests}-Error: The data for {item.Id} does not exist in the table.");
                            continue;
                        }
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
        /// Method to create an Open Shift Mapping Entity to store in Azure Table storage.
        /// </summary>
        /// <param name="openShiftRequest">The open shift Wfi request.</param>
        /// <param name="kronosPersonNumber">The kronos number of the user that sent the request.</param>
        /// <param name="kronosRequestId">The Kronos Request ID.</param>
        /// <param name="openShiftEntity">The mapping for the open shift that has been requested.</param>
        /// <returns>An object of type <see cref="AllOpenShiftRequestMappingEntity"/>.</returns>
        private static AllOpenShiftRequestMappingEntity CreateOpenShiftRequestMapping(
            OpenShiftRequestIS openShiftRequest,
            string kronosPersonNumber,
            string kronosRequestId,
            AllOpenShiftMappingEntity openShiftEntity)
        {
            // Use the open shift entities month partition key
            return new AllOpenShiftRequestMappingEntity()
            {
                TeamsOpenShiftId = openShiftRequest.OpenShiftId,
                RowKey = openShiftRequest.Id,
                AadUserId = openShiftRequest.SenderUserId,
                KronosPersonNumber = kronosPersonNumber,
                KronosOpenShiftRequestId = kronosRequestId,
                KronosStatus = ApiConstants.Submitted,
                KronosOpenShiftUniqueId = openShiftEntity.RowKey,
                ShiftsStatus = ApiConstants.Pending,
                PartitionKey = openShiftEntity.PartitionKey,
            };
        }

        /// <summary>Creates and sends an open shift request to Kronos in draft state.</summary>
        /// <param name="graphOpenShift">The open shift object in Teams.</param>
        /// <param name="userMappingRecord">The user details.</param>
        /// <param name="kronosTimeZoneInfo">The Kronos time zone info.</param>
        /// <param name="openShiftRequestQueryDateSpan">The query date span for the request.</param>
        /// <param name="allRequiredConfigurations">The required configuration details.</param>
        /// <returns>An open shift request response.</returns>
        private async Task<OpenShiftRequest.Response> CreateDraftOpenShiftRequestInKronos(
            GraphOpenShift graphOpenShift,
            UserDetailsModel userMappingRecord,
            TimeZoneInfo kronosTimeZoneInfo,
            string openShiftRequestQueryDateSpan,
            SetupDetails allRequiredConfigurations)
        {
            // Build the Kronos shift segments using the activites returned from Teams
            var openShiftSegments = this.BuildKronosOpenShiftSegments(graphOpenShift, userMappingRecord.OrgJobPath, kronosTimeZoneInfo);

            var inputDraftOpenShiftRequest = new OpenShiftObj()
            {
                ShiftDate = TimeZoneInfo.ConvertTime(graphOpenShift.SharedOpenShift.StartDateTime, kronosTimeZoneInfo).ToString(Constants.DateFormat, CultureInfo.InvariantCulture).Replace('-', '/'),
                QueryDateSpan = openShiftRequestQueryDateSpan,
                PersonNumber = userMappingRecord?.KronosPersonNumber,
                OpenShiftSegments = openShiftSegments,
            };

            return await this.openShiftActivity.PostDraftOpenShiftRequestAsync(
                allRequiredConfigurations.TenantId,
                allRequiredConfigurations.KronosSession,
                inputDraftOpenShiftRequest,
                new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);
        }

        /// <summary>
        /// This method will build the Kronos Open Shift Segments from the activities of the Open Shift.
        /// </summary>
        /// <param name="graphOpenShift">The open shift to attempt to request for.</param>
        /// <param name="orgJobPath">The Kronos Org Job Path.</param>
        /// <param name="kronosTimeZoneInfo">The Kronos Time Zone Information.</param>
        /// <returns>A list of <see cref="ShiftSegment"/>.</returns>
        private List<OpenShiftResponse.ShiftSegment> BuildKronosOpenShiftSegments(GraphOpenShift graphOpenShift, string orgJobPath, TimeZoneInfo kronosTimeZoneInfo)
        {
            this.telemetryClient.TrackTrace($"BuildKronosOpenShiftSegments for the Org Job Path: {orgJobPath}, TeamsOpenShiftId = {graphOpenShift.Id}");

            var shiftSegmentList = new List<OpenShiftResponse.ShiftSegment>();

            var kronosOrgJob = Utility.OrgJobPathKronosConversion(orgJobPath);

            foreach (var item in graphOpenShift.SharedOpenShift.Activities)
            {
                // OrgJobPath represent a job in Kronos and so we do not want to give an orgJobPath value
                // for any 'BREAK' activites in Teams
                var segmentToAdd = new OpenShiftResponse.ShiftSegment
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

            List<AllUserMappingEntity> mappedUsersResult = await this.userMappingProvider.GetAllActiveMappedUserDetailsAsync().ConfigureAwait(false);

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