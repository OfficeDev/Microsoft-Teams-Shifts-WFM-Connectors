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
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI.Incoming;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This is the teams controller that is being used here.
    /// </summary>
    [Route("/v1/teams")]
    [ApiController]
    public class TeamsController : Controller
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IConfigurationProvider configurationProvider;
        private readonly OpenShiftRequestController openShiftRequestController;
        private readonly SwapShiftController swapShiftController;
        private readonly Common.Utility utility;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider;
        private readonly IOpenShiftMappingEntityProvider openShiftMappingEntityProvider;
        private readonly ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="TeamsController"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="configurationProvider">ConfigurationProvider DI.</param>
        /// <param name="openShiftRequestController">OpenShiftRequestController DI.</param>
        /// <param name="swapShiftController">SwapShiftController DI.</param>
        /// <param name="utility">The common utility methods DI.</param>
        /// <param name="userMappingProvider">The user mapping provider DI.</param>
        /// <param name="shiftMappingEntityProvider">The shift entity mapping provider DI.</param>
        /// <param name="openShiftRequestMappingEntityProvider">The open shift request mapping entity provider DI.</param>
        /// <param name="openShiftMappingEntityProvider">The open shift mapping entity provider DI.</param>
        /// <param name="swapShiftMappingEntityProvider">The swap shift mapping entity provider DI.</param>
        public TeamsController(
            TelemetryClient telemetryClient,
            IConfigurationProvider configurationProvider,
            OpenShiftRequestController openShiftRequestController,
            SwapShiftController swapShiftController,
            Common.Utility utility,
            IUserMappingProvider userMappingProvider,
            IShiftMappingEntityProvider shiftMappingEntityProvider,
            IOpenShiftRequestMappingEntityProvider openShiftRequestMappingEntityProvider,
            IOpenShiftMappingEntityProvider openShiftMappingEntityProvider,
            ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider)
        {
            this.telemetryClient = telemetryClient;
            this.configurationProvider = configurationProvider;
            this.openShiftRequestController = openShiftRequestController;
            this.swapShiftController = swapShiftController;
            this.utility = utility;
            this.userMappingProvider = userMappingProvider;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
            this.openShiftRequestMappingEntityProvider = openShiftRequestMappingEntityProvider;
            this.openShiftMappingEntityProvider = openShiftMappingEntityProvider;
            this.swapShiftMappingEntityProvider = swapShiftMappingEntityProvider;
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
            this.telemetryClient.TrackTrace("IncomingRequest, starts for method: UpdateTeam - " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

            // Step 1 - Obtain the secret from the database.
            var configurationEntities = await this.configurationProvider.GetConfigurationsAsync().ConfigureAwait(false);
            var configurationEntity = configurationEntities?.FirstOrDefault();

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

            // Check if payload is for open shift request.
            if (jsonModel.Requests.Any(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture)))
            {
                // Process payload for open shift request.
                responseModelList = await this.ProcessOpenShiftRequest(jsonModel, updateProps).ConfigureAwait(false);
            }

            // Check if payload is for swap shift request.
            else if (jsonModel.Requests.Any(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)))
            {
                this.telemetryClient.TrackTrace("Teams Controller swapRequests " + JsonConvert.SerializeObject(jsonModel));

                // Process payload for swap shift request.
                responseModelList = await this.ProcessSwapShiftRequest(jsonModel, aadGroupId).ConfigureAwait(true);
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
                // Acknowledge with status OK for shift as solution does not synchronize shifts into Kronos, Kronos being single source of truth for front line manager actions.
                integrationResponse = ProcessShiftAcknowledgement(jsonModel, updateProps);
                responseModelList.Add(integrationResponse);
            }

            responseModel.ShiftsIntegResponses = responseModelList;
            responseModelStr = JsonConvert.SerializeObject(responseModel);

            this.telemetryClient.TrackTrace("IncomingRequest, ends for method: UpdateTeam - " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

            // Sends response back to Shifts.
            return this.Ok(responseModelStr);
        }

        /// <summary>
        /// This method will create the necessary acknowledgement response whenever Shift entities are created, or updated.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="updateProps">The type of <see cref="Dictionary{TKey, TValue}"/> that contains properties that are being logged to ApplicationInsights.</param>
        /// <returns>A type of <see cref="ShiftsIntegResponse"/>.</returns>
        private static ShiftsIntegResponse ProcessShiftAcknowledgement(RequestModel jsonModel, Dictionary<string, string> updateProps)
        {
            if (jsonModel.Requests.First(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)).Body != null)
            {
                var incomingShift = JsonConvert.DeserializeObject<Shift>(jsonModel.Requests.First(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)).Body.ToString());

                updateProps.Add("ShiftId", incomingShift.Id);
                updateProps.Add("UserIdForShift", incomingShift.UserId);
                updateProps.Add("SchedulingGroupId", incomingShift.SchedulingGroupId);

                var integrationResponse = GenerateResponse(incomingShift.Id, HttpStatusCode.OK, null, null);
                return integrationResponse;
            }
            else
            {
                var nullBodyShiftId = jsonModel.Requests.First(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture)).Id;
                updateProps.Add("NullBodyShiftId", nullBodyShiftId);

                // The outbound acknowledgement does not honor the null Etag, 502 Bad Gateway is thrown if so.
                // Checking for the null eTag value, from the attributes in the payload and generate a non-null value in GenerateResponse method.
                var integrationResponse = GenerateResponse(
                    nullBodyShiftId,
                    HttpStatusCode.OK,
                    null,
                    null);

                return integrationResponse;
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

                integrationResponse = GenerateResponse(incomingOpenShift.Id, HttpStatusCode.OK, null, null);
            }
            else
            {
                var nullBodyIncomingOpenShiftId = jsonModel.Requests.First(x => x.Url.Contains("/openshifts/", StringComparison.InvariantCulture)).Id;
                updateProps.Add("NullBodyOpenShiftId", nullBodyIncomingOpenShiftId);
                integrationResponse = GenerateResponse(nullBodyIncomingOpenShiftId, HttpStatusCode.OK, null, null);
            }

            return integrationResponse;
        }

        /// <summary>
        /// Generates the response for each outbound request.
        /// </summary>
        /// <param name="itemId">Id for response.</param>
        /// <param name="statusCode">HttpStatusCode for the request been processed.</param>
        /// <param name="eTag">Etag based on response.</param>
        /// <param name="error">Forward error to shifts if any.</param>
        /// <returns>ShiftsIntegResponse.</returns>
        private static ShiftsIntegResponse GenerateResponse(string itemId, HttpStatusCode statusCode, string eTag, ResponseError error)
        {
            // The outbound acknowledgement does not honor the null Etag, 502 Bad Gateway is thrown if so.
            // Checking for the null eTag value, from the attributes in the payload.
            string responseEtag;
            if (string.IsNullOrEmpty(eTag))
            {
                responseEtag = GenerateNewGuid();
            }
            else
            {
                responseEtag = eTag;
            }

            var integrationResponse = new ShiftsIntegResponse()
            {
                Id = itemId,
                Status = (int)statusCode,
                Body = new Body
                {
                    Error = error,
                    ETag = responseEtag,
                },
            };

            return integrationResponse;
        }

        /// <summary>
        /// Generates the Guid for outbound call response.
        /// </summary>
        /// <returns>Returns newly generated GUID string.</returns>
        private static string GenerateNewGuid()
        {
            return Guid.NewGuid().ToString();
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
        /// <returns>Returns list of ShiftIntegResponse for request.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessOpenShiftRequest(RequestModel jsonModel, Dictionary<string, string> updateProps)
        {
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            var requestBody = jsonModel.Requests.First(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture)).Body;
            var requestState = requestBody != null ? requestBody["state"].Value<string>() : null;

            if (requestBody != null)
            {
                switch (requestState)
                {
                    // The Open shift request is submitted in Shifts and is pending with manager for approval.
                    case ApiConstants.ShiftsPending:
                        {
                            responseModelList = await this.ProcessOutboundOpenShiftRequestAsync(jsonModel, updateProps).ConfigureAwait(false);
                        }

                        break;

                    // The Open shift request is approved by manager.
                    case ApiConstants.ShiftsApproved:
                        {
                            responseModelList = await this.ProcessOpenShiftRequestApprovalAsync(jsonModel, updateProps).ConfigureAwait(false);
                        }

                        break;

                    // The code below would be when there is a decline. There is no need for further
                    // processing, the decline was made on Kronos side.
                    case ApiConstants.ShiftsDeclined:
                        {
                            var integrationResponse = new ShiftsIntegResponse();
                            foreach (var item in jsonModel.Requests)
                            {
                                integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, null, null);
                                responseModelList.Add(integrationResponse);
                            }
                        }

                        break;

                    // The code below handles the system declined request.
                    default:
                        {
                            var integrationResponse = new ShiftsIntegResponse();
                            foreach (var item in jsonModel.Requests)
                            {
                                integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, null, null);
                                responseModelList.Add(integrationResponse);
                            }
                        }

                        break;
                }
            }
            else
            {
                // Code below handles the delete open shift request.
                var integrationResponse = new ShiftsIntegResponse();
                foreach (var item in jsonModel.Requests)
                {
                    integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, null, null);
                    responseModelList.Add(integrationResponse);
                }
            }

            return responseModelList;
        }

        /// <summary>
        /// Process swap shift requests outbound calls.
        /// </summary>
        /// <param name="jsonModel">Incoming payload for the request been made in Shifts.</param>
        /// <param name="aadGroupId">AAD Group id.</param>
        /// <returns>Returns list of ShiftIntegResponse for request.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessSwapShiftRequest(RequestModel jsonModel, string aadGroupId)
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
                    var requestState = requestBody != null ? requestBody["state"].Value<string>() : null;
                    var requestAssignedTo = requestBody != null ? requestBody["assignedTo"].Value<string>() : null;

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
                        integrationResponseSwap = GenerateResponse(swapRequest.Id, HttpStatusCode.OK, swapRequest.ETag, null);
                        responseModelList.Add(integrationResponseSwap);
                    }

                    // Manager has approved the request in Kronos.
                    else if (requestState == ApiConstants.ShiftsApproved && requestAssignedTo == ApiConstants.ShiftsManager)
                    {
                        responseModelList = await this.ProcessSwapShiftRequestApprovalAsync(jsonModel, aadGroupId).ConfigureAwait(false);
                    }

                    // There is a System decline with the Swap Shift Request
                    else if (requestState == ApiConstants.Declined && requestAssignedTo == ApiConstants.System)
                    {
                        var systemDeclineSwapReqId = jsonModel.Requests.First(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)).Id;
                        ResponseError responseError = new ResponseError
                        {
                            Message = Resource.SystemDeclined,
                        };

                        integrationResponseSwap = GenerateResponse(systemDeclineSwapReqId, HttpStatusCode.OK, null, responseError);
                        responseModelList.Add(integrationResponseSwap);
                    }
                }
                else
                {
                    // Code below handles the delete swap shift request - should the record be deleted or not?
                    var deleteSwapRequestId = jsonModel.Requests.First(x => x.Url.Contains("/swapRequests/", StringComparison.InvariantCulture)).Id;

                    // Logging to telemetry the incoming cancelled request by FLW1.
                    this.telemetryClient.TrackTrace($"The Swap Shift Request: {deleteSwapRequestId} has been declined by FLW1.");

                    var entityToCancel = await this.swapShiftMappingEntityProvider.GetKronosReqAsync(deleteSwapRequestId).ConfigureAwait(false);

                    // Updating the ShiftsStatus to Cancelled.
                    entityToCancel.ShiftsStatus = ApiConstants.SwapShiftCancelled;

                    // Updating the entity accordingly
                    await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(entityToCancel).ConfigureAwait(false);

                    integrationResponseSwap = GenerateResponse(deleteSwapRequestId, HttpStatusCode.OK, null, null);
                    responseModelList.Add(integrationResponseSwap);
                }
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackTrace("Teams Controller swapRequests Exception" + ex.Message + "  " + ex.InnerException?.Message);
                this.telemetryClient.TrackTrace("Teams Controller swapRequests responseModelList Exception" + JsonConvert.SerializeObject(responseModelList));
                throw;
            }

            this.telemetryClient.TrackTrace("Teams Controller swapRequests responseModelList" + JsonConvert.SerializeObject(responseModelList));

            return responseModelList;
        }

        /// <summary>
        /// This method processes the open shift request approval, and proceeds to update the Azure table storage accordingly with the Shifts status
        /// of the open shift request, and also ensures that the ShiftMappingEntity table is properly in sync.
        /// </summary>
        /// <param name="jsonModel">The decrypted JSON payload.</param>
        /// <param name="updateProps">A dictionary of string, string that will be logged to ApplicationInsights.</param>
        /// <returns>A unit of execution.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessOpenShiftRequestApprovalAsync(RequestModel jsonModel, Dictionary<string, string> updateProps)
        {
            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            ShiftsIntegResponse integrationResponse = null;

            var finalOpenShiftReqObj = jsonModel?.Requests?.FirstOrDefault(x => x.Url.Contains("/openshiftrequests/", StringComparison.InvariantCulture));
            var finalOpenShiftObj = jsonModel?.Requests?.FirstOrDefault(x => x.Url.Contains("/openshifts/", StringComparison.InvariantCulture));
            var finalShiftObj = jsonModel?.Requests?.FirstOrDefault(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture));

            var finalShift = JsonConvert.DeserializeObject<Shift>(finalShiftObj.Body.ToString());
            var finalOpenShiftRequest = JsonConvert.DeserializeObject<OpenShiftRequestIS>(finalOpenShiftReqObj.Body.ToString());
            var finalOpenShift = JsonConvert.DeserializeObject<OpenShiftIS>(finalOpenShiftObj.Body.ToString());

            updateProps.Add("NewShiftId", finalShift.Id);
            updateProps.Add("GraphOpenShiftRequestId", finalOpenShiftRequest.Id);
            updateProps.Add("GraphOpenShiftId", finalOpenShift.Id);

            // Step 1 - Create the Kronos Unique ID.
            var kronosUniqueId = this.utility.CreateUniqueId(finalShift);

            this.telemetryClient.TrackTrace("KronosHash-OpenShiftRequestApproval-TeamsController: " + kronosUniqueId);

            try
            {
                this.telemetryClient.TrackTrace("Updating entities-OpenShiftRequestApproval started: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));

                // Step 1 - Get the temp shift record first by table scan against RowKey.
                var tempShiftRowKey = $"SHFT_PENDING_{finalOpenShiftRequest.Id}";
                var tempShiftEntity = await this.shiftMappingEntityProvider.GetShiftMappingEntityByRowKeyAsync(tempShiftRowKey).ConfigureAwait(false);

                // We need to check if the tempShift is not null because in the Open Shift Request controller, the tempShift was created
                // as part of the Graph API call to approve the Open Shift Request.
                if (tempShiftEntity != null)
                {
                    // Step 2 - Form the new shift record.
                    var shiftToInsert = new TeamsShiftMappingEntity()
                    {
                        RowKey = finalShift.Id,
                        KronosPersonNumber = tempShiftEntity.KronosPersonNumber,
                        KronosUniqueId = tempShiftEntity.KronosUniqueId,
                        PartitionKey = tempShiftEntity.PartitionKey,
                        AadUserId = tempShiftEntity.AadUserId,
                    };

                    // Step 3 - Save the new shift record.
                    await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(shiftToInsert, shiftToInsert.RowKey, shiftToInsert.PartitionKey).ConfigureAwait(false);

                    // Step 4 - Delete the temp shift record.
                    await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(tempShiftEntity).ConfigureAwait(false);
                }
                else
                {
                    // We are logging to ApplicationInsights that the tempShift entity could not be found.
                    this.telemetryClient.TrackTrace(string.Format(CultureInfo.InvariantCulture, Resource.EntityNotFoundWithRowKey, tempShiftRowKey));
                }

                // Logging to ApplicationInsights the OpenShiftRequestId.
                this.telemetryClient.TrackTrace("OpenShiftRequestId = " + finalOpenShiftRequest.Id);

                // Find the open shift request for which we update the ShiftsStatus to Approved.
                var openShiftRequestEntityToUpdate = await this.openShiftRequestMappingEntityProvider.GetOpenShiftRequestMappingEntityByOpenShiftIdAsync(
                    finalOpenShift.Id,
                    finalOpenShiftRequest.Id).ConfigureAwait(false);

                openShiftRequestEntityToUpdate.ShiftsStatus = finalOpenShiftRequest.State;

                // Update the open shift request to Approved in the ShiftStatus column.
                await this.openShiftRequestMappingEntityProvider.SaveOrUpdateOpenShiftRequestMappingEntityAsync(openShiftRequestEntityToUpdate).ConfigureAwait(false);

                // Delete the open shift entity accordingly from the OpenShiftEntityMapping table in Azure Table storage as the open shift request has been approved.
                await this.openShiftMappingEntityProvider.DeleteOrphanDataFromOpenShiftMappingByOpenShiftIdAsync(finalOpenShift.Id).ConfigureAwait(false);

                // Sending the acknowledgement for each subrequest from the request payload received from Shifts.
                foreach (var item in jsonModel.Requests)
                {
                    integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, null, null);
                    responseModelList.Add(integrationResponse);
                }

                this.telemetryClient.TrackTrace("Updating entities-OpenShiftRequestApproval complete: " + DateTime.Now.ToString(CultureInfo.InvariantCulture));
            }
            catch (Exception ex)
            {
                // Logging when the inner exception is not null - including the open shift request ID.
                if (ex.InnerException != null)
                {
                    this.telemetryClient.TrackTrace($"Shift mapping has failed for {finalOpenShiftRequest.Id}: " + ex.InnerException.ToString());
                    this.telemetryClient.TrackException(ex.InnerException);
                }

                // Logging the exception regardless, and making sure to add the open shift request ID as well.
                this.telemetryClient.TrackTrace($"Shift mapping has resulted in some type of error with the following: {ex.StackTrace.ToString(CultureInfo.InvariantCulture)}, happening with open shift request ID: {finalOpenShiftRequest.Id}");
                this.telemetryClient.TrackException(ex);

                throw;
            }

            return responseModelList;
        }

        /// <summary>
        /// This method further processes the Swap Shift request approval.
        /// </summary>
        /// <param name="jsonModel">The decryped JSON payload from Shifts/MS Graph.</param>
        /// <param name="aadGroupId">The team ID for which the Swap Shift request has been approved.</param>
        /// <returns>A unit of execution that contains the type of <see cref="ShiftsIntegResponse"/>.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessSwapShiftRequestApprovalAsync(RequestModel jsonModel, string aadGroupId)
        {
            List<ShiftsIntegResponse> swapShiftsIntegResponses = new List<ShiftsIntegResponse>();
            ShiftsIntegResponse integrationResponse = null;
            var swapShiftApprovalRes = from requests in jsonModel.Requests
                                       group requests by requests.Url;

            var swapShiftRequestObj = swapShiftApprovalRes?.FirstOrDefault(x => x.Key.Contains("/swapRequests/", StringComparison.InvariantCulture))?.First();
            var swapShiftRequest = JsonConvert.DeserializeObject<SwapRequest>(swapShiftRequestObj.Body.ToString());

            var postedShifts = jsonModel.Requests.Where(x => x.Url.Contains("/shifts/", StringComparison.InvariantCulture) && x.Method == "POST").ToList();

            if (swapShiftRequest != null)
            {
                var newShiftFirst = JsonConvert.DeserializeObject<Shift>(postedShifts.First().Body.ToString());
                var newShiftSecond = JsonConvert.DeserializeObject<Shift>(postedShifts.Last().Body.ToString());

                // Step 1 - Create the Kronos Unique ID.
                var kronosUniqueIdFirst = this.utility.CreateUniqueId(newShiftFirst);
                var kronosUniqueIdSecond = this.utility.CreateUniqueId(newShiftSecond);

                try
                {
                    var userMappingRecord = await this.userMappingProvider.GetUserMappingEntityAsyncNew(
                                       newShiftFirst?.UserId,
                                       aadGroupId).ConfigureAwait(false);

                    // When getting the month partition key, make sure to take into account the Kronos Time Zone as well
                    var provider = CultureInfo.InvariantCulture;
                    var actualStartDateTimeStr = this.utility.CalculateStartDateTime(
                        newShiftFirst.SharedShift.StartDateTime.Date).ToString("M/dd/yyyy", provider);
                    var actualEndDateTimeStr = this.utility.CalculateEndDateTime(
                        newShiftFirst.SharedShift.EndDateTime.Date).ToString("M/dd/yyyy", provider);

                    // Create the month partition key based on the finalShift object.
                    var monthPartitions = Common.Utility.GetMonthPartition(actualStartDateTimeStr, actualEndDateTimeStr);
                    var monthPartition = monthPartitions?.FirstOrDefault();

                    // Create the shift mapping entity based on the finalShift object also.
                    var shiftEntity = Common.Utility.CreateShiftMappingEntity(newShiftFirst, userMappingRecord, kronosUniqueIdFirst);
                    await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(
                        shiftEntity,
                        newShiftFirst.Id,
                        monthPartition).ConfigureAwait(false);

                    var userMappingRecordSec = await this.userMappingProvider.GetUserMappingEntityAsyncNew(
                                      newShiftSecond?.UserId,
                                      aadGroupId).ConfigureAwait(false);

                    // When getting the month partition key, make sure to take into account the Kronos Time Zone as well
                    var actualStartDateTimeStrSec = this.utility.CalculateStartDateTime(
                        newShiftSecond.SharedShift.StartDateTime).ToString("M/dd/yyyy", provider);
                    var actualEndDateTimeStrSec = this.utility.CalculateEndDateTime(
                        newShiftSecond.SharedShift.EndDateTime).ToString("M/dd/yyyy", provider);

                    // Create the month partition key based on the finalShift object.
                    var monthPartitionsSec = Common.Utility.GetMonthPartition(actualStartDateTimeStrSec, actualEndDateTimeStrSec);
                    var monthPartitionSec = monthPartitionsSec?.FirstOrDefault();

                    // Create the shift mapping entity based on the finalShift object also.
                    var shiftEntitySec = Common.Utility.CreateShiftMappingEntity(newShiftSecond, userMappingRecordSec, kronosUniqueIdSecond);
                    await this.shiftMappingEntityProvider.SaveOrUpdateShiftMappingEntityAsync(
                        shiftEntitySec,
                        newShiftSecond.Id,
                        monthPartitionSec).ConfigureAwait(false);

                    foreach (var item in jsonModel.Requests)
                    {
                        if (item.Url.Contains("/swapRequests/", StringComparison.InvariantCulture))
                        {
                            integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, swapShiftRequest.ETag, null);
                        }
                        else
                        {
                            integrationResponse = GenerateResponse(item.Id, HttpStatusCode.OK, null, null);
                        }

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
        /// Process outbound open shift request.
        /// </summary>
        /// <param name="jsonModel">Request payload.</param>
        /// <param name="updateProps">Telemetry properties.</param>
        /// <returns>Returns list of shiftIntegResponse.</returns>
        private async Task<List<ShiftsIntegResponse>> ProcessOutboundOpenShiftRequestAsync(
            RequestModel jsonModel,
            Dictionary<string, string> updateProps)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessOutboundOpenShiftRequestAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            List<ShiftsIntegResponse> responseModelList = new List<ShiftsIntegResponse>();
            ShiftsIntegResponse integrationResponse, openShiftReqSubmitResponse;

            if (jsonModel.Requests.First().Body != null)
            {
                // Having the JSON parsed as OpenShiftRequest.
                var openShiftRequest = JsonConvert.DeserializeObject<OpenShiftRequestIS>(
                    jsonModel.Requests.First().Body.ToString());

                updateProps.Add("OpenShiftRequestId", openShiftRequest.Id);
                updateProps.Add("OpenShiftId", openShiftRequest.OpenShiftId);

                this.telemetryClient.TrackTrace(Resource.IncomingOpenShiftRequest, updateProps);

                // Open shift request is declined by manager from kronos. The same response need to sync with Shifts.
                if (openShiftRequest.State == ApiConstants.Declined)
                {
                    integrationResponse = GenerateResponse(jsonModel.Requests.First().Id, HttpStatusCode.OK, null, null);
                    responseModelList.Add(integrationResponse);
                }
                else
                {
                    // This code will be submitting the Open Shift request to Kronos.
                    openShiftReqSubmitResponse = await this.openShiftRequestController.SubmitOpenShiftRequestToKronosAsync(openShiftRequest).ConfigureAwait(false);
                    responseModelList.Add(openShiftReqSubmitResponse);
                }
            }
            else
            {
                integrationResponse = GenerateResponse(jsonModel.Requests.First().Id, HttpStatusCode.OK, null, null);
                responseModelList.Add(integrationResponse);
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessOutboundOpenShiftRequestAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return responseModelList;
        }
    }
}