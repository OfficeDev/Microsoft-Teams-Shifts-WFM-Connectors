// <copyright file="SwapShiftController.cs" company="Microsoft">
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
    using Microsoft.Graph;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShift;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models;
    using Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels;
    using Newtonsoft.Json;
    using Approve = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals;
    using Logon = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon;

    /// <summary>
    /// This is the SwapShiftController.
    /// </summary>
    [Authorize(Policy = "AppID")]
    [Route("api/[controller]")]
    public class SwapShiftController : Controller
    {
        private readonly AppSettings appSettings;
        private readonly TelemetryClient telemetryClient;
        private readonly ILogonActivity logonActivity;
        private readonly IUserMappingProvider userMappingProvider;
        private readonly ISwapShiftActivity swapShiftActivity;
        private readonly ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider;
        private readonly Utility utility;
        private readonly IGraphUtility graphUtility;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly ITeamDepartmentMappingProvider teamDepartmentMappingProvider;
        private readonly IShiftMappingEntityProvider shiftMappingEntityProvider;
        private readonly BackgroundTaskWrapper taskWrapper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapShiftController"/> class.
        /// </summary>
        /// <param name="appSettings">Configuration DI.</param>
        /// <param name="telemetryClient">Telemetry Client.</param>
        /// <param name="logonActivity">Logon activity.</param>
        /// <param name="userMappingProvider">User To User Mapping Provider.</param>
        /// <param name="swapShiftActivity">swapshift activity.</param>
        /// <param name="swapShiftMappingEntityProvider">Swap Shift entity provider.</param>
        /// <param name="utility">UniqueId Utility DI.</param>
        /// <param name="graphUtility">GraphUtility DI.</param>
        /// <param name="httpClientFactory">The HTTP Client DI.</param>
        /// <param name="shiftMappingEntityProvider">Shift mapping entity provider DI.</param>
        /// <param name="teamDepartmentMappingProvider">Team department mapping provider.</param>
        /// <param name="taskWrapper">Wrapper class instance for BackgroundTask.</param>
        public SwapShiftController(
            AppSettings appSettings,
            TelemetryClient telemetryClient,
            ILogonActivity logonActivity,
            IUserMappingProvider userMappingProvider,
            ISwapShiftActivity swapShiftActivity,
            ISwapShiftMappingEntityProvider swapShiftMappingEntityProvider,
            Utility utility,
            IGraphUtility graphUtility,
            IHttpClientFactory httpClientFactory,
            ITeamDepartmentMappingProvider teamDepartmentMappingProvider,
            IShiftMappingEntityProvider shiftMappingEntityProvider,
            BackgroundTaskWrapper taskWrapper)
        {
            if (appSettings is null)
            {
                throw new ArgumentNullException(nameof(appSettings));
            }

            this.appSettings = appSettings;
            this.telemetryClient = telemetryClient;
            this.logonActivity = logonActivity;
            this.userMappingProvider = userMappingProvider;
            this.swapShiftActivity = swapShiftActivity;
            this.swapShiftMappingEntityProvider = swapShiftMappingEntityProvider;
            this.utility = utility;
            this.graphUtility = graphUtility;
            this.httpClientFactory = httpClientFactory;
            this.teamDepartmentMappingProvider = teamDepartmentMappingProvider;
            this.shiftMappingEntityProvider = shiftMappingEntityProvider;
            this.taskWrapper = taskWrapper;
        }

        /// <summary>
        /// This method generates a new GUID.
        /// </summary>
        /// <returns>A GUID string.</returns>
        public static string GenerateNewGuid()
        {
            return Guid.NewGuid().ToString();
        }

        /// <summary>
        /// This method will be called to submit the Swap Shift Request to Kronos.
        /// </summary>
        /// <param name="swapRequest">The incoming Swap Shift Request.</param>
        /// <param name="teamsId">User teams id.</param>
        /// <returns>A unit of execution.</returns>
        [AllowAnonymous]
        public async Task<ShiftsIntegResponse> SubmitSwapShiftRequestToKronosAsync(
            SwapRequest swapRequest,
            string teamsId)
        {
            if (swapRequest is null)
            {
                throw new ArgumentNullException(nameof(swapRequest));
            }

            var telemetryProps = new Dictionary<string, string>()
            {
                { "TeamsId", teamsId },
                { "SwapShiftSenderId", swapRequest.SenderUserId },
                { "SwapShiftRecipientId", swapRequest.RecipientUserId },
                { "SwapShiftSenderShiftId", swapRequest.SenderShiftId },
                { "SwapShiftRecipientShiftId", swapRequest.RecipientShiftId },
            };

            this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            var swapShiftResponse = new ShiftsIntegResponse();

            // Step 1 - Check if all the tokens are present and required configurations are done.
            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                // we need the mapped team in order to get the Kronos Time Zone which we need to convert the UTC times from Teams into
                // Kronos times, however, as the mapping table maps departments we will get back multiple mappings but realistically
                // can only deal with a single time zone for each team so we will take the first on the assumption that all the rest are
                // the same - if not we have an issue because Teams does not supply the department information we would need in order to
                // be able to select the correct one
                var mappedTeams = await this.teamDepartmentMappingProvider.GetMappedTeamDetailsAsync(teamsId).ConfigureAwait(false);
                var mappedTeam = mappedTeams.FirstOrDefault();
                var kronosTimeZone = string.IsNullOrEmpty(mappedTeam?.KronosTimeZone) ? this.appSettings.KronosTimeZone : mappedTeam.KronosTimeZone;

                // Step 1a - Get the user details of FLW1.
                var sender = await this.userMappingProvider.GetUserMappingEntityAsyncNew(swapRequest.SenderUserId, teamsId).ConfigureAwait(false);

                // Step 1b - Get the user details of FLW2.
                var recipient = await this.userMappingProvider.GetUserMappingEntityAsyncNew(swapRequest.RecipientUserId, teamsId).ConfigureAwait(false);

                // Step 1c - Get the sender's shift details.
                var senderShiftDetails = await this.GetShiftDetailsAsync(
                    allRequiredConfigurations,
                    teamsId,
                    swapRequest.SenderShiftId).ConfigureAwait(false);

                // Step 1d - Check if sender's shift is synced from Kronos WFC and is present in Shifts.
                if (senderShiftDetails != null && senderShiftDetails.SharedShift.StartDateTime > DateTime.UtcNow)
                {
                    this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - Successfully got the shift of {sender?.KronosUserName} - {senderShiftDetails?.Id}", telemetryProps);

                    // Step 1e - Get the recipient's shift details.
                    var recipientShiftDetails = await this.GetShiftDetailsAsync(
                        allRequiredConfigurations,
                        teamsId,
                        swapRequest.RecipientShiftId).ConfigureAwait(false);

                    // Step 1f - Check if recipient shift is present in Shifts and synced from Kronos.
                    if (recipientShiftDetails != null && recipientShiftDetails.SharedShift.StartDateTime > DateTime.UtcNow)
                    {
                        var shiftStartDate = senderShiftDetails.SharedShift.StartDateTime < recipientShiftDetails.SharedShift.StartDateTime ?
                                             senderShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                             .AddDays(-Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                             .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture)
                                             : recipientShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime.
                                             AddDays(-Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture)).ToString(
                                             this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                        var shiftEndDate = senderShiftDetails.SharedShift.StartDateTime > recipientShiftDetails.SharedShift.StartDateTime ?
                                           senderShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                           .AddDays(Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture)).ToString(
                                           this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture)
                                           : recipientShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                           .AddDays(Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                           .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                        this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - Successfully got the shift of {recipient?.KronosUserName} - {recipientShiftDetails?.Id} and now forming the request POST to Kronos WFC.", telemetryProps);

                        var commentTimeStamp = this.utility.UTCToKronosTimeZone(DateTime.UtcNow, mappedTeam.KronosTimeZone).ToString(CultureInfo.InvariantCulture);
                        var comments = XmlHelper.GenerateKronosComments(swapRequest.SenderMessage, this.appSettings.SenderSwapRequestCommentText, commentTimeStamp);

                        SwapShiftObj swapShiftObj = new SwapShiftObj
                        {
                            // Convert shift time to Kronos time zone.
                            Emp1FromDateTime = this.utility.UTCToKronosTimeZone(senderShiftDetails.SharedShift.StartDateTime, kronosTimeZone),
                            Emp1ToDateTime = this.utility.UTCToKronosTimeZone(senderShiftDetails.SharedShift.EndDateTime, kronosTimeZone),
                            Emp2FromDateTime = this.utility.UTCToKronosTimeZone(recipientShiftDetails.SharedShift.StartDateTime, kronosTimeZone),
                            Emp2ToDateTime = this.utility.UTCToKronosTimeZone(recipientShiftDetails.SharedShift.EndDateTime, kronosTimeZone),
                            QueryDateSpan = $"{shiftStartDate}-{shiftEndDate}",
                            RequestedToName = recipient?.KronosUserName,
                            RequestedToPersonNumber = recipient?.RowKey,
                            RequestorName = sender?.KronosUserName,
                            RequestorPersonNumber = sender?.RowKey,
                            SelectedAvailableShift = string.Empty,
                            SelectedJob = sender.PartitionKey.Replace("$", "/", StringComparison.InvariantCulture),
                            SelectedLocation = "All",
                            SelectedShiftToSwap = string.Empty,
                            Comments = comments,
                        };

                        // Step 2 - Draft the swap shift request.
                        var swapShiftDraftRes = await this.swapShiftActivity.DraftSwapShiftAsync(
                            allRequiredConfigurations.KronosSession,
                            swapShiftObj,
                            allRequiredConfigurations.WfmEndPoint).ConfigureAwait(false);

                        // Step 2a - Check if the call on line 226 is successful or not.
                        if (swapShiftDraftRes?.Status == ApiConstants.Success)
                        {
                            // Getting the Kronos Request ID.
                            var draftKronosRequestId = swapShiftDraftRes?.EmployeeRequestMgm.RequestItem.EmployeeSwapShiftRequestItems.Id;

                            this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - Successfully posted a DRAFT Swap Shift Request to Kronos WFC, RequestID: {draftKronosRequestId}", telemetryProps);

                            // Step 3 - Update the Swap Request from DRAFT state to OFFERED state.
                            var swapShiftOfferedeRes = await this.swapShiftActivity.SubmitSwapShiftAsync(
                                allRequiredConfigurations.KronosSession,
                                sender?.RowKey,
                                draftKronosRequestId,
                                swapShiftObj.QueryDateSpan,
                                new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

                            // Step 4 - Check if the Swap Shift Request status has been updated successfully.
                            if (swapShiftOfferedeRes?.Status == ApiConstants.Success)
                            {
                                this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - Successfully updated the status of the Swap Shift request from DRAFT to OFFERED, RequestID: {draftKronosRequestId}", telemetryProps);

                                // Step 5 - Save the mapping in SwapShiftEntityMapping table.
                                var senderShiftKronosDet = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapRequest.SenderShiftId).ConfigureAwait(false);
                                var recShiftKronosDet = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapRequest.RecipientShiftId).ConfigureAwait(false);
                                SwapShiftMappingEntity swapShiftMappingEntity = new SwapShiftMappingEntity
                                {
                                    AadUserId = swapRequest.SenderUserId,
                                    KronosReqId = draftKronosRequestId,
                                    KronosUniqueIdForOfferedShift = senderShiftKronosDet.KronosUniqueId,
                                    KronosUniqueIdForRequestedShift = recShiftKronosDet.KronosUniqueId,
                                    PartitionKey = GetPartitionKey(swapRequest.CreatedDateTime),
                                    RequestedKronosPersonNumber = recipient?.RowKey,
                                    RequestorKronosPersonNumber = sender?.RowKey,
                                    RowKey = swapRequest.Id,
                                    TeamsOfferedShiftId = swapRequest.SenderShiftId,
                                    TeamsRequestedShiftId = swapRequest.RecipientShiftId,
                                    KronosStatus = ApiConstants.Offered,
                                    ShiftsStatus = ApiConstants.Pending,
                                    ShiftsTeamId = teamsId,
                                };

                                await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(false);

                                swapShiftResponse.Status = (int)HttpStatusCode.OK;
                                swapShiftResponse.Id = swapRequest.Id;
                                swapShiftResponse.Body = new Body()
                                {
                                    Error = null,
                                    ETag = GenerateNewGuid(),
                                };
                            }
                            else
                            {
                                var swapShiftOfferedErrorMsg = swapShiftOfferedeRes?.Error.DetailErrors.Error.FirstOrDefault().Message;
                                this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - An error has happened in updating the DRAFT request to OFFERED: {swapShiftOfferedErrorMsg}", telemetryProps);

                                // Submit swap shift request failed in Kronos.
                                swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                                swapShiftResponse.Body = new Body()
                                {
                                    Error = new ResponseError
                                    {
                                        Code = Resource.KronosErrorStatus,
                                        Message = swapShiftOfferedErrorMsg,
                                    },

                                    ETag = null,
                                };
                                swapShiftResponse.Id = swapRequest.Id;
                            }
                        }
                        else
                        {
                            var swapShiftDraftErrorMsg = swapShiftDraftRes?.Error.DetailErrors.Error.FirstOrDefault().Message;

                            this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - An error has happened in the POST call to make the Swap Shift Request appear in DRAFT state: {swapShiftDraftErrorMsg}", telemetryProps);

                            // Draft swap shift request failed in Kronos.
                            swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                            swapShiftResponse.Body = new Body()
                            {
                                Error = new ResponseError
                                {
                                    Code = Resource.KronosErrorStatus,
                                    Message = swapShiftDraftErrorMsg,
                                },

                                ETag = null,
                            };
                            swapShiftResponse.Id = swapRequest.Id;
                        }
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - The recipient shift details are neither present in Shifts, nor has it been synced from Kronos: {recipient?.KronosUserName}, {recipientShiftDetails?.Id}", telemetryProps);

                        // Recipient shift is either not present in shift or not synced from Kronos.
                        swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                        swapShiftResponse.Body = new Body()
                        {
                            Error = new ResponseError
                            {
                                Code = Resource.RecipientShiftNotFound,
                                Message = Resource.RecipientShiftNotFound,
                            },

                            ETag = null,
                        };
                        swapShiftResponse.Id = swapRequest.Id;
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - The sender shift details are neither present in Shifts, nor hast it been synced from Kronos: {sender?.KronosUserName}, {senderShiftDetails?.Id}", telemetryProps);

                    // Sender shift is either not present in shift or not synced from Kronos.
                    swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                    swapShiftResponse.Body = new Body()
                    {
                        Error = new ResponseError
                        {
                            Code = Resource.SenderShiftNotFound,
                            Message = Resource.SenderShiftNotFound,
                        },
                        ETag = null,
                    };
                    swapShiftResponse.Id = swapRequest.Id;
                }
            }
            else
            {
                this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} - {Resource.SetUpNotDoneMessage}", telemetryProps);

                // Either All the tokens are not present or the configuration is not done properly.
                swapShiftResponse.Body = new Body()
                {
                    Error = new ResponseError
                    {
                        Code = Resource.SetUpNotDoneMessage,
                        Message = Resource.SetUpNotDoneMessage,
                    },
                };
                swapShiftResponse.Id = swapRequest.Id;
                swapShiftResponse.Status = StatusCodes.Status500InternalServerError;

                this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

                return swapShiftResponse;
            }

            this.telemetryClient.TrackTrace($"{Resource.SubmitSwapShiftRequestToKronosAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            return swapShiftResponse;
        }

        /// <summary>
        /// This method will be called to submit the Swap Shift approval to Kronos.
        /// </summary>
        /// <param name="swapRequest">The incoming Swap Shift Request.</param>
        /// <param name="teamsId">User teams id.</param>
        /// <param name="kronosTimeZone">The Kronos time zone.</param>
        /// <returns>A unit of execution.</returns>
        public async Task<ShiftsIntegResponse> ApproveOrDeclineSwapShiftRequestToKronosAsync(SwapRequest swapRequest, string teamsId, string kronosTimeZone)
        {
            if (swapRequest is null)
            {
                throw new ArgumentNullException(nameof(swapRequest));
            }

            var telemetryProps = new Dictionary<string, string>()
            {
                { "TeamsId", teamsId },
                { "SwapShiftSenderId", swapRequest.SenderUserId },
                { "SwapShiftRecipientId", swapRequest.RecipientUserId },
                { "SwapShiftSenderShiftId", swapRequest.SenderShiftId },
                { "SwapShiftRecipientShiftId", swapRequest.RecipientShiftId },
            };

            this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            Logon.Response loginKronosResult;

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            var swapShiftResponse = new ShiftsIntegResponse();

            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                var loginKronos = this.logonActivity.LogonAsync(
                    allRequiredConfigurations.KronosUserName,
                    allRequiredConfigurations.KronosPassword,
                    new Uri(allRequiredConfigurations.WfmEndPoint));

                loginKronosResult = await loginKronos.ConfigureAwait(false);

                var sender = await this.userMappingProvider.GetUserMappingEntityAsyncNew(swapRequest.SenderUserId, teamsId).ConfigureAwait(false);
                var recipient = await this.userMappingProvider.GetUserMappingEntityAsyncNew(swapRequest.RecipientUserId, teamsId).ConfigureAwait(false);

                var senderShiftDetails = await this.GetShiftDetailsAsync(
                        allRequiredConfigurations,
                        teamsId,
                        swapRequest.SenderShiftId).ConfigureAwait(false);

                // Step 1 - Check if sender shift is present in Shifts and synced from Kronos.
                if (senderShiftDetails != null)
                {
                    this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - Successfully got the shift of {sender?.KronosUserName} - {senderShiftDetails?.Id}", telemetryProps);

                    var recipientShiftDetails = await this.GetShiftDetailsAsync(
                    allRequiredConfigurations,
                    teamsId,
                    swapRequest.RecipientShiftId).ConfigureAwait(false);

                    // Step 2 - Check if recipient shift is present in Shifts and synced from Kronos.
                    if (recipientShiftDetails != null)
                    {
                        this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - Successfully got the shift of {recipient?.KronosUserName} - {recipientShiftDetails?.Id}", telemetryProps);

                        var shiftStartDate = senderShiftDetails.SharedShift.StartDateTime < recipientShiftDetails.SharedShift.StartDateTime ?
                                       senderShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                       .AddDays(-Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                       .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture)
                                       : recipientShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime.
                                       AddDays(-Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture)).ToString(
                                       this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                        var shiftEndDate = senderShiftDetails.SharedShift.StartDateTime > recipientShiftDetails.SharedShift.StartDateTime ?
                                           senderShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                           .AddDays(Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture)).ToString(
                                           this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture)
                                           : recipientShiftDetails.SharedShift.StartDateTime.GetValueOrDefault().DateTime
                                           .AddDays(Convert.ToInt16(this.appSettings.CorrectedDateSpanForOutboundCalls, CultureInfo.InvariantCulture))
                                           .ToString(this.appSettings.KronosQueryDateSpanFormat, CultureInfo.InvariantCulture);

                        var kronosReqId = await this.swapShiftMappingEntityProvider.GetKronosReqAsync(swapRequest.Id).ConfigureAwait(false);

                        var commentTimeStamp = this.utility.UTCToKronosTimeZone(DateTime.UtcNow, kronosTimeZone).ToString(CultureInfo.InvariantCulture);
                        var comments = XmlHelper.GenerateKronosComments(swapRequest.RecipientActionMessage, this.appSettings.RecipientSwapRequestCommentText, commentTimeStamp);

                        // Step 3 - If the request state is declined, then post status as Refused in Kronos. Otherwise, the status remains as Submitted.
                        var swapShiftSubmitRes = await this.swapShiftActivity.SubmitApprovalAsync(
                            loginKronosResult.Jsession,
                            kronosReqId.KronosReqId,
                            kronosReqId.RequestedKronosPersonNumber,
                            swapRequest.State == "Declined" ? ApiConstants.Refused : ApiConstants.Submitted,
                            $"{shiftStartDate}-{shiftEndDate}",
                            comments,
                            new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

                        // Step 4 - If the request has successfully updated the status.
                        if (swapShiftSubmitRes?.Status == ApiConstants.Success)
                        {
                            this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - Successfully updated {kronosReqId?.KronosReqId} to SUBMITTED with the Kronos WFC operation status: {swapShiftSubmitRes?.Status}", telemetryProps);

                            // On successful approval from FLW2, update the mapping to Submitted state from Offered state.
                            var senderShiftKronosDet = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapRequest.SenderShiftId).ConfigureAwait(false);
                            var recShiftKronosDet = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapRequest.RecipientShiftId).ConfigureAwait(false);

                            SwapShiftMappingEntity swapShiftMappingEntity = new SwapShiftMappingEntity
                            {
                                AadUserId = swapRequest.SenderUserId,
                                KronosReqId = kronosReqId.KronosReqId,
                                KronosUniqueIdForOfferedShift = senderShiftKronosDet.KronosUniqueId,
                                KronosUniqueIdForRequestedShift = recShiftKronosDet.KronosUniqueId,
                                PartitionKey = GetPartitionKey(swapRequest.CreatedDateTime),
                                RequestedKronosPersonNumber = recipient?.RowKey,
                                RequestorKronosPersonNumber = sender?.RowKey,
                                RowKey = swapRequest.Id,
                                TeamsOfferedShiftId = swapRequest.SenderShiftId,
                                TeamsRequestedShiftId = swapRequest.RecipientShiftId,
                                KronosStatus = swapRequest.State == ApiConstants.Declined ? ApiConstants.Refused : ApiConstants.Submitted,
                                ShiftsStatus = swapRequest.State == ApiConstants.Declined ? ApiConstants.Declined : ApiConstants.Pending,
                                ShiftsTeamId = teamsId,
                            };

                            await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(false);
                            swapShiftResponse = new ShiftsIntegResponse()
                            {
                                Id = swapRequest.Id,
                                Status = StatusCodes.Status200OK,
                                Body = new Body()
                                {
                                    Error = null,
                                    ETag = swapRequest.ETag,
                                },
                            };
                        }
                        else
                        {
                            var swapShiftSubmitErrorMsg = swapShiftSubmitRes?.Error.DetailErrors.Error.FirstOrDefault().Message;
                            this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - An error has happened in posting the SUBMITTED request to Kronos WFC: {swapShiftSubmitErrorMsg}", telemetryProps);

                            // If submit to Kronos fails.
                            swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                            swapShiftResponse.Body = new Body()
                            {
                                Error = new ResponseError
                                {
                                    Code = Resource.KronosErrorStatus,
                                    Message = swapShiftSubmitErrorMsg,
                                },

                                ETag = null,
                            };
                            swapShiftResponse.Id = swapRequest.Id;

                            var entityToKeepAtOffered = await this.swapShiftMappingEntityProvider.GetKronosReqAsync(swapRequest.Id).ConfigureAwait(false);
                            entityToKeepAtOffered.KronosStatus = ApiConstants.Offered;
                            await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(entityToKeepAtOffered).ConfigureAwait(false);
                        }
                    }
                    else
                    {
                        this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - The recipient shift details are neither present in Shifts, nor has it been synced from Kronos: {recipient?.KronosUserName}, {recipientShiftDetails?.Id}", telemetryProps);

                        // Recipient shift is neither present in Shifts nor synced from Kronos.
                        swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                        swapShiftResponse.Body = new Body()
                        {
                            Error = new ResponseError
                            {
                                Code = Resource.RecipientShiftNotFound,
                                Message = Resource.RecipientShiftNotFound,
                            },
                            ETag = null,
                        };
                        swapShiftResponse.Id = swapRequest.Id;
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - The sender shift details are neither present in Shifts, nor hast it been synced from Kronos: {sender?.KronosUserName}, {senderShiftDetails?.Id}", telemetryProps);

                    // Sender shift is neither present in Shifts nor synced from Kronos.
                    swapShiftResponse.Status = StatusCodes.Status500InternalServerError;
                    swapShiftResponse.Body = new Body()
                    {
                        Error = new ResponseError
                        {
                            Code = Resource.SenderShiftNotFound,
                            Message = Resource.SenderShiftNotFound,
                        },
                        ETag = null,
                    };
                    swapShiftResponse.Id = swapRequest.Id;
                }
            }
            else
            {
                this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} - {Resource.SetUpNotDoneMessage}", telemetryProps);

                // Either all the tokens are not present nor the configurations are properly done.
                swapShiftResponse.Body = new Body()
                {
                    Error = new ResponseError
                    {
                        Code = Resource.SetUpNotDoneMessage,
                        Message = Resource.SetUpNotDoneMessage,
                    },
                };
                swapShiftResponse.Id = swapRequest.Id;
                swapShiftResponse.Status = StatusCodes.Status500InternalServerError;

                this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            }

            this.telemetryClient.TrackTrace($"{Resource.ApproveOrDeclineSwapShiftRequestToKronosAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return swapShiftResponse;
        }

        public async Task<ShiftsIntegResponse> RetractOfferedShiftAsync(
            SwapShiftMappingEntity map)
        {
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);
            var swapShiftResponse = new ShiftsIntegResponse();
            var user = map.RequestorKronosPersonNumber;

            this.utility.SetQuerySpan(
                Convert.ToBoolean(true, CultureInfo.InvariantCulture),
                out var shiftStartDate,
                out var shiftEndDate);

            if (map.KronosStatus == ApiConstants.Submitted)
            {
                user = map.RequestedKronosPersonNumber;
            }

            if ((bool)allRequiredConfigurations?.IsAllSetUpExists)
            {
                var response = await this.swapShiftActivity.SubmitRetractionRequest(
                    allRequiredConfigurations.KronosSession,
                    map.KronosReqId,
                    user,
                    $"{shiftStartDate} - {shiftEndDate}",
                    new Uri(allRequiredConfigurations.WfmEndPoint)).ConfigureAwait(false);

                if (response?.Status == ApiConstants.Success)
                {
                    map.KronosStatus = ApiConstants.Retract;
                    await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(map).ConfigureAwait(false);

                    swapShiftResponse = new ShiftsIntegResponse()
                    {
                        Id = map.KronosReqId,
                        Status = (int)HttpStatusCode.OK,
                        Body = new Body()
                        {
                            Error = null,
                            ETag = null,
                        },
                    };
                }
                else
                {
                    swapShiftResponse.Status = (int)HttpStatusCode.InternalServerError;
                    swapShiftResponse.Body = new Body()
                    {
                        Error = new ResponseError
                        {
                            Code = Resource.KronosErrorStatus,
                            Message = response.Error.Message,
                        },

                        ETag = null,
                    };
                    swapShiftResponse.Id = map.KronosReqId;
                }
            }

            return swapShiftResponse;
        }

        /// <summary>
        /// start swap shift sync from Kronos and push it to Shifts.
        /// </summary>
        /// <param name="isRequestFromLogicApp">Checks if request is coming from logic app or portal.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        internal async Task ProcessSwapShiftsAsync(string isRequestFromLogicApp)
        {
            this.telemetryClient.TrackTrace($"{Resource.ProcessSwapShiftsAsync} start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);

            var telemetryProps = new Dictionary<string, string>()
                {
                    { "CallingMethod", "UpdateTeam" },
                };

            this.utility.SetQuerySpan(
                Convert.ToBoolean(isRequestFromLogicApp, CultureInfo.InvariantCulture),
                out var shiftStartDate,
                out var shiftEndDate);

            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(shiftStartDate, shiftEndDate);

            string queryDateSpan = $"{shiftStartDate} - {shiftEndDate}";

            // Check if all the tokens are present and configuration is done.
            if (allRequiredConfigurations != null && (bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                // Get the users whose request is in pending state in Shifts.
                List<SwapShiftMappingEntity> pendingSwapShiftEntities = await this.swapShiftMappingEntityProvider.GetPendingRequest().ConfigureAwait(false);

                // If there are the requests which are in pending state.
                if (pendingSwapShiftEntities.Any())
                {
                    // list of Kronos person numbers whose request is in pending state in database.
                    List<string> kronosPersonNumbers = pendingSwapShiftEntities.Select(c => c.RequestorKronosPersonNumber).Distinct().ToList();

                    // Gets all the approved swap shifts.
                    var swapshiftDetails = await this.GetSwapShiftResultsByUserAsync(
                    kronosPersonNumbers,
                    allRequiredConfigurations.WFIId,
                    allRequiredConfigurations.WfmEndPoint,
                    allRequiredConfigurations.KronosSession,
                    queryDateSpan).ConfigureAwait(false);

                    // Gets all the refused swap shifts.
                    var swapshiftRefusedDetails = await this.GetSwapShiftRefusedResultsByUserAsync(
                        kronosPersonNumbers,
                        allRequiredConfigurations.WFIId,
                        allRequiredConfigurations.WfmEndPoint,
                        allRequiredConfigurations.KronosSession,
                        queryDateSpan).ConfigureAwait(false);

                    // Gets all the retracted swap shifts.
                    var swapshiftRetractedDetails = await this.GetSwapShiftRetractedResultsByUserAsync(
                        kronosPersonNumbers,
                        allRequiredConfigurations.WFIId,
                        allRequiredConfigurations.WfmEndPoint,
                        allRequiredConfigurations.KronosSession,
                        queryDateSpan).ConfigureAwait(false);

                    if (swapshiftDetails != null && swapshiftDetails.RequestMgmt?.RequestItems?.SwapShiftRequestItem?.Count > 0)
                    {
                        // Process for only those requests which are approved in Kronos but still pending in our db.
                        foreach (var swapShiftEntity in pendingSwapShiftEntities)
                        {
                            var approvedData = swapshiftDetails.RequestMgmt.RequestItems.SwapShiftRequestItem.Where(c => c.Id == swapShiftEntity.KronosReqId).FirstOrDefault();

                            if (approvedData != null)
                            {
                                // Fetch notes for the swap shift request.
                                var notes = approvedData.RequestStatusChanges?.RequestStatusChange;
                                var note = notes.Select(c => c.Comments).FirstOrDefault()?.Comment?.FirstOrDefault()?.Notes?.Note?.FirstOrDefault().Text;

                                await this.AddSwapShiftApprovalAsync(allRequiredConfigurations, swapShiftEntity, note).ConfigureAwait(false);
                            }
                        }
                    }

                    if (swapshiftRefusedDetails != null && swapshiftRefusedDetails.RequestMgmt?.RequestItems?.SwapShiftRequestItem?.Count > 0)
                    {
                        // Process for only those requests which are refused in Kronos but still pending in our db.
                        foreach (var swapShiftEntity in pendingSwapShiftEntities)
                        {
                            var refusedData = swapshiftRefusedDetails.RequestMgmt.RequestItems.SwapShiftRequestItem.Where(c => c.Id == swapShiftEntity.KronosReqId).FirstOrDefault();
                            if (refusedData != null)
                            {
                                // Fetch notes for the swap shift request.
                                var notes = refusedData.RequestStatusChanges?.RequestStatusChange;
                                var note = notes.Select(c => c.Comments).FirstOrDefault()?.Comment?.FirstOrDefault()?.Notes?.Note?.FirstOrDefault().Text;

                                await this.DeclineSwapShiftRequestAsync(
                                    allRequiredConfigurations,
                                    swapShiftEntity?.ShiftsTeamId,
                                    swapShiftEntity,
                                    refusedData.StatusName,
                                    note).ConfigureAwait(false);
                            }
                        }
                    }

                    if (swapshiftRetractedDetails != null && swapshiftRetractedDetails.RequestMgmt?.RequestItems?.SwapShiftRequestItem?.Count > 0)
                    {
                        // Process for only those requests which are retracted in Kronos but still pending in our db.
                        foreach (var swapShiftEntity in pendingSwapShiftEntities)
                        {
                            var retractedData = swapshiftRetractedDetails.RequestMgmt.RequestItems.SwapShiftRequestItem.Where(c => c.Id == swapShiftEntity.KronosReqId).FirstOrDefault();

                            if (retractedData != null)
                            {
                                // Fetch notes for the swap shift request.
                                var notes = retractedData.RequestStatusChanges?.RequestStatusChange;
                                var note = notes.Select(c => c.Comments).FirstOrDefault()?.Comment?.FirstOrDefault()?.Notes?.Note?.FirstOrDefault().Text;

                                await this.DeclineSwapShiftRequestAsync(
                                    allRequiredConfigurations,
                                    swapShiftEntity?.ShiftsTeamId,
                                    swapShiftEntity,
                                    retractedData?.StatusName,
                                    note).ConfigureAwait(false);
                            }
                        }
                    }
                }
                else
                {
                    this.telemetryClient.TrackTrace(Resource.NoSwapReqPending);
                }
            }

            this.telemetryClient.TrackTrace($"{Resource.ProcessSwapShiftsAsync} ended at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}" + " for isRequestFromLogicApp: " + isRequestFromLogicApp);
        }

        /// <summary>
        /// Creates and sends the relevant request to approve or deny a swap shift request.
        /// </summary>
        /// <param name="kronosReqId">The Kronos request id for the swap shift request.</param>
        /// <param name="kronosUserId">The Kronos user id for the assigned user.</param>
        /// <param name="swapShiftMapping">The mapping for the swap shift.</param>
        /// <param name="approved">Whether the swap shift should be approved (true) or denied (false).</param>
        /// <returns>Returns a bool that represents whether the request was a success (true) or not (false).</returns>
        internal async Task<bool> ApproveSwapShiftInKronos(
            string kronosReqId,
            string kronosUserId,
            SwapShiftMappingEntity swapShiftMapping,
            Comments comments,
            bool approved)
        {
            var provider = CultureInfo.InvariantCulture;
            this.telemetryClient.TrackTrace($"ApproveSwapShiftInKronos start at: {DateTime.Now.ToString("o", provider)}");

            this.utility.SetQuerySpan(true, out var swapShiftStartDate, out var swapShiftEndDate);

            var swapShiftQueryDateSpan = $"{swapShiftStartDate}-{swapShiftEndDate}";

            // Get all the necessary prerequisites.
            var allRequiredConfigurations = await this.utility.GetAllConfigurationsAsync().ConfigureAwait(false);

            // Check whether date range are in correct format.
            var isCorrectDateRange = Utility.CheckDates(swapShiftStartDate, swapShiftEndDate);

            if ((bool)allRequiredConfigurations?.IsAllSetUpExists && isCorrectDateRange)
            {
                var response = await this.swapShiftActivity.ApproveOrDenySwapShiftRequestsForUserAsync(
                        new Uri(allRequiredConfigurations.WfmEndPoint),
                        allRequiredConfigurations.KronosSession,
                        swapShiftQueryDateSpan,
                        kronosUserId,
                        approved,
                        comments,
                        kronosReqId).ConfigureAwait(false);

                if (response.Status == "Success" && approved)
                {
                    swapShiftMapping.KronosStatus = ApiConstants.ApprovedStatus;
                    await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMapping).ConfigureAwait(false);
                    return true;
                }

                if (response.Status == "Success" && !approved)
                {
                    swapShiftMapping.KronosStatus = ApiConstants.Refused;
                    await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMapping).ConfigureAwait(false);
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Method that will calculate the monthwise partition key.
        /// </summary>
        /// <param name="swapRequestDateTime">The date/time object of the Swap Request entity.</param>
        /// <returns>A string which represent the monthwise partition key (i.e. 2_2020 for February 2020).</returns>
        private static string GetPartitionKey(DateTime swapRequestDateTime)
        {
            var month = swapRequestDateTime.Month;
            var year = swapRequestDateTime.Year;

            return month + "_" + year;
        }

        /// <summary>
        /// This method obtains the Shift details from Microsoft Graph.
        /// </summary>
        /// <param name="allRequiredConfigurations">All required configuration.</param>
        /// <param name="teamsId">The team ID that the user belongs to.</param>
        /// <param name="shiftId">The Shift ID being requested.</param>
        /// <returns>A unit of exection that contains a type of <see cref="Graph.Shift"/>.</returns>
        private async Task<Graph.Shift> GetShiftDetailsAsync(SetupDetails allRequiredConfigurations, string teamsId, string shiftId)
        {
            var telemetryProps = new Dictionary<string, string>
            {
                { "ShiftDetails for ShiftId: ", shiftId },
            };

            this.telemetryClient.TrackTrace($"GetShiftDetailsAsync start at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            // Check if shift is synced from Kronos.
            if (await this.swapShiftMappingEntityProvider.CheckShiftExistanceAsync(shiftId).ConfigureAwait(false))
            {
                // Get the shift details using graph call.
                var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.GraphConfigurationDetails.ShiftsAccessToken);

                var requestUrl = $"teams/{teamsId}/schedule/shifts/{shiftId}";

                var response = await this.graphUtility.SendHttpRequest(allRequiredConfigurations.GraphConfigurationDetails, httpClient, HttpMethod.Get, requestUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    this.telemetryClient.TrackTrace($"GetShiftDetailsAsync end at {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

                    var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return JsonConvert.DeserializeObject<Microsoft.Graph.Shift>(responseString);
                }

                return null;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Method that posts an approved Swap Shift request to Shifts.
        /// </summary>
        /// <param name="allRequiredConfigurations">Configuration details.</param>
        /// <param name="swapShiftMappingEntity">The Swap Shift entity from database.</param>
        /// <param name="note">The necessary notes that are applicable to the Swap Shift request approval.</param>
        private async Task AddSwapShiftApprovalAsync(SetupDetails allRequiredConfigurations, SwapShiftMappingEntity swapShiftMappingEntity, string note)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "TeamsId", swapShiftMappingEntity.ShiftsTeamId },
                { "SwapShiftRequestId", swapShiftMappingEntity.RowKey },
                { "KronosSwapShiftRequestId", swapShiftMappingEntity.KronosReqId },
            };

            this.telemetryClient.TrackTrace($"{Resource.AddSwapShiftApprovalAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.GraphConfigurationDetails.ShiftsAccessToken);

            // Send Passthrough header to indicate the sender of request in outbound call.
            httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfigurations.WFIId);

            var requestUrl = $"teams/{swapShiftMappingEntity.ShiftsTeamId}/schedule/swapShiftsChangeRequests/{swapShiftMappingEntity.RowKey}/approve";

            ApproveMsg approveMsg = new ApproveMsg { Message = note };
            var requestString = JsonConvert.SerializeObject(approveMsg);

            var response = await this.graphUtility.SendHttpRequest(allRequiredConfigurations.GraphConfigurationDetails, httpClient, HttpMethod.Post, requestUrl, requestString).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                swapShiftMappingEntity.KronosStatus = ApiConstants.ApprovedStatus;
                swapShiftMappingEntity.ShiftsStatus = ApiConstants.ApprovedStatus;

                await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(true);

                var offeredShift = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapShiftMappingEntity.TeamsOfferedShiftId).ConfigureAwait(false);
                var requestedShift = await this.swapShiftMappingEntityProvider.GetShiftDetailsAsync(swapShiftMappingEntity.TeamsRequestedShiftId).ConfigureAwait(false);

                telemetryProps.Add("Delete OfferedShiftId", offeredShift?.RowKey);
                telemetryProps.Add("Delete RequestedShiftId", requestedShift?.RowKey);

                // Delete the earlier mapping from ShiftMappingEntity for sender as well as for recipient.
                if (offeredShift != null)
                {
                    await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(offeredShift).ConfigureAwait(false);
                }

                if (requestedShift != null)
                {
                    await this.shiftMappingEntityProvider.DeleteOrphanDataFromShiftMappingAsync(requestedShift).ConfigureAwait(false);
                }

                this.telemetryClient.TrackTrace($"{Resource.AddSwapShiftApprovalAsync} - MS Graph Approval of {swapShiftMappingEntity?.RowKey} has succeeded with status code: {(int)response.StatusCode}", telemetryProps);
            }
            else
            {
                swapShiftMappingEntity.KronosStatus = ApiConstants.Declined;
                swapShiftMappingEntity.ShiftsStatus = ApiConstants.Declined;

                telemetryProps.Add("SwapShiftRequestMessage", response?.RequestMessage.ToString());
                telemetryProps.Add("SwapShiftReasonPhrase", response.ReasonPhrase);

                await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(false);

                this.telemetryClient.TrackTrace($"{Resource.AddSwapShiftApprovalAsync} - MS Graph Approval of {swapShiftMappingEntity?.RowKey} did not succeed with status code: {(int)response.StatusCode}", telemetryProps);
            }

            this.telemetryClient.TrackTrace($"{Resource.AddSwapShiftApprovalAsync} ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
        }

        /// <summary>
        /// Method that will decline a Swap Shift request in Shifts.
        /// </summary>
        /// <param name="allRequiredConfigurations">Configuration details.</param>
        /// <param name="teamsId">The Team ID for which the Swap Shift request is to be declined.</param>
        /// <param name="swapShiftMappingEntity">The Swap Shift entity from database.</param>
        /// <param name="status">The status to post as part of the decline.</param>
        /// <param name="note">Applicable notes for the decline of the Swap Shift request.</param>
        private async Task DeclineSwapShiftRequestAsync(SetupDetails allRequiredConfigurations, string teamsId, SwapShiftMappingEntity swapShiftMappingEntity, string status, string note)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "TeamId", teamsId },
                { "SwapShiftRequestId", swapShiftMappingEntity?.RowKey },
            };

            this.telemetryClient.TrackTrace($"{Resource.DeclineSwapShiftRequestAsync} starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            var httpClient = this.httpClientFactory.CreateClient("ShiftsAPI");
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", allRequiredConfigurations.GraphConfigurationDetails.ShiftsAccessToken);

            // Send Passthrough header to indicate the sender of request in outbound call.
            httpClient.DefaultRequestHeaders.Add("X-MS-WFMPassthrough", allRequiredConfigurations.WFIId);

            var requestUrl = $"teams/{teamsId}/schedule/swapShiftsChangeRequests/{swapShiftMappingEntity.RowKey}/decline";

            ApproveMsg declineMsg = new ApproveMsg { Message = note };
            var requestString = JsonConvert.SerializeObject(declineMsg);

            var response = await this.graphUtility.SendHttpRequest(allRequiredConfigurations.GraphConfigurationDetails, httpClient, HttpMethod.Post, requestUrl, requestString).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                this.telemetryClient.TrackTrace($"{Resource.DeclineSwapShiftRequestAsync} - MS Graph Decline of {swapShiftMappingEntity?.RowKey} has succeeded with status code: {(int)response.StatusCode}", telemetryProps);
                swapShiftMappingEntity.KronosStatus = status;
                swapShiftMappingEntity.ShiftsStatus = status;
                await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(false);
            }
            else
            {
                this.telemetryClient.TrackTrace($"{Resource.DeclineSwapShiftRequestAsync} - MS Graph Decline of {swapShiftMappingEntity?.RowKey} did not succeed with status code: {(int)response.StatusCode}", telemetryProps);
                swapShiftMappingEntity.KronosStatus = status;
                swapShiftMappingEntity.ShiftsStatus = status;
                await this.swapShiftMappingEntityProvider.AddOrUpdateSwapShiftMappingAsync(swapShiftMappingEntity).ConfigureAwait(false);
            }

            this.telemetryClient.TrackTrace($"DeclineSwapShiftRequestAsync ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
        }

        /// <summary>
        /// This method will get the approved Swap Shift results per user.
        /// </summary>
        /// <param name="kronosPersonNumbers">The Kronos Person Number.</param>
        /// <param name="wFIID">Workforce integration id.</param>
        /// <param name="wFEndpoint">Kronos endpoint.</param>
        /// <param name="jsession">The JSession (Kronos "token").</param>
        /// /// <param name="queryDateSpan">Date span for Kronos to fetch the swap shift details.</param>
        /// <returns>A unit of execution that contains the approved response.</returns>
        private async Task<Approve.SwapShiftData.Response> GetSwapShiftResultsByUserAsync(
            List<string> kronosPersonNumbers,
            string wFIID,
            string wFEndpoint,
            string jsession,
            string queryDateSpan)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                 { "WorkforceIntegrationId", wFIID },
                 { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace($"GetSwapShiftResultsByUserAsync start at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            var swapshiftRequests = await this.swapShiftActivity.GetAllSwapShiftRequestDetailsAsync(
                new Uri(wFEndpoint),
                jsession,
                queryDateSpan,
                kronosPersonNumbers,
                ApiConstants.ApprovedStatus).ConfigureAwait(false);

            // Failed to get approved swap shifts details from Kronos.
            if (swapshiftRequests?.Status != ApiConstants.Success)
            {
                telemetryProps.Add("kronosPersonNumbers", string.Join(",", kronosPersonNumbers));
                telemetryProps.Add("kronosErrorStatus", swapshiftRequests?.Status);
                this.telemetryClient.TrackTrace($"GetSwapShiftResultsByUserAsync end at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            }

            this.telemetryClient.TrackTrace($"GetSwapShiftResultsByUserAsync end at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return swapshiftRequests;
        }

        /// <summary>
        /// This method will get the approved Swap Shift results per user.
        /// </summary>
        /// <param name="kronosPersonNumbers">The Kronos Person Number.</param>
        /// <param name="wFIID">The workforce integration id.</param>
        /// <param name="wFEndpoint">Kronos Endpoint.</param>
        /// <param name="jsession">The JSession (Kronos "token").</param>
        /// <param name="queryDateSpan">Query date span for Kronos details.</param>
        /// <returns>A unit of execution that contains the refused response.</returns>
        private async Task<Approve.SwapShiftData.Response> GetSwapShiftRefusedResultsByUserAsync(
            List<string> kronosPersonNumbers,
            string wFIID,
            string wFEndpoint,
            string jsession,
            string queryDateSpan)
        {
            var telemetryProps = new Dictionary<string, string>()
             {
                 { "WorkforceIntegrationId", wFIID },
                 { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
             };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            var swapshiftRequests = await this.swapShiftActivity.GetAllSwapShiftRequestDetailsAsync(
                new Uri(wFEndpoint),
                jsession,
                queryDateSpan,
                kronosPersonNumbers,
                ApiConstants.Refused).ConfigureAwait(false);

            // Failed to get refused swap shifts details from Kronos.
            if (swapshiftRequests?.Status != ApiConstants.Success)
            {
                telemetryProps.Add("kronosPersonNumbers", string.Join(", ", kronosPersonNumbers));
                telemetryProps.Add("kronosError", swapshiftRequests?.Status);
                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);
            }

            return swapshiftRequests;
        }

        /// <summary>
        /// This method will get the approved Swap Shift results per user.
        /// </summary>
        /// <param name="kronosPersonNumbers">The Kronos Person Number.</param>
        /// <param name="wFIID">The workforce integration id.</param>
        /// <param name="wFEndpoint">Kronos Endpoint.</param>
        /// <param name="jsession">The JSession (Kronos "token").</param>
        /// <param name="queryDateSpan">Query date span for Kronos details.</param>
        /// <returns>A unit of execution that contains the retracted response.</returns>
        private async Task<Approve.SwapShiftData.Response> GetSwapShiftRetractedResultsByUserAsync(
            List<string> kronosPersonNumbers,
            string wFIID,
            string wFEndpoint,
            string jsession,
            string queryDateSpan)
        {
            var telemetryProps = new Dictionary<string, string>()
             {
                 { "WorkforceIntegrationId", wFIID },
                 { "CallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
             };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            var swapshiftRequests = await this.swapShiftActivity.GetAllSwapShiftRequestDetailsAsync(
               new Uri(wFEndpoint),
               jsession,
               queryDateSpan,
               kronosPersonNumbers,
               ApiConstants.Retract).ConfigureAwait(false);

            // Failed to get retracted swap shifts details from Kronos.
            if (swapshiftRequests?.Status != ApiConstants.Success)
            {
                telemetryProps.Add("kronosPersonNumbers", string.Join(", ", kronosPersonNumbers));
                telemetryProps.Add("kronosError", swapshiftRequests?.Status);
                this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);
            }

            return swapshiftRequests;
        }
    }
}