// <copyright file="SwapShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShift
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.SubmitRequest;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift;
    using Microsoft.Teams.App.KronosWfc.Service;
    using FetchApprove = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals;
    using SubmitRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;
    using SubmitResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.SubmitSwapShift;
    using TimeOff = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests;

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

        /// <summary>
        /// Fecth swap shift request details for displaying history.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="personNumbers">List of person numbers whose request is approved.</param>
        /// <param name="statusName">Request status name.</param>
        /// <returns>Request details response object.</returns>
        public async Task<FetchApprove.SwapShiftData.Response> GetSwapShiftRequestDetailsAsync(
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
                ApiConstants.SoapEnvOpen,
                xmlSwapShiftRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            FetchApprove.SwapShiftData.Response swapResponse = this.ProcessSwapShiftApproved(tupleResponse.Item1);

            return swapResponse;
        }

        /// <summary>
        /// The method to create the Swap Shift Request in Draft state.
        /// </summary>
        /// <param name="jSession">The jSession.</param>
        /// <param name="obj">The input SwapShiftObj.</param>
        /// <param name="apiEndpoint">The Kronos API Endpoint.</param>
        /// <returns>A unit of execution that contains the response object.</returns>
        public async Task<SubmitResponse.Response> DraftSwapShiftAsync(
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
                    ApiConstants.SoapEnvOpen,
                    xmlRequest,
                    ApiConstants.SoapEnvClose,
                    jSession).ConfigureAwait(false);

                SubmitResponse.Response response = this.ProcessSwapShiftDraftResponse(tupleResponse.Item1);

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

        /// <summary>
        /// The method to post a SwapShift request in a offered state.
        /// </summary>
        /// <param name="jSession">Kronos session.</param>
        /// <param name="personNumber">The Kronos personNumber.</param>
        /// <param name="reqId">The reqId of swap shift request.</param>
        /// <param name="querySpan">The querySpan.</param>
        /// <param name="comment">The comment for request.</param>
        /// <param name="endpointUrl">Endpoint Kronos URL.</param>
        /// <returns>request to send to Kronos.</returns>
        public async Task<SubmitResponse.Response> SubmitSwapShiftAsync(
            string jSession,
            string personNumber,
            string reqId,
            string querySpan,
            string comment,
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
                string xmlRequest = this.CreateSwapShiftSubmitRequest(
                    personNumber,
                    reqId,
                    querySpan,
                    comment);

                var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                    endpointUrl,
                    ApiConstants.SoapEnvOpen,
                    xmlRequest,
                    ApiConstants.SoapEnvClose,
                    jSession).ConfigureAwait(false);

                SubmitResponse.Response response = this.ProcessSwapShiftDraftResponse(tupleResponse.Item1);

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

        /// <summary>
        /// The method to post a SwapShift request in an offered state.
        /// </summary>
        /// <param name="jSession">The JSession (Kronos "token").</param>
        /// <param name="reqId">The SwapShift Request ID.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="status">The status to update.</param>
        /// <param name="querySpan">The query date span.</param>
        /// <param name="comment">The comment to be applied wherever applicable.</param>
        /// <param name="endpointUrl">The Kronos WFC API Endpoint URL.</param>
        /// <returns>A unit of execution that contains the response object.</returns>
        public async Task<Response> SubmitApprovalAsync(
            string jSession,
            string reqId,
            string personNumber,
            string status,
            string querySpan,
            string comment,
            Uri endpointUrl)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "KronosRequestId", reqId },
                { "KronosPersonNumber", personNumber },
                { "KronosStatus", status },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitApprovalAsync starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);

            string xmlRequest = this.CreateApprovalRequest(
                personNumber,
                reqId,
                status,
                querySpan,
                comment);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endpointUrl,
                ApiConstants.SoapEnvOpen,
                xmlRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Response swapShiftResponse = this.ProcessSwapShiftResponse(tupleResponse.Item1);

            this.telemetryClient.TrackTrace($"SwapShiftActivity - SubmitApprovalAsync ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return swapShiftResponse;
        }

        /// <summary>
        /// Processes the response for the approve SwapShift.
        /// </summary>
        /// <param name="strResponse">The incoming response XML string.</param>
        /// <returns>A response object.</returns>
        private FetchApprove.SwapShiftData.Response ProcessSwapShiftApproved(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<FetchApprove.SwapShiftData.Response>(xResponse.ToString());
        }

        /// <summary>
        /// This method will create the XML request string for a Swap Shift Request.
        /// </summary>
        /// <param name="swapShiftObj">The current swap shift object.</param>
        /// <returns>A string that represents the XML request.</returns>
        private string CreateSwapShiftDraftRequest(SwapShiftObj swapShiftObj)
        {
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest starts: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            var requestorEmployee = new Employee
            {
                PersonIdentity = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.PersonIdentity
                {
                    PersonNumber = swapShiftObj.RequestorPersonNumber,
                },
            };

            var requestedToEmployee = new Employee
            {
                PersonIdentity = new Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline.PersonIdentity
                {
                    PersonNumber = swapShiftObj.RequestedToPersonNumber,
                },
            };

            SubmitRequest.Request rq = new SubmitRequest.Request()
            {
                Action = ApiConstants.AddRequests,
                EmployeeRequestMgmt = new EmployeeRequestMgmt
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
                                    EndDateTime = swapShiftObj.Emp1ToDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, System.Globalization.CultureInfo.InvariantCulture),
                                    OrgJobPath = swapShiftObj.SelectedJob,
                                    StartDateTime = swapShiftObj.Emp1FromDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, System.Globalization.CultureInfo.InvariantCulture),
                                },
                            },
                            RequestedShift = new RequestedShift
                            {
                                ShiftRequestItem = new ShiftRequestItem
                                {
                                    Employee = requestedToEmployee,
                                    EndDateTime = swapShiftObj.Emp2ToDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, System.Globalization.CultureInfo.InvariantCulture),
                                    OrgJobPath = swapShiftObj.SelectedJob,
                                    StartDateTime = swapShiftObj.Emp2FromDateTime.ToString(ApiConstants.KronosAcceptableDateFormat, System.Globalization.CultureInfo.InvariantCulture),
                                },
                            },
                        },
                    },
                },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest: {rq.XmlSerialize().ToString(CultureInfo.InvariantCulture)}");
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftDraftRequest ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return rq.XmlSerialize();
        }

        /// <summary>
        /// This method will process the response after a draft request has been submitted.
        /// </summary>
        /// <param name="strResponse">The response string.</param>
        /// <returns>A response object is returned.</returns>
        private SubmitResponse.Response ProcessSwapShiftDraftResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<SubmitResponse.Response>(xResponse.ToString());
        }

        /// <summary>
        /// The method to create a SwapShift request in a offered state.
        /// </summary>
        /// <param name="personNumber">The Kronos personNumber.</param>
        /// <param name="reqId">The reqId of swap shift request.</param>
        /// <param name="querySpan">The querySpan.</param>
        /// <param name="comment">The comment for request.</param>
        /// <returns>Request to send to Kronos.</returns>
        private string CreateSwapShiftSubmitRequest(
            string personNumber,
            string reqId,
            string querySpan,
            string comment)
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
                    RequestStatusChanges = new RequestManagementSwap.RequestStatusChanges(),
                },
            };

            rq.RequestMgmt.RequestStatusChanges.RequestStatusChange = new RequestManagementSwap.RequestStatusChange[]
            {
                new RequestManagementSwap.RequestStatusChange
                {
                    Comments = comment == null ? null : new Comments()
                    {
                        Comment = new Comment[]
                        {
                            new Comment
                            {
                                CommentText = ApiConstants.SwapShiftComment,
                                Notes = new Notes
                                {
                                    Note = new Note
                                    {
#pragma warning disable CA1303 // Do not pass literals as localized parameters
                                        Text = ApiConstants.SwapShiftNoteText,
#pragma warning restore CA1303 // Do not pass literals as localized parameters
                                    },
                                },
                            },
                        },
                    },
                    RequestId = reqId,
                    ToStatusName = ApiConstants.Offered,
                },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftSubmitRequest: {rq.XmlSerialize().ToString(CultureInfo.InvariantCulture)}");
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateSwapShiftSubmitRequest ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

            return rq.XmlSerialize();
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

            List<TimeOff.PersonIdentity> personIdentities = new List<TimeOff.PersonIdentity>();
            foreach (var personNumber in personNumbers)
            {
                TimeOff.PersonIdentity personIdentity = new TimeOff.PersonIdentity
                {
                    PersonNumber = personNumber,
                };

                personIdentities.Add(personIdentity);
            }

            TimeOff.Request rq = new TimeOff.Request
            {
                Action = ApiConstants.RetrieveWithDetails,
                RequestMgmt = new TimeOff.RequestMgmt
                {
                    QueryDateSpan = $"{queryDateSpan}",
                    RequestFor = ApiConstants.SwapShiftRequest,
                    StatusName = statusName,
                    Employees = new Employees
                    {
                        PersonIdentity = personIdentities,
                    },
                },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApproveSwapShiftRequests: {rq.XmlSerialize().ToString(CultureInfo.InvariantCulture)}", telemetryProps);
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApproveSwapShiftRequests ends: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}", telemetryProps);
            return rq.XmlSerialize();
        }

        /// <summary>
        /// Process response for request details.
        /// </summary>
        /// <param name="strResponse">Response received from request for TOR detail.</param>
        /// <returns>Response object.</returns>
        private Response ProcessSwapShiftResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Response>(xResponse.ToString());
        }

        /// <summary>
        /// This method creates the approval request.
        /// </summary>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="reqId">The SwapShift Request ID.</param>
        /// <param name="status">The incoming status.</param>
        /// <param name="querySpan">The query date span.</param>
        /// <param name="comment">The comment to apply if applicable.</param>
        /// <returns>A string that represents the request XML.</returns>
        private string CreateApprovalRequest(
            string personNumber,
            string reqId,
            string status,
            string querySpan,
            string comment)
        {
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApprovalRequest starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");

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
                    RequestStatusChanges = new RequestManagementSwap.RequestStatusChanges(),
                },
            };

            rq.RequestMgmt.RequestStatusChanges.RequestStatusChange = new RequestManagementSwap.RequestStatusChange[]
            {
                new RequestManagementSwap.RequestStatusChange
                {
                    Comments = comment == null ? null : new Comments()
                    {
                        Comment = new Comment[]
                        {
                            new Comment
                            {
                                CommentText = ApiConstants.SwapShiftComment,
                                Notes = new Notes
                                {
                                    Note = new Note
                                    {
                                        Text = comment,
                                    },
                                },
                            },
                        },
                    },
                    RequestId = reqId,
                    ToStatusName = status,
                },
            };

            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApprovalRequest: {rq.XmlSerialize().ToString(CultureInfo.InvariantCulture)}");
            this.telemetryClient.TrackTrace($"SwapShiftActivity - CreateApprovalRequest starts at: {DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)}");
            return rq.XmlSerialize();
        }
    }
}