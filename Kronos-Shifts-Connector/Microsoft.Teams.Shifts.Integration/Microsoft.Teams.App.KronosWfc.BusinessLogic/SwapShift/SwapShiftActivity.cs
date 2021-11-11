﻿// <copyright file="SwapShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShift
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;
    using Microsoft.Teams.App.KronosWfc.Service;
    using static Common.XmlHelper;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using CommonResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using FetchApprovalResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData.Response;
    using SubmitRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;
    using SubmitResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift.Response;
    using SwapShiftResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.Response;

    /// <summary>
    /// This class implements all the methods that are defined in <see cref="ISwapShiftActivity"/>.
    /// </summary>
    public class SwapShiftActivity : ISwapShiftActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapShiftActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public SwapShiftActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <inheritdoc/>
        public async Task<FetchApprovalResponse> GetAllSwapShiftRequestDetailsAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            List<string> personNumbers,
            string statusName)
        {
            if (personNumbers is null)
            {
                throw new ArgumentNullException(nameof(personNumbers));
            }

            var xmlSwapShiftRequest = this.CreateApprovedSwapShiftRequests(
                personNumbers,
                queryDateSpan,
                statusName);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                SoapEnvOpen,
                xmlSwapShiftRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<FetchApprovalResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<SubmitResponse> DraftSwapShiftAsync(
            string jSession,
            SwapShiftObj obj,
            string apiEndpoint)
        {
            this.telemetryClient.TrackTrace($"SwapShiftActivity - DraftSwapShiftAsync starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            try
            {
                string xmlRequest = this.CreateSwapShiftDraftRequest(obj);

                var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                    new Uri(apiEndpoint),
                    SoapEnvOpen,
                    xmlRequest,
                    SoapEnvClose,
                    jSession).ConfigureAwait(false);

                SubmitResponse response = tupleResponse.ProcessResponse<SubmitResponse>(this.telemetryClient);

                this.telemetryClient.TrackTrace($"SwapShiftActivity - DraftSwapShiftAsync ends at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                this.telemetryClient.TrackException(ex);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<SubmitResponse> SubmitSwapShiftAsync(
            string jSession,
            string personNumber,
            string reqId,
            string querySpan,
            Uri endpointUrl)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "KronosRequestId", reqId },
                { "KronosPersonNumber", personNumber },
                { "QueryDateSpan", querySpan },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitSwapShiftAsync starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            try
            {
                string xmlRequest = this.CreateSwapShiftSubmitRequest(personNumber, reqId, querySpan);

                var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                    endpointUrl,
                    SoapEnvOpen,
                    xmlRequest,
                    SoapEnvClose,
                    jSession).ConfigureAwait(false);

                SubmitResponse response = tupleResponse.ProcessResponse<SubmitResponse>(this.telemetryClient);

                this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitSwapShiftAsync ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
                return response;
            }
#pragma warning disable CA1031 // Do not catch general exception types
            catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
            {
                this.telemetryClient.TrackException(ex, telemetryProps);
                return null;
            }
        }

        /// <inheritdoc/>
        public async Task<SwapShiftResponse> SubmitApprovalAsync(
            string jSession,
            string reqId,
            string personNumber,
            string status,
            string querySpan,
            Comments comments,
            Uri endpointUrl)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "KronosRequestId", reqId },
                { "KronosPersonNumber", personNumber },
                { "KronosStatus", status },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitApprovalAsync starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            string xmlRequest = CommonRequests.CreateUpdateStatusRequest(personNumber, reqId, status, querySpan, comments);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endpointUrl,
                SoapEnvOpen,
                xmlRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitApprovalAsync ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return tupleResponse.ProcessResponse<SwapShiftResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CommonResponse> SubmitRetractionRequest(
            string jSession,
            string reqId,
            string personNumber,
            string querySpan,
            Uri endpointUrl)
        {
            string xmlRequest = this.CreateRetractionRequest(querySpan, personNumber, reqId);
            var response = await this.apiHelper.SendSoapPostRequestAsync(endpointUrl, SoapEnvOpen, xmlRequest, SoapEnvClose, jSession).ConfigureAwait(false);
            return response.ProcessResponse<CommonResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<FetchApprovalResponse> ApproveOrDenySwapShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            Comments comments,
            string kronosId)
        {
            var status = approved ? ApiConstants.ApprovedStatus : ApiConstants.Refused;

            var swapShiftApprovalRequest = CommonRequests.CreateUpdateStatusRequest(kronosPersonNumber, kronosId, status, queryDateSpan, comments);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                SoapEnvOpen,
                swapShiftApprovalRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace(
                "ShiftSwapActivity - ApproveOrDenySwapShiftRequestsForUserAsync",
                new Dictionary<string, string>()
                {
                    { "Response",  tupleResponse.Item1 }
                });

            return tupleResponse.ProcessResponse<FetchApprovalResponse>(this.telemetryClient);
        }

        /// <summary>
        /// This method will create the XML request string for a Swap Shift Request.
        /// </summary>
        /// <param name="swapShiftObj">The current swap shift object.</param>
        /// <returns>A string that represents the XML request.</returns>
        private string CreateSwapShiftDraftRequest(SwapShiftObj swapShiftObj)
        {
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            var requestorEmployee = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.Employee
            {
                PersonIdentity = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.PersonIdentity
                {
                    PersonNumber = swapShiftObj.RequestorPersonNumber,
                },
            };

            var requestedToEmployee = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.Employee
            {
                PersonIdentity = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.PersonIdentity
                {
                    PersonNumber = swapShiftObj.RequestedToPersonNumber,
                },
            };

            SubmitRequest.Request rq = new SubmitRequest.Request()
            {
                Action = ApiConstants.AddRequests,
                EmployeeRequestMgmt = new SubmitRequest.EmployeeRequestMgmt
                {
                    Employee = requestorEmployee,
                    QueryDateSpan = swapShiftObj.QueryDateSpan,
                    RequestItems = new RequestItems
                    {
                        SwapShiftRequestItem = new SwapShiftRequestItem
                        {
                            Employee = requestorEmployee,
                            RequestFor = ApiConstants.SwapShiftRequest,
                            OfferedShift = new OfferedShift
                            {
                                ShiftRequestItem = new ShiftRequestItem
                                {
                                    Employee = requestorEmployee,

                                    // The Kronos is expecting only these formats to be serialized,
                                    // however we are converting the correct date format while creating actual shifts, timeoff etc.
                                    EndDateTime = swapShiftObj.Emp1ToDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, CultureInfo.InvariantCulture),
                                    OrgJobPath = swapShiftObj.SelectedJob,
                                    StartDateTime = swapShiftObj.Emp1FromDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, CultureInfo.InvariantCulture),
                                },
                            },
                            RequestedShift = new RequestedShift
                            {
                                ShiftRequestItem = new ShiftRequestItem
                                {
                                    Employee = requestedToEmployee,
                                    EndDateTime = swapShiftObj.Emp2ToDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, CultureInfo.InvariantCulture),
                                    OrgJobPath = swapShiftObj.SelectedJob,
                                    StartDateTime = swapShiftObj.Emp2FromDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, CultureInfo.InvariantCulture),
                                },
                            },
                            Comments = swapShiftObj.Comments,
                        },
                    },
                },
            };

            var request = rq.XmlSerialize();
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest: {request.ToString(CultureInfo.InvariantCulture)}");
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return request;
        }

        /// <summary>
        /// The method to create a SwapShift request in a offered state.
        /// </summary>
        /// <param name="personNumber">The Kronos personNumber.</param>
        /// <param name="reqId">The reqId of swap shift request.</param>
        /// <param name="querySpan">The querySpan.</param>
        /// <returns>Request to send to Kronos.</returns>
        private string CreateSwapShiftSubmitRequest(string personNumber, string reqId, string querySpan)
        {
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftSubmitRequest starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            RequestManagementSwap.Request rq = new RequestManagementSwap.Request()
            {
                Action = ApiConstants.UpdateStatus,
                RequestMgmt = new RequestManagementSwap.RequestMgmt()
                {
                    Employees = new RequestManagementSwap.Employee()
                    {
                        PersonIdentity = new RequestManagementSwap.PersonIdentity()
                        {
                            PersonNumber = personNumber,
                        },
                    },
                    QueryDateSpan = querySpan,
                    RequestStatusChanges = new RequestManagementSwap.RequestStatusChanges()
                    {
                        RequestStatusChange = new RequestManagementSwap.RequestStatusChange[]
                        {
                            new RequestManagementSwap.RequestStatusChange
                            {
                                RequestId = reqId,
                                ToStatusName = ApiConstants.Offered,
                            },
                        },
                    },
                },
            };

            var request = rq.XmlSerialize();
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftSubmitRequest: {request.ToString(CultureInfo.InvariantCulture)}");
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftSubmitRequest ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return request;
        }

        /// <summary>
        /// Create approved SwapShift requests for a list of people.
        /// </summary>
        /// <param name="personNumbers">List of person numbers whose request is approved.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="statusName">Request statusName.</param>
        /// <returns>XML request string.</returns>
        private string CreateApprovedSwapShiftRequests(
            List<string> personNumbers,
            string queryDateSpan,
            string statusName)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "QueryDateSpan", queryDateSpan },
                { "KronosStatus", statusName },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApproveSwapShiftRequests starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            List<PersonIdentity> personIdentities = new List<PersonIdentity>();
            foreach (var personNumber in personNumbers)
            {
                PersonIdentity personIdentity = new PersonIdentity
                {
                    PersonNumber = personNumber,
                };

                personIdentities.Add(personIdentity);
            }

            var rq = new Models.RequestEntities.SwapShift.GetAllApprovedRequests.GetAllApprovedRequest
            {
                Action = RetrieveWithDetails,
                RequestMgmt = new Models.RequestEntities.Common.RequestMgmt
                {
                    QueryDateSpan = $"{queryDateSpan}",
                    RequestFor = SwapShiftRequest,
                    StatusName = statusName,
                    Employees = new Employees
                    {
                        PersonIdentity = personIdentities,
                    },
                },
            };

            var request = rq.XmlSerialize();
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApproveSwapShiftRequests: {request.ToString(CultureInfo.InvariantCulture)}", telemetryProps);
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApproveSwapShiftRequests ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return request;
        }

        /// <summary>
        /// Creates a retraction request for a given shift swap request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="id">The Kronos id of the request.</param>
        /// <returns>XML request string.</returns>
        private string CreateRetractionRequest(
            string queryDateSpan,
            string personNumber,
            string id)
        {
            var request = new RetractRequest()
            {
                Action = RetractRequests,
                EmployeeRequestMgmt = new Models.RequestEntities.Common.EmployeeRequestMgmt()
                {
                    QueryDateSpan = queryDateSpan,
                    Employee = new Employee() { PersonIdentity = new PersonIdentity() { PersonNumber = personNumber } },
                    RequestIds = new RequestIds()
                    {
                        RequestId = new List<RequestId> { new RequestId { Id = id } },
                    },
                },
            };

            return request.XmlSerialize();
        }
    }
}