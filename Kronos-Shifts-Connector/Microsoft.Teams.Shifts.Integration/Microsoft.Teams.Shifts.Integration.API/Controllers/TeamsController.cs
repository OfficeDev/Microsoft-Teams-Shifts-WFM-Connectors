// <copyright file="TeamsController.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.Shifts.Encryption.Encryptors;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using static Microsoft.Teams.Shifts.Integration.API.Common.ResponseHelper;

    /// <summary>
    /// This is the teams controller that is being used here.
    /// </summary>
    [Route("/v1/teams")]
    [ApiController]
    public class TeamsController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly IConfigurationProvider configurationProvider;
        private readonly OpenShiftRequestController openShiftRequestController;
        private readonly SwapShiftController swapShiftController;
        private readonly ShiftController shiftController;
        private readonly TimeOffController timeOffController;
        private readonly SwapShiftEligibilityController swapShiftEligibilityController;
        private readonly Common.Utility utility;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider;
        private readonly IOpenShiftMappingEntityProvider openShiftMappingEntityProvider;
        private readonly ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly ITimeOffReasonProvider timeOffReasonProvider;
        private readonly ITimeOffMappingEntityProvider timeOffReqMappingEntityProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsController"/> class.
        /// </summary>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="configurationProvider">ConfigurationProvider DI.</param>
        /// <param name="openShiftRequestController">OpenShiftRequestController DI.</param>
        /// <param name="swapShiftController">SwapShiftController DI.</param>
        /// <param name="shiftController">ShiftController DI.</param>
        /// <param name="timeOffController">TimeOffCntroller DI.</param>
        /// <param name="swapShiftEligibilityController">Swap Shift Eligibility Controller DI.</param>
        /// <param name="utility">The common utility methods DI.</param>
        /// <param name="userMappingProvider">The user mapping provider DI.</param>
        /// <param name="shiftMappingEntityProvider">The shift entity mapping provider DI.</param>
        /// <param name="openShiftRequestMappingEntityProvider">The open shift request mapping entity provider DI.</param>
        /// <param name="openShiftMappingEntityProvider">The open shift mapping entity provider DI.</param>
        /// <param name="swapShiftMappingEntityProvider">The swap shift mapping entity provider DI.</param>
        /// <param name="teamDepartmentMappingProvider">The team department mapping entity provider DI.</param>
        /// <param name="timeOffReasonProvider">Paycodes to Time Off Reasons Mapping provider.</param>
        /// <param name="timeOffReqMappingEntityProvider">time off entity provider.</param>
        public TeamsController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider,
            OpenShiftRequestController openShiftRequestController,
            SwapShiftController swapShiftController,
            ShiftController shiftController,
            TimeOffController timeOffController,
            SwapShiftEligibilityController swapShiftEligibilityController,
            Common.Utility utility,
            IUserMappingProvider userMappingProvider,
            IShiftMappingEntityProvider shiftMappingEntityProvider,
            IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider,
            IOpenShiftMappingEntityProvider openShiftMappingEntityProvider,
            ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            ITimeOffReasonProvider timeOffReasonProvider,
            ITimeOffMappingEntityProvider timeOffReqMappingEntityProvider)
        {
            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
            this.openShiftRequestController = openShiftRequestController;
            this.swapShiftController = swapShiftController;
            this.shiftController = shiftController;
            this.timeOffController = timeOffController;
            this.utility = utility;
            this.userMappingProvider = userMappingProvider;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
            this.openShiftRequestMappingEntityProvider = openShiftRequestMappingEntityProvider;
            this.openShiftMappingEntityProvider = openShiftMappingEntityProvider;
            this.swapShiftMappingEntityProvider = swapShiftMappingEntityProvider;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.timeOffReasonProvider = timeOffReasonProvider;
            this.timeOffReqMappingEntityProvider = timeOffReqMappingEntityProvider;
            this.swapShiftEligibilityController = swapShiftEligibilityController;
        }

        /// <summary>
        /// Method to update the Workforce Integration ID to the schedule.
        /// </summary>
        /// <returns>A unit of execution that contains the HTTP Response.</returns>
        [HttpGet]
        [Route("/api/teams/CheckSetup")]
        public async Task<HttpResponseMessage> CheckSetupAsync()
        {
            HttpResponseMessage httpResponseMessage = new HttpResponseMessage();

            var isSetUpDone = await this.utility.IsSetUpDoneAsync().ConfigureAwait(false);

            // Check for all the setup i.e User to user mapping, team department mapping, user logged in to configuration web app
            if (isSetUpDone)
            {
                httpResponseMessage.StatusCode = HttpStatusCode.OK;
            }
            else
            {
                httpResponseMessage.StatusCode = HttpStatusCode.InternalServerError;
            }

            return httpResponseMessage;
        }

        /// <summary>
        /// The method that will be called from Shifts.
        /// </summary>
        /// <param name="aadGroupId">The AAD Group Id for the Team.</param>
        /// <returns>An action result.</returns>
        [HttpPost]
        [Route("/v1/teams/{aadGroupId}/update")]
        public async Task<ActionResult> UpdateTeam([FromRoute] string aadGroupId)
        {
            var request = this.Request;
            var requestHeaders = request.Headers;
            Microsoft.Extensions.Primitives.StringValues passThroughValue = string.Empty;

            this.telemetryClient.TrackTrace("IncomingRequest, starts for method: UpdateTeam - " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

            // Step 1 - Obtain the secret from the database.
            var configurationEntities = await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false);
            var configurationEntity = configurationEntities?.FirstOrDefault();

            // Check whether Request coming from correct workforce integration is present and is equal to workforce integration id.
            // Request is valid for OpenShift and SwapShift request FLM approval when the value of X-MS-WFMRequest coming
            // from correct workforce integration is equal to current workforce integration id.
            var isRequestFromCorrectIntegration = requestHeaders.TryGetValue("X-MS-WFMPassthrough", out passThroughValue) &&
                                           string.Equals(passThroughValue, configurationEntity.WorkforceIntegrationId, StringComparison.Ordinal);

            // Step 2 - Create/declare the byte arrays, and other data types required.
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(configurationEntity?.WorkforceIntegrationSecret);

            // Step 3 - Extract the incoming request using the symmetric key, and the HttpRequest.
            var jsonModel = await DecryptEncryptedRequestFromShiftsAsync(
                secretKeyBytes,
                this.Request).ConfigureAwait(false);

            IntegrationApiResponseModel responseModel = new IntegrationApiResponseModel();
            ShiftsIntegResponse integrationResponse;
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            string responseModelStr = string.Empty;

            var updateProps = new Dictionary<string, string>()
            {
                { "IncomingAadGroupId", aadGroupId },
            };

            // get the kronos org job mapping information for the team and get the timezone for the first mapping
            // logically all mappings for the team must have the same time zone set
            var mappedTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsAsync(aadGroupId).ConfigureAwait(false);
            var mappedTeam = mappedTeams.FirstOrDefault();
            var kronosTimeZone = string.IsNullOrEmpty(mappedTeam?.KronosTimeZone) ? this.appSettings.KronosTimeZone : mappedTeam.KronosTimeZone;

            // Check if payload is for open shift request.
            if (jsonModel.Requests.Any(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture)))
            {
                // Process payload for open shift request.
                responseModelList = await this.ProcessOpenShiftRequest(jsonModel, updateProps, aadGroupId, isRequestFromCorrectIntegration, kronosTimeZone).ConfigureAwait(false);
            }

            // Check if payload is for swap shift request.
            else if (jsonModel.Requests.Any(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)))
            {
                this.telemetryClient.TrackTrace("Teams Controller swapRequests " + JsonConvert.SerializeObject(jsonModel));

                // Process payload for swap shift request.
                responseModelList = await this.ProcessSwapShiftRequest(jsonModel, aadGroupId, isRequestFromCorrectIntegration, kronosTimeZone).ConfigureAwait(true);
            }

            // Check if payload is for time off request.
            else if (jsonModel.Requests.Any(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture)))
            {
                this.telemetryClient.TrackTrace("Teams Controller timeOffRequests " + JsonConvert.SerializeObject(jsonModel));

                // Process payload for time off request.
                responseModelList = await this.ProcessTimeOffRequest(jsonModel, aadGroupId, isRequestFromCorrectIntegration, kronosTimeZone).ConfigureAwait(true);
            }

            // Check if payload is for open shift.
            else if (jsonModel.Requests.Any(x => x.Url.Contains("/openshifts/", StringComparison.InvariantCulture)))
            {
                // Acknowledge with status OK for open shift as solution does not synchronize open shifts into Kronos, Kronos being single source of truth for front line manager actions.
                integrationResponse = ProcessOpenShiftAcknowledgement(jsonModel, updateProps);
                responseModelList.Add(integrationResponse);
            }

            // Check if payload is for shift.
            else if (jsonModel.Requests.Any(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)))
            {
                integrationResponse = await this.ProcessShiftAsync(jsonModel, updateProps, mappedTeam, isRequestFromCorrectIntegration).ConfigureAwait(false);
                responseModelList.Add(integrationResponse);
            }

            responseModel.ShiftsIntegResponses = responseModelList;
            responseModelStr = JsonConvert.SerializeObject(responseModel);

            this.telemetryClient.TrackTrace("IncomingRequest, ends for method: UpdateTeam - " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

            // Sends response back to Shifts.
            return this.Ok(responseModelStr);
        }

        /// <summary>
        /// The method that will be called from Shifts.
        /// </summary>
        /// <param name="aadGroupId">The AAD Group Id for the Team.</param>
        /// <returns>An action result.</returns>
        [HttpPost]
        [Route("/v1/teams/{aadGroupId}/read")]
        public async Task<ActionResult> UpdateShiftEligibility([FromRoute] string aadGroupId)
        {
            var configurationEntity = (await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false))?.FirstOrDefault();
            byte[] secretKeyBytes = Encoding.UTF8.GetBytes(configurationEntity?.WorkforceIntegrationSecret);
            var jsonModel = await DecryptEncryptedRequestFromShiftsAsync(secretKeyBytes, this.Request).ConfigureAwait(false);

            var mappedTeam = (await this.teamDepartmentMappingProvider.GetMappedTeamDetailsAsync(aadGroupId).ConfigureAwait(false)).FirstOrDefault();
            var kronosTimeZone = string.IsNullOrEmpty(mappedTeam?.KronosTimeZone) ? this.appSettings.KronosTimeZone : mappedTeam.KronosTimeZone;

            var integrationResponse = await this.swapShiftEligibilityController.GetEligibleShiftsForSwappingAsync(jsonModel.Requests[0].Id, kronosTimeZone)
                .ConfigureAwait(false);

            IntegrationApiResponseModel responseModel = new IntegrationApiResponseModel
            {
                ShiftsIntegResponses = new List<ShiftsIntegResponse> { integrationResponse },
            };
            string responseModelStr = JsonConvert.SerializeObject(responseModel);

            return this.Ok(responseModelStr);
        }

        /// <summary>
        /// This method will manage how Shift entities are created, updated, and deleted.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="updateProps">The type of <see cref="Dictionary{TKey, TValue}"/> that contains properties that are being logged to ApplicationInsights.</param>
        /// <returns>A type of <see cref="ShiftsIntegResponse"/>.</returns>
        private async Task<ShiftsIntegResponse> ProcessShiftAsync(RequestModel jsonModel, Dictionary<string, string> updateProps, TeamToDepartmentJobMappingEntity mappedTeam, bool isFromLogicApp)
        {
            var requestBody = jsonModel.Requests.First(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)).Body;
            ShiftsIntegResponse response = null;

            if (requestBody != null)
            {
                var shift = ControllerHelper.Get<Shift>(jsonModel, "/shifts/");
                var user = await this.userMappingProvider.GetUserMappingEntityAsyncNew(
                    shift.UserId,
                    shift.SchedulingGroupId).ConfigureAwait(false);

                if (isFromLogicApp)
                {
                    return CreateSuccessfulResponse(shift.Id);
                }

                try
                {
                    if (jsonModel.Requests.Any(c => c.Method == "POST"))
                    {
                        response = await this.shiftController.AddShiftToKronos(shift, user, mappedTeam).ConfigureAwait(false);
                    }
                    else if (jsonModel.Requests.Any(c => c.Method == "PUT"))
                    {
                        if (shift.DraftShift?.IsActive == false || shift.SharedShift?.IsActive == false)
                        {
                            // This looks like a bug with shifts sending a PUT for both edits and deletes, so it looks for the isActive flag instead.
                            response = await this.shiftController.DeleteShiftFromKronos(shift, user, mappedTeam).ConfigureAwait(false);
                        }
                        else
                        {
                            // Edit goes here
                            response = CreateSuccessfulResponse(shift.Id);
                        }
                    }
                }
                catch (Exception)
                {
                    this.telemetryClient.TrackTrace("Exception dealing with WFI call regarding shifts." + JsonConvert.SerializeObject(response));
                    throw;
                }

                return response;
            }
            else
            {
                var nullBodyShiftId = jsonModel.Requests.First(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)).Id;
                updateProps.Add("NullBodyShiftId", nullBodyShiftId);

                // The outbound acknowledgement does not honor the null Etag, 502 Bad Gateway is thrown if so.
                // Checking for the null eTag value, from the attributes in the payload and generate a non-null value in GenerateResponse method.
                return CreateSuccessfulResponse(nullBodyShiftId);
            }
        }

        /// <summary>
        /// This method will generate the necessary response for acknowledging the open shift being created or changed.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="updateProps">The type of <see cref="Dictionary{TKey, TValue}"/> which contains various properties to log to ApplicationInsights.</param>
        /// <returns>A type of <see cref="ShiftsIntegResponse"/>.</returns>
        private static ShiftsIntegResponse ProcessOpenShiftAcknowledgement(RequestModel jsonModel, Dictionary<string, string> updateProps)
        {
            ShiftsIntegResponse integrationResponse;
            if (jsonModel.Requests.First(x => x.Url.Contains("/openshifts/", StringComparison.InvariantCulture)).Body != null)
            {
                var incomingOpenShift = JsonConvert.DeserializeObject<OpenShiftIS>(jsonModel.Requests.First().Body.ToString());

                updateProps.Add("OpenShiftId", incomingOpenShift.Id);
                updateProps.Add("SchedulingGroupId", incomingOpenShift.SchedulingGroupId);

                integrationResponse = CreateSuccessfulResponse(incomingOpenShift.Id);
            }
            else
            {
                var nullBodyIncomingOpenShiftId = jsonModel.Requests.First(x => x.Url.Contains("/openshifts/", StringComparison.InvariantCulture)).Id;
                updateProps.Add("NullBodyOpenShiftId", nullBodyIncomingOpenShiftId);
                integrationResponse = CreateSuccessfulResponse(nullBodyIncomingOpenShiftId);
            }

            return integrationResponse;
        }

        /// <summary>
        /// This method will properly decrypt the encrypted payload that is being received from Shifts.
        /// </summary>
        /// <param name="secretKeyBytes">The sharedSecret from Shifts casted into a byte array.</param>
        /// <param name="request">The incoming request from Shifts UI that contains an encrypted payload.</param>
        /// <returns>A unit of execution which contains the RequestModel.</returns>
        private static async Task<RequestModel> DecryptEncryptedRequestFromShiftsAsync(byte[] secretKeyBytes, HttpRequest request)
        {
            string decryptedRequestBody = null;

            // Step 1 - using a memory stream for the processing of the request.
            using (MemoryStream ms = new MemoryStream())
            {
                await request.Body.CopyToAsync(ms).ConfigureAwait(false);
                byte[] encryptedRequestBytes = ms.ToArray();
                Aes256CbcHmacSha256Encryptor decryptor = new Aes256CbcHmacSha256Encryptor(secretKeyBytes);
                byte[] decryptedRequestBodyBytes = decryptor.Decrypt(encryptedRequestBytes);
                decryptedRequestBody = Encoding.UTF8.GetString(decryptedRequestBodyBytes);
            }

            // Step 2 - Parse the decrypted request into the correct model.
            return JsonConvert.DeserializeObject<RequestModel>(decryptedRequestBody);
        }

        /// <summary>
        /// Process open shift requests outbound calls.
        /// </summary>
        /// <param name="jsonModel">Incoming payload for the request been made in Shifts.</param>
        /// <param name="updateProps">telemetry properties.</param>
        /// <param name="teamsId">The ID of the team from which the request originated.</param>
        /// <param name="isRequestFromCorrectIntegration">Whether the request originated from the correct workforce integration or not</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>Returns list of ShiftIntegResponse for request.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessOpenShiftRequest(RequestModel jsonModel, Dictionary<string, string> updateProps, string teamsId, bool isRequestFromCorrectIntegration, string kronosTimeZone)
        {
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var requestBody = jsonModel.Requests.First(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture)).Body;

            if (requestBody != null)
            {
                switch (requestBody["state"].Value<string>())
                {
                    // The Open shift request is submitted in Shifts and is pending with manager for approval.
                    case ApiConstants.ShiftsPending:
                        {
                            var openShiftRequest = JsonConvert.DeserializeObject<OpenShiftRequestIS>(requestBody.ToString());
                            responseModelList = await this.openShiftRequestController.ProcessCreateOpenShiftRequestFromTeamsAsync(openShiftRequest, teamsId).ConfigureAwait(false);
                        }

                        break;

                    // The Open shift request is approved by manager.
                    case ApiConstants.ShiftsApproved:
                        {
                            // The request is coming from intended workforce integration.
                            if (isRequestFromCorrectIntegration)
                            {
                                this.telemetryClient.TrackTrace($"Request coming from correct workforce integration is {isRequestFromCorrectIntegration} for OpenShiftRequest approval outbound call.");
                                responseModelList = await this.openShiftRequestController.ProcessOpenShiftRequestApprovalAsync(jsonModel, updateProps, kronosTimeZone).ConfigureAwait(false);
                            }

                            // Request is coming from the Shifts UI.
                            else
                            {
                                responseModelList = await this.openShiftRequestController.ProcessOpenShiftRequestApprovalFromTeamsAsync(jsonModel, updateProps, kronosTimeZone, true).ConfigureAwait(false);
                            }
                        }

                        break;

                    // The code below would be when there is a decline.
                    case ApiConstants.ShiftsDeclined:
                        {
                            // The request is coming from intended workforce integration.
                            if (isRequestFromCorrectIntegration)
                            {
                                this.telemetryClient.TrackTrace($"Request coming from correct workforce integration is {isRequestFromCorrectIntegration} for OpenShiftRequest decline outbound call.");
                                var integrationResponse = new ShiftsIntegResponse();
                                foreach (var item in jsonModel.Requests)
                                {
                                    integrationResponse = CreateSuccessfulResponse(item.Id);
                                    responseModelList.Add(integrationResponse);
                                }
                            }

                            // Request is coming from the Shifts UI.
                            else
                            {
                                responseModelList = await this.openShiftRequestController.ProcessOpenShiftRequestApprovalFromTeamsAsync(jsonModel, updateProps, kronosTimeZone, false).ConfigureAwait(false);
                            }
                        }

                        break;
                }
            }

            return responseModelList;
        }

        /// <summary>
        /// Process swap shift requests outbound calls.
        /// </summary>
        /// <param name="jsonModel">Incoming payload for the request been made in Shifts.</param>
        /// <param name="aadGroupId">AAD Group id.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>Returns list of ShiftIntegResponse for request.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessSwapShiftRequest(RequestModel jsonModel, string aadGroupId, bool isRequestFromCorrectIntegration, string kronosTimeZone)
        {
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var requestBody = jsonModel.Requests.First(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)).Body;
            ShiftsIntegResponse integrationResponseSwap = null;
            try
            {
                // If the requestBody is not null - represents either a new Swap Shift request being created by FLW1,
                // FLW2 accepts or declines FLW1's request, or FLM approves or declines the Swap Shift request.
                // If the requestBody is null - represents FLW1's cancellation of the Swap Shift Request.
                if (requestBody != null)
                {
                    var requestState = requestBody?["state"].Value<string>();
                    var requestAssignedTo = requestBody?["assignedTo"].Value<string>();

                    var swapRequest = JsonConvert.DeserializeObject<SwapRequest>(requestBody.ToString());

                    // FLW1 has requested for swap shift, submit the request in Kronos.
                    if (requestState == ApiConstants.ShiftsPending && requestAssignedTo == ApiConstants.ShiftsRecipient)
                    {
                        integrationResponseSwap = await this.swapShiftController.SubmitSwapShiftRequestToKronosAsync(swapRequest, aadGroupId).ConfigureAwait(false);
                        responseModelList.Add(integrationResponseSwap);
                    }

                    // FLW2 has approved the swap shift, updates the status in Kronos to submitted and request goes to manager for approval.
                    else if (requestState == ApiConstants.ShiftsPending && requestAssignedTo == ApiConstants.ShiftsManager)
                    {
                        integrationResponseSwap = await this.swapShiftController.ApproveOrDeclineSwapShiftRequestToKronosAsync(swapRequest, aadGroupId).ConfigureAwait(false);
                        responseModelList.Add(integrationResponseSwap);
                    }

                    // FLW2 has declined the swap shift, updates the status in Kronos to refused.
                    else if (requestState == ApiConstants.Declined && requestAssignedTo == ApiConstants.ShiftsRecipient)
                    {
                        integrationResponseSwap = await this.swapShiftController.ApproveOrDeclineSwapShiftRequestToKronosAsync(swapRequest, aadGroupId).ConfigureAwait(false);
                        responseModelList.Add(integrationResponseSwap);
                    }

                    // Manager has declined the request in Kronos, which declines the request in Shifts also.
                    else if (requestState == ApiConstants.ShiftsDeclined && requestAssignedTo == ApiConstants.ShiftsManager)
                    {
                        // The request is coming from intended workforce integration.
                        if (isRequestFromCorrectIntegration)
                        {
                            this.telemetryClient.TrackTrace($"Request coming from correct workforce integration is {isRequestFromCorrectIntegration} for SwapShiftRequest decline outbound call.");
                            integrationResponseSwap = CreateResponse(swapRequest.Id, (int)HttpStatusCode.OK, eTag: swapRequest.ETag);
                            responseModelList.Add(integrationResponseSwap);
                        }

                        // Request is coming from either Shifts UI or from incorrect workforce integration.
                        else
                        {
                            responseModelList = await this.ProcessShiftSwapRequestApprovalViaTeams(jsonModel, kronosTimeZone, false).ConfigureAwait(false);
                        }
                    }

                    // Manager has approved the request in Kronos.
                    else if (requestState == ApiConstants.ShiftsApproved && requestAssignedTo == ApiConstants.ShiftsManager)
                    {
                        // The request is coming from intended workforce integration.
                        if (isRequestFromCorrectIntegration)
                        {
                            this.telemetryClient.TrackTrace($"Request coming from correct workforce integration is {isRequestFromCorrectIntegration} for SwapShiftRequest approval outbound call.");
                            responseModelList = await this.ProcessSwapShiftRequestApprovalAsync(jsonModel, aadGroupId, kronosTimeZone).ConfigureAwait(false);
                        }

                        // Request is coming from either Shifts UI or from incorrect workforce integration.
                        else
                        {
                            responseModelList = await this.ProcessShiftSwapRequestApprovalViaTeams(jsonModel, kronosTimeZone, true).ConfigureAwait(false);
                        }
                    }

                    // There is a System decline with the Swap Shift Request
                    else if (requestState == ApiConstants.Declined && requestAssignedTo == ApiConstants.System)
                    {
                        var systemDeclineSwapReqId = jsonModel.Requests.First(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)).Id;
                        integrationResponseSwap = CreateResponse(systemDeclineSwapReqId, (int)HttpStatusCode.OK, Resource.SystemDeclined);
                        responseModelList.Add(integrationResponseSwap);
                    }
                }
                else if (jsonModel.Requests.Any(c => c.Method == "DELETE"))
                {
                    // Code below handles the delete swap shift request.
                    var deleteSwapRequestId = jsonModel.Requests.First(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)).Id;

                    // Logging to telemetry the incoming cancelled request by FLW1.
                    this.telemetryClient.TrackTrace($"The Swap Shift Request: {deleteSwapRequestId} has been declined by FLW1.");

                    var entityToCancel = await this.swapShiftMappingEntityProvider.GetKronosReqAsync(deleteSwapRequestId).ConfigureAwait(false);

                    if (entityToCancel != null)
                    {
                        if (entityToCancel.KronosStatus != ApiConstants.Retract)
                        {
                            responseModelList.Add(await this.swapShiftController.RetractOfferedShiftAsync(entityToCancel).ConfigureAwait(false));
                            entityToCancel.ShiftsStatus = ApiConstants.SwapShiftCancelled;
                            await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(entityToCancel).ConfigureAwait(false);
                        }
                    }

                    integrationResponseSwap = CreateSuccessfulResponse(deleteSwapRequestId);
                    responseModelList.Add(integrationResponseSwap);
                }
            }
            catch (Exception)
            {
                this.telemetryClient.TrackTrace("Teams Controller swapRequests responseModelList Exception" + JsonConvert.SerializeObject(responseModelList));
                throw;
            }

            this.telemetryClient.TrackTrace("Teams Controller swapRequests responseModelList" + JsonConvert.SerializeObject(responseModelList));

            return responseModelList;
        }

        /// <summary>
        /// Process time off request outbound calls.
        /// </summary>
        /// <param name="jsonModel">Incoming payload for the request been made in Shifts.</param>
        /// <param name="aadGroupId">AAD Group id.</param>
        /// <param name="isRequestFromCorrectIntegration">Whether the request originated from the correct workforce integration or not.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>Returns list of ShiftIntegResponse for request.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessTimeOffRequest(RequestModel jsonModel, string aadGroupId, bool isRequestFromCorrectIntegration, string kronosTimeZone)
        {
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var requestBody = jsonModel.Requests.First(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture)).Body;

            try
            {
                if (requestBody != null)
                {
                    switch (requestBody["state"].Value<string>())
                    {
                        // The time off request is submitted in Shifts and is pending manager approval.
                        case ApiConstants.ShiftsPending:
                            {
                                // Request came from the correct workforce integration
                                if (isRequestFromCorrectIntegration)
                                {
                                    this.telemetryClient.TrackTrace("Create time off request came from correct workforce integration however we dont support this sync.");

                                    // All required work handled by logic app so just return ok response
                                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, "Syncing time off requests created in Kronos is not supported."));
                                }

                                // Request came from Shifts UI
                                else
                                {
                                    this.telemetryClient.TrackTrace($"Create time off request coming from Shifts UI.");
                                    responseModelList = await this.ProcessCreateTimeOffRequestViaTeamsAsync(jsonModel, aadGroupId, kronosTimeZone).ConfigureAwait(false);
                                }
                            }

                            break;

                        // The time off request is approved by a manager
                        case ApiConstants.ShiftsApproved:
                            {
                                // Request came from the correct workforce integration
                                if (isRequestFromCorrectIntegration)
                                {
                                    this.telemetryClient.TrackTrace("Approve time off request came from correct workforce integration.");
                                    var requestId = jsonModel.Requests.First(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture)).Id;

                                    // All required work handled by logic app so just return ok response
                                    responseModelList.Add(CreateSuccessfulResponse(requestId));
                                }

                                // Request came from Shifts UI
                                else
                                {
                                    this.telemetryClient.TrackTrace($"Approve time off request coming from Shifts UI.");
                                    responseModelList = await this.ProcessTimeOffRequestApprovalViaTeamsAsync(jsonModel, kronosTimeZone, true).ConfigureAwait(false);
                                }
                            }

                            break;

                        // The time off request is declined by a manager
                        case ApiConstants.ShiftsDeclined:
                            {
                                // Request came from the correct workforce integration
                                if (isRequestFromCorrectIntegration)
                                {
                                    this.telemetryClient.TrackTrace("Decline time off request came from correct workforce integration.");
                                    var requestId = jsonModel.Requests.First(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture)).Id;

                                    // All required work handled by logic app so just return ok response
                                    responseModelList.Add(CreateSuccessfulResponse(requestId));
                                }

                                // Request came from Shifts UI
                                else
                                {
                                    this.telemetryClient.TrackTrace($"Decline time off request coming from Shifts UI.");
                                    responseModelList = await this.ProcessTimeOffRequestApprovalViaTeamsAsync(jsonModel, kronosTimeZone, false).ConfigureAwait(false);
                                }
                            }

                            break;
                    }
                }

                // Request has been cancelled in Shifts app
                else if (jsonModel.Requests.Any(c => c.Method == "DELETE"))
                {
                    this.telemetryClient.TrackTrace($"Cancel time off request coming from Shifts UI.");
                    responseModelList = await this.ProcessCancelTimeOffRequestViaTeamsAsync(jsonModel).ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                this.telemetryClient.TrackTrace("Teams Controller timeOffRequests responseModelList Exception" + JsonConvert.SerializeObject(responseModelList));
                throw;
            }

            return responseModelList;
        }

        /// <summary>
        /// Approves the swap shift request.
        /// </summary>
        /// <param name="swapShiftRequest">A swap shift request.</param>
        /// <param name="swapShiftMapping">A swap shift request mapping.</param>
        /// <param name="responseModelList">The list of responses.</param>
        private async Task ApproveSwapShiftRequestInTables(
            SwapRequest swapShiftRequest,
            SwapShiftMappingEntity swapShiftMapping,
            List<ShiftsIntegResponse> responseModelList)
        {
            this.telemetryClient.TrackTrace($"Started approving SwapShiftRequest {swapShiftRequest.Id}");

            swapShiftMapping.ShiftsStatus = swapShiftRequest.State;
            await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMapping).ConfigureAwait(false);
            responseModelList.Add(CreateSuccessfulResponse(swapShiftRequest.Id));

            this.telemetryClient.TrackTrace($"Finished approving SwapShiftRequest {swapShiftRequest.Id}");
        }

        /// <summary>
        /// Gets the temporary shift.
        /// Creates a new shift using all of the temporary shift's data besides the RowKey.
        /// Deletes the temporary shift.
        /// </summary>
        /// <param name="openShiftRequest">An open shift request.</param>
        /// <param name="shift">A shift.</param>
        /// <param name="kronosTimeZone">The Kronos time zone.</param>
        /// <param name="responseModelList">The list of responses.</param>
        private async Task CreateShiftFromTempShift(
            OpenShiftRequestIS openShiftRequest,
            Shift shift,
            string kronosTimeZone,
            List<ShiftsIntegResponse> responseModelList)
        {
            this.telemetryClient.TrackTrace($"Started dealing with OpenShiftRequest {openShiftRequest.Id}");

            // Step 1 - Get the temp shift record first by table scan against RowKey.
            var tempShiftRowKey = $"SHFT_PENDING_{openShiftRequest.Id}";
            var tempShiftEntity = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(tempShiftRowKey).ConfigureAwait(false);

            // We need to check if the tempShift is not null because in the Open Shift Request controller, the tempShift was created
            // as part of the Graph API call to approve the Open Shift Request.
            if (tempShiftEntity != null)
            {
                var startDateTime = DateTime.SpecifyKind(shift.SharedShift.StartDateTime, DateTimeKind.Utc);

                // Step 2 - Form the new shift record.
                var shiftToInsert = new TeamsShiftMappingEntity()
                {
                    RowKey = shift.Id,
                    KronosPersonNumber = tempShiftEntity.KronosPersonNumber,
                    KronosUniqueId = tempShiftEntity.KronosUniqueId,
                    PartitionKey = tempShiftEntity.PartitionKey,
                    AadUserId = tempShiftEntity.AadUserId,
                    ShiftStartDate = startDateTime,
                };

                // Step 3 - Save the new shift record.
                await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(shiftToInsert, shiftToInsert.RowKey, shiftToInsert.PartitionKey).ConfigureAwait(false);

                // Step 4 - Delete the temp shift record.
                await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(tempShiftEntity).ConfigureAwait(false);

                // Adding response for create new shift.
                responseModelList.Add(CreateSuccessfulResponse(shift.Id));
            }
            else
            {
                // We are logging to ApplicationInsights that the tempShift entity could not be found.
                this.telemetryClient.TrackTrace(string.Format(CultureInfo.InvariantCulture, Resource.EntityNotFoundWithRowKey, tempShiftRowKey));
            }

            this.telemetryClient.TrackTrace($"Finished dealing with OpenShiftRequest {openShiftRequest.Id}");
        }

        /// <summary>
        /// This method takes a time off request created in the Shifts app
        /// Creates the request to be sent to Kronos, sends it and depending on the response updates the relevant tables.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessCreateTimeOffRequestViaTeamsAsync(RequestModel jsonModel, string teamsId, string kronosTimeZone)
        {
            this.telemetryClient.TrackTrace("Processing creation of a TimeOffRequest received from Shifts app");
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var updateProps = new Dictionary<string, string>();
            var timeOffObject = jsonModel?.Requests?.FirstOrDefault(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture));
            TimeOffRequestItem timeOffEntity = null;

            timeOffEntity = JsonConvert.DeserializeObject<TimeOffRequestItem>(timeOffObject.Body.ToString());

            try
            {
                var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

                // Get the requestors user details
                var user = await UsersHelper.GetMappedUserDetailsAsync(allRequiredConfigurations.WFIId, timeOffEntity.SenderUserId, teamsId, this.userMappingProvider, this.teamDepartmentMappingProvider, this.telemetryClient).ConfigureAwait(false);
                if (user is null)
                {
                    this.telemetryClient.TrackTrace($"Create time off request from teams failed - Could not find user {timeOffEntity.SenderUserId}");
                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.UserMappingNotFound));
                }

                // Get list of time off reasons from pay code to time off reason mapping table
                var timeOffReasons = await this.timeOffReasonProvider.GetTimeOffReasonsAsync().ConfigureAwait(false);

                var timeOffReason = timeOffReasons.SingleOrDefault(t => t.TimeOffReasonId == timeOffEntity.TimeOffReasonId);
                if (timeOffReason is null)
                {
                    this.telemetryClient.TrackTrace($"Create time off request from teams failed - Could not find the time off reason with id: {timeOffEntity.TimeOffReasonId}");
                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.TimeOffReasonNotFound));
                }

                var wasSuccess = await this.timeOffController.CreateTimeOffRequestInKronosAsync(user, timeOffEntity, timeOffReason, allRequiredConfigurations, kronosTimeZone).ConfigureAwait(true);
                if (wasSuccess is false)
                {
                    this.telemetryClient.TrackTrace($"Time off request creation was unsuccessful.");
                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.TimeOffRequestCreationFailed));
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Time off request creation was successful.");
                    responseModelList.Add(CreateSuccessfulResponse(timeOffEntity.Id));
                }
            }
            catch (Exception ex)
            {
                var exceptionProps = new Dictionary<string, string>()
                    {
                        { "TimeOffRequestId", timeOffEntity.Id },
                        { "UserId", timeOffEntity.SenderUserId },
                    };

                this.telemetryClient.TrackException(ex, exceptionProps);
                throw;
            }

            this.telemetryClient.TrackTrace($"Time off request creation sync process from Teams complete.");
            return responseModelList;
        }

        /// <summary>
        /// This method cancels a time off request via teams
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessCancelTimeOffRequestViaTeamsAsync(RequestModel jsonModel)
        {
            this.telemetryClient.TrackTrace("Processing cancellation of a TimeOffRequest received from Shifts app");
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();

            var cancelTimeOffRequestId = jsonModel.Requests.First(x => x.Url.Contains("/timeOffRequests/", StringComparison.InvariantCulture)).Id;

            try
            {
                var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

                var entityToCancel = await this.timeOffReqMappingEntityProvider.GetTimeOffRequestMappingEntityByRequestIdAsync(cancelTimeOffRequestId).ConfigureAwait(false);

                var wasSuccess = await this.timeOffController.CancelTimeOffRequestInKronosAsync(entityToCancel).ConfigureAwait(true);
                if (wasSuccess is false)
                {
                    this.telemetryClient.TrackTrace($"Time off request cancellation failed.");
                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.TimeOffRequestCancellationFailed));
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Time off request cancellation was successful.");
                    responseModelList.Add(CreateSuccessfulResponse(cancelTimeOffRequestId));
                }
            }
            catch (Exception ex)
            {
                var exceptionProps = new Dictionary<string, string>()
                    {
                        { "TimeOffRequestId", cancelTimeOffRequestId },
                    };

                this.telemetryClient.TrackException(ex, exceptionProps);
                throw;
            }

            return responseModelList;
        }

        /// <summary>
        /// This method takes an approval or Decline that happens in the Shifts app
        /// Creates the request to be sent to Kronos, sends it and depending on the response updates the relevant tables.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <param name="approved">Whether the request is approved or declined.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessTimeOffRequestApprovalViaTeamsAsync(
            RequestModel jsonModel,
            string kronosTimeZone,
            bool approved)
        {
            this.telemetryClient.TrackTrace("Processing approval of TimeOffRequest received from Shifts app");
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();

            var timeOffRequest = ControllerHelper.GetRequest<TimeOffRequestItem>(jsonModel, "/timeOffRequests/", approved);
            var success = false;

            try
            {
                var timeOffRequestMapping = await this.timeOffReqMappingEntityProvider.GetTimeOffRequestMappingEntityByRequestIdAsync(timeOffRequest.Id).ConfigureAwait(false);
                var kronosReqId = timeOffRequestMapping.KronosRequestId;
                var kronosUserId = timeOffRequestMapping.KronosPersonNumber;

                var updateProps = new Dictionary<string, string>()
                {
                    { "KronosPersonNumber", kronosUserId },
                    { "TimeOffRequestID", timeOffRequestMapping.ShiftsRequestId },
                    { "KronosTimeOffRequestId", kronosReqId },
                };

                if (!approved)
                {
                    this.telemetryClient.TrackTrace($"Process denial of {timeOffRequest.Id}", updateProps);

                    // Deny in Kronos, Update mapping for Teams.
                    success = await this.timeOffController.ApproveOrDenyTimeOffRequestInKronos(kronosReqId, kronosUserId, timeOffRequestMapping, approved).ConfigureAwait(false);
                    if (!success)
                    {
                        this.telemetryClient.TrackTrace($"Process failure to deny time off request: {timeOffRequest.Id}", updateProps);
                        responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.TimeOffRequestDeclineFailed));
                        return responseModelList;
                    }

                    responseModelList.Add(CreateSuccessfulResponse(timeOffRequest.Id));
                    timeOffRequestMapping.ShiftsStatus = ApiConstants.Refused;
                    await this.timeOffReqMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(timeOffRequestMapping).ConfigureAwait(false);
                    this.telemetryClient.TrackTrace($"Finished denial of {timeOffRequest.Id}", updateProps);
                    return responseModelList;
                }

                this.telemetryClient.TrackTrace($"Process approval of {timeOffRequest.Id}", updateProps);

                // approve in kronos
                success = await this.timeOffController.ApproveOrDenyTimeOffRequestInKronos(kronosReqId, kronosUserId, timeOffRequestMapping, approved).ConfigureAwait(false);
                updateProps.Add("SuccessfullyApprovedInKronos", $"{success}");

                if (!success)
                {
                    this.telemetryClient.TrackTrace($"Process failure to approve time off request: {timeOffRequest.Id}", updateProps);
                    responseModelList.AddRange(CreateMultipleBadResponses(jsonModel, Resource.TimeOffRequestApproveFailed));
                    return responseModelList;
                }

                responseModelList.Add(CreateSuccessfulResponse(timeOffRequest.Id));
                timeOffRequestMapping.ShiftsStatus = ApiConstants.ShiftsApproved;
                await this.timeOffReqMappingEntityProvider.SaveOrUpdateTimeOffMappingEntityAsync(timeOffRequestMapping).ConfigureAwait(false);
                this.telemetryClient.TrackTrace($"Finished approval of {timeOffRequest.Id}", updateProps);
                return responseModelList;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace($"An unexpected error occured with the following: {ex.StackTrace.ToString(CultureInfo.InvariantCulture)}");
                throw;
            }
        }

        /// <summary>
        /// This method further processes the Swap Shift request approval.
        /// </summary>
        /// <param name="jsonModel">The decryped JSON payload from Shifts/MS Graph.</param>
        /// <param name="aadGroupId">The team ID for which the Swap Shift request has been approved.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>A unit of execution that contains the type of <see cref="ShiftsIntegResponse"/>.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessSwapShiftRequestApprovalAsync(RequestModel jsonModel, string aadGroupId, string kronosTimeZone)
        {
            List<ShiftsIntegResponse> swapShiftsIntegResponses = new List<ShiftsIntegResponse>();
            ShiftsIntegResponse integrationResponse = null;
            var swapShiftApprovalRes = from requests in jsonModel.Requests
                                       group requests by requests.Url;

            var swapRequests = jsonModel.Requests.Where(c => c.Url.Contains("/swapRequests/", StringComparison.InvariantCulture));

            // Filter all the system declined requests.
            var autoDeclinedRequests = swapRequests.Where(c => c.Body != null && c.Body["state"].Value<string>() == ApiConstants.Declined && c.Body["assignedTo"].Value<string>() == ApiConstants.System).ToList();

            // Filter approved swap shift request.
            var approvedSwapShiftRequest = swapRequests.Where(c => c.Body != null && c.Body["state"].Value<string>() == ApiConstants.ShiftsApproved && c.Body["assignedTo"].Value<string>() == ApiConstants.ShiftsManager).FirstOrDefault();

            var swapShiftRequest = JsonConvert.DeserializeObject<SwapRequest>(approvedSwapShiftRequest.Body.ToString());

            var postedShifts = jsonModel.Requests.Where(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture) && x.Method == "POST").ToList();

            var deletedShifts = jsonModel.Requests.Where(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture) && x.Method == "DELETE").ToList();

            if (swapShiftRequest != null)
            {
                var newShiftFirst = JsonConvert.DeserializeObject<Shift>(postedShifts.First().Body.ToString());
                var newShiftSecond = JsonConvert.DeserializeObject<Shift>(postedShifts.Last().Body.ToString());

                // Step 1 - Create the Kronos Unique ID.
                var kronosUniqueIdFirst = this.utility.CreateUniqueId(newShiftFirst, kronosTimeZone);
                var kronosUniqueIdSecond = this.utility.CreateUniqueId(newShiftSecond, kronosTimeZone);

                try
                {
                    var userMappingRecord = await this.userMappingProvider.GetUserMappingEntityAsyncNew(
                                       newShiftFirst?.UserId,
                                       aadGroupId).ConfigureAwait(false);

                    // When getting the month partition key, make sure to take into account the Kronos Time Zone as well
                    var provider = CultureInfo.InvariantCulture;
                    var actualStartDateTimeStr = this.utility.CalculateStartDateTime(
                        newShiftFirst.SharedShift.StartDateTime.Date, kronosTimeZone).ToString("M/dd/yyyy", provider);
                    var actualEndDateTimeStr = this.utility.CalculateEndDateTime(
                        newShiftFirst.SharedShift.EndDateTime.Date, kronosTimeZone).ToString("M/dd/yyyy", provider);

                    // Create the month partition key based on the finalShift object.
                    var monthPartitions = Common.Utility.GetMonthPartition(actualStartDateTimeStr, actualEndDateTimeStr);
                    var monthPartition = monthPartitions?.FirstOrDefault();

                    // Create the shift mapping entity based on the finalShift object also.
                    var shiftEntity = this.utility.CreateShiftMappingEntity(newShiftFirst, userMappingRecord, kronosUniqueIdFirst);
                    await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(
                        shiftEntity,
                        newShiftFirst.Id,
                        monthPartition).ConfigureAwait(false);

                    var userMappingRecordSec = await this.userMappingProvider.GetUserMappingEntityAsyncNew(
                                      newShiftSecond?.UserId,
                                      aadGroupId).ConfigureAwait(false);
                    integrationResponse = CreateSuccessfulResponse(newShiftFirst.Id);
                    swapShiftsIntegResponses.Add(integrationResponse);

                    // When getting the month partition key, make sure to take into account the Kronos Time Zone as well
                    var actualStartDateTimeStrSec = this.utility.CalculateStartDateTime(
                        newShiftSecond.SharedShift.StartDateTime, kronosTimeZone).ToString("M/dd/yyyy", provider);
                    var actualEndDateTimeStrSec = this.utility.CalculateEndDateTime(
                        newShiftSecond.SharedShift.EndDateTime, kronosTimeZone).ToString("M/dd/yyyy", provider);

                    // Create the month partition key based on the finalShift object.
                    var monthPartitionsSec = Common.Utility.GetMonthPartition(actualStartDateTimeStrSec, actualEndDateTimeStrSec);
                    var monthPartitionSec = monthPartitionsSec?.FirstOrDefault();

                    // Create the shift mapping entity based on the finalShift object also.
                    var shiftEntitySec = this.utility.CreateShiftMappingEntity(newShiftSecond, userMappingRecordSec, kronosUniqueIdSecond);
                    await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(
                        shiftEntitySec,
                        newShiftSecond.Id,
                        monthPartitionSec).ConfigureAwait(false);
                    integrationResponse = CreateSuccessfulResponse(newShiftSecond.Id);
                    swapShiftsIntegResponses.Add(integrationResponse);

                    foreach (var delShifts in deletedShifts)
                    {
                        integrationResponse = CreateSuccessfulResponse(delShifts.Id);
                        swapShiftsIntegResponses.Add(integrationResponse);
                    }

                    integrationResponse = CreateResponse(approvedSwapShiftRequest.Id, (int)HttpStatusCode.OK, eTag: swapShiftRequest.ETag);
                    swapShiftsIntegResponses.Add(integrationResponse);

                    foreach (var declinedRequest in autoDeclinedRequests)
                    {
                        this.telemetryClient.TrackTrace($"SystemDeclinedOpenShiftRequestId: {declinedRequest.Id}");
                        var declinedSwapShiftRequest = JsonConvert.DeserializeObject<SwapRequest>(declinedRequest.Body.ToString());

                        // Get the requests from storage.
                        var entityToUpdate = await this.swapShiftMappingEntityProvider.GetKronosReqAsync(
                            declinedRequest.Id).ConfigureAwait(false);

                        entityToUpdate.KronosStatus = declinedSwapShiftRequest.State;
                        entityToUpdate.ShiftsStatus = declinedSwapShiftRequest.State;

                        // Commit the change to the database.
                        await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(entityToUpdate).ConfigureAwait(false);

                        this.telemetryClient.TrackTrace($"OpenShiftRequestId: {declinedSwapShiftRequest.Id}, assigned to: {declinedSwapShiftRequest.AssignedTo}, state: {declinedSwapShiftRequest.State}");

                        // Adding response for system declined open shift request.
                        integrationResponse = CreateResponse(declinedSwapShiftRequest.Id, (int)HttpStatusCode.OK, eTag: declinedSwapShiftRequest.ETag);
                        swapShiftsIntegResponses.Add(integrationResponse);
                    }
                }
                catch (Exception ex)
                {
                    var exceptionProps = new Dictionary<string, string>()
                    {
                        { "NewFirstShiftId", newShiftFirst.Id },
                        { "NewSecondShiftId", newShiftSecond.Id },
                    };

                    this.telemetryClient.TrackException(ex, exceptionProps);
                    throw;
                }
            }

            return swapShiftsIntegResponses;
        }

        /// <summary>
        /// This method takes an approval that happens in the Shifts app
        /// Creates the request to be sent to Kronos, sends it and depending on the response updates the relevant tables.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="kronosTimeZone">The time zone to use when converting the times.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessShiftSwapRequestApprovalViaTeams(
           RequestModel jsonModel,
           string kronosTimeZone,
           bool approved)
        {
            this.telemetryClient.TrackTrace("Processing approval of ShiftSwapRequest received from Shifts app");
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var updateProps = new Dictionary<string, string>();
            var swapRequest = ControllerHelper.GetRequest<SwapRequest>(jsonModel, "/swapRequests/", approved);
            var postedShifts = jsonModel.Requests.Where(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture) && x.Method == "POST").ToList();

            var offeredShiftMap = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(swapRequest.SenderShiftId).ConfigureAwait(false);
            var requestedShiftMap = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(swapRequest.RecipientShiftId).ConfigureAwait(false);
            var success = false;

            try
            {
                var swapShiftRequestMapping = await this.swapShiftMappingEntityProvider.
                    GetMapping(offeredShiftMap.RowKey, requestedShiftMap.RowKey).ConfigureAwait(false);
                var kronosReqId = swapShiftRequestMapping.KronosReqId;
                var kronosRequestingUserId = swapShiftRequestMapping.RequestorKronosPersonNumber;
                var kronosRequestedUserId = swapShiftRequestMapping.RequestedKronosPersonNumber;

                updateProps.Add("KronosRequestingPersonNumber", kronosRequestingUserId);
                updateProps.Add("SwapShiftRequestID", swapRequest.Id);
                updateProps.Add("KronosOpenShiftRequestId", kronosReqId);

                if (!approved)
                {
                    // Deny in Kronos, Update mapping for Teams.
                    success = await this.swapShiftController.ApproveSwapShiftInKronos(kronosReqId, kronosRequestingUserId, swapShiftRequestMapping, approved).ConfigureAwait(false);
                    if (!success)
                    {
                        responseModelList.Add(CreateBadResponse(swapRequest.Id, (int)HttpStatusCode.BadRequest, "Failed in Kronos."));
                        return responseModelList;
                    }

                    swapShiftRequestMapping.ShiftsStatus = ApiConstants.Refused;
                    await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftRequestMapping).ConfigureAwait(false);
                    responseModelList.Add(CreateSuccessfulResponse(swapRequest.Id));
                    return responseModelList;
                }

                this.telemetryClient.TrackTrace($"Process approval of {swapRequest.Id}", updateProps);

                // approve in kronos
                success = await this.swapShiftController.ApproveSwapShiftInKronos(kronosReqId, kronosRequestingUserId, swapShiftRequestMapping, approved).ConfigureAwait(false);
                updateProps.Add("SuccessfullyApprovedInKronos", $"{success}");

                if (success)
                {
                    var requestorShift = JsonConvert.DeserializeObject<Shift>(postedShifts?.First().Body.ToString());
                    var requestedShift = JsonConvert.DeserializeObject<Shift>(postedShifts?.Last().Body.ToString());

                    await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(offeredShiftMap).ConfigureAwait(false);
                    await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(requestedShiftMap).ConfigureAwait(false);

                    var requestingUserShifts = await this.shiftController.GetShiftsForUser(kronosRequestingUserId, swapShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                    var requestedUserShifts = await this.shiftController.GetShiftsForUser(kronosRequestedUserId, swapShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                    var requestorShiftDate = this.utility.UTCToKronosTimeZone(requestorShift.SharedShift.StartDateTime, kronosTimeZone).ToString("d", CultureInfo.InvariantCulture);
                    var requestedShiftDate = this.utility.UTCToKronosTimeZone(requestedShift.SharedShift.StartDateTime, kronosTimeZone).ToString("d", CultureInfo.InvariantCulture);

                    // confirm new shifts exists on kronos
                    var requestedShiftKronos = requestingUserShifts
                        .Schedule.ScheduleItems.ScheduleShift
                        .FirstOrDefault(x => x.Employee.FirstOrDefault().PersonNumber == kronosRequestingUserId && x.StartDate == requestorShiftDate);
                    var requestorsShiftKronos = requestedUserShifts
                        .Schedule.ScheduleItems.ScheduleShift
                        .FirstOrDefault(x => x.Employee.FirstOrDefault().PersonNumber == kronosRequestedUserId && x.StartDate == requestedShiftDate);

                    if (requestedShiftKronos != null && requestorsShiftKronos != null)
                    {
                        var kronosRequestorsShiftUniqueId = this.utility.CreateUniqueId(requestorShift, kronosTimeZone);
                        var kronosRequestedShiftUniqueId = this.utility.CreateUniqueId(requestedShift, kronosTimeZone);
                        var requestorsShiftLink = this.shiftController.CreateNewShiftMappingEntity(requestorShift, kronosRequestorsShiftUniqueId, kronosRequestedUserId);
                        var requestedShiftLink = this.shiftController.CreateNewShiftMappingEntity(requestedShift, kronosRequestedShiftUniqueId, kronosRequestingUserId);

                        await this.ApproveSwapShiftRequestInTables(swapRequest, swapShiftRequestMapping, responseModelList).ConfigureAwait(false);
                        await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(requestorsShiftLink, requestorShift.Id, swapShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                        await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(requestedShiftLink, requestedShift.Id, swapShiftRequestMapping.PartitionKey).ConfigureAwait(false);
                        responseModelList.Add(CreateSuccessfulResponse(swapRequest.Id));
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"Error during approval of {swapRequest.Id}", updateProps);
                        responseModelList.Add(CreateBadResponse(swapRequest.Id, (int)HttpStatusCode.NotFound, "Error during approval."));
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace($"Error during approval of {swapRequest.Id}", updateProps);
                    responseModelList.Add(CreateBadResponse(swapRequest.Id, error: "Error when approving in Kronos."));
                }
            }
            catch (Exception ex)
            {
                if (ex.InnerException != null)
                {
                    this.telemetryClient.TrackTrace($"Shift mapping has failed for {swapRequest.Id}: " + ex.InnerException.ToString());
                }

                this.telemetryClient.TrackTrace($"Shift mapping has resulted in some type of error with the following: {ex.StackTrace.ToString(CultureInfo.InvariantCulture)}");
                throw;
            }

            this.telemetryClient.TrackTrace("Finished approval of processing ShiftSwapRequest received from Shifts app");
            return responseModelList;
        }
    }
}