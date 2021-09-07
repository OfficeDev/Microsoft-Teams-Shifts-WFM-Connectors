// <copyright file="OpenShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;
    using Microsoft.Teams.App.KronosWfc.Service;
    using CommonSegments = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common.ShiftSegments;
    using CreateOpenShiftRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.CreateOpenShift;
    using OpenShiftApproveDecline = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline;
    using OpenShiftSubmitReq = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;
    using Request = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement.Request;

    /// <summary>
    /// This class implements methods that are defined in <see cref="IOpenShiftActivity"/>.
    /// </summary>
    public class OpenShiftActivity : IOpenShiftActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="OpenShiftActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">The mechanisms to capture telemetry.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public OpenShiftActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <inheritdoc/>
        public async Task<Models.ResponseEntities.OpenShift.Batch.Response> CreateOpenShiftAsync(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string openShiftLabel,
            string startTime,
            string endTime)
        {
            var createOpenShiftRequest = this.CreateOpenShiftRequest(
                shiftStartDate,
                shiftEndDate,
                overADateBorder,
                jobPath,
                openShiftLabel,
                startTime,
                endTime);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                ApiConstants.SoapEnvOpen,
                createOpenShiftRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return response.ProcessResponse<Models.ResponseEntities.OpenShift.Batch.Response>(this.telemetryClient);
        }

        /// <summary>
        /// Fecth open shift request details for displaying history.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="kronosPersonNumber">The Kronos Person Number.</param>
        /// <returns>Request details response object.</returns>
        public async Task<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response> GetApprovedOrDeclinedOpenShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");

            var xmlTimeOffRequest = this.CreateOpenShiftsApprovedDeclinedRequest(queryDateSpan, kronosPersonNumber);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response response = this.ProcessOpenShiftsApprovedDeclinedResponse(tupleResponse.Item1);

            return response;
        }

        /// <summary>
        /// Approves or Denies the request.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="kronosPersonNumber">The Kronos Person Number.</param>
        /// <param name="approved">Whether the request needs to be approved or denied.</param>
        /// <param name="kronosId">The Kronos id of the request.</param>
        /// <returns>Request details response object.</returns>
        public async Task<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response> ApproveOrDenyOpenShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId)
        {
            var xmlOpenShiftApprovalRequest = this.CreateApproveOrDeclineRequest(queryDateSpan, kronosPersonNumber, approved, kronosId);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlOpenShiftApprovalRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace(
                "OpenShiftActivity - ApproveOrDenyOpenShiftRequestsForUserAsync",
                new Dictionary<string, string>()
                {
                    { "Response", tupleResponse.Item1 }
                });

            return this.ProcessOpenShiftsApprovedDeclinedResponse(tupleResponse.Item1);
        }

        /// <summary>
        /// Method that will have the create the DraftOpenShift.
        /// </summary>
        /// <param name="tenantId">The TenantId.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="obj">The Open Shift object.</param>
        /// <param name="endPointUrl">The Kronos API endpoint.</param>
        /// <returns>A unit of execution that contains a response.</returns>
        public async Task<Models.ResponseEntities.OpenShiftRequest.Response> PostDraftOpenShiftRequestAsync(
            string tenantId,
            string jSession,
            OpenShiftObj obj,
            Uri endPointUrl)
        {
            if (obj is null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            string xmlRequest = this.CreateOpenShiftDraftRequest(obj);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Models.ResponseEntities.OpenShiftRequest.Response response =
                this.ProcessCreateDraftOpenShiftResponse(tupleResponse.Item1);

            return response;
        }

        /// <summary>
        /// Posts the status update for an Open Shift Request from DRAFT to SUBMITTED such that the request shows in the Request Manager.
        /// </summary>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="requestId">The request Id.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="comment">The comment to apply.</param>
        /// <param name="endpointUrl">The Kronos API endpoint.</param>
        /// <param name="jSession">The jSession token.</param>
        /// <returns>A task that contains a type of <see cref="Models.ResponseEntities.OpenShiftRequest.Response"/>.</returns>
        public async Task<Models.ResponseEntities.OpenShiftRequest.Response> PostOpenShiftRequestStatusUpdateAsync(
            string personNumber,
            string requestId,
            string queryDateSpan,
            string comment,
            Uri endpointUrl,
            string jSession)
        {
            string xmlRequest = this.CreateOpenShiftRequestStatusUpdate(
                personNumber,
                requestId,
                queryDateSpan,
                comment);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endpointUrl,
                ApiConstants.SoapEnvOpen,
                xmlRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Models.ResponseEntities.OpenShiftRequest.Response response =
                this.ProcessCreateDraftOpenShiftResponse(tupleResponse.Item1);

            return response;
        }

        /// <summary>
        /// Gets the batch response for the batch open shift request.
        /// </summary>
        /// <param name="endpointUrl">Kronos WFC API Endpoint.</param>
        /// <param name="jSession">The Kronos jSession.</param>
        /// <param name="orgJobPathsBatchList">The batch of Org Job Paths.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <returns>A response object that's part of a unit of execution.</returns>
        public async Task<Models.ResponseEntities.OpenShift.Batch.Response> GetOpenShiftDetailsInBatchAsync(
            Uri endpointUrl,
            string jSession,
            List<string> orgJobPathsBatchList,
            string queryDateSpan)
        {
            var xmlRequest = this.CreateOpenShiftDetailsBatchRequest(
                queryDateSpan,
                orgJobPathsBatchList);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endpointUrl,
                ApiConstants.SoapEnvOpen,
                xmlRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Models.ResponseEntities.OpenShift.Batch.Response response = this.ProcessOpenShiftBatchResponse(tupleResponse.Item1);
            return response;
        }

        private static List<Models.RequestEntities.OpenShift.ScheduleOS> BuildScheduleList(
            string queryDateSpan,
            List<string> orgJobPaths)
        {
            List<Models.RequestEntities.OpenShift.ScheduleOS> outputList = new List<Models.RequestEntities.OpenShift.ScheduleOS>();
            foreach (var item in orgJobPaths)
            {
                outputList.Add(new Models.RequestEntities.OpenShift.ScheduleOS()
                {
                    QueryDateSpan = queryDateSpan,
                    OrgJobPath = item,
                });
            }

            return outputList;
        }

        /// <summary>
        /// Method that will process the SwapShiftDraftResponse.
        /// </summary>
        /// <param name="strResponse">The response string.</param>
        /// <returns>A response object.</returns>
        private Models.ResponseEntities.OpenShiftRequest.Response ProcessCreateDraftOpenShiftResponse(
            string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Models.ResponseEntities.OpenShiftRequest.Response>(xResponse.ToString());
        }

        /// <summary>
        /// The method that converts a response string to the response object.
        /// </summary>
        /// <param name="strResponse">The SOAP Response string.</param>
        /// <returns>A model that is type of <see cref="Models.ResponseEntities.OpenShift.Batch.Response"/>.</returns>
        private Models.ResponseEntities.OpenShift.Batch.Response ProcessOpenShiftBatchResponse(
            string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Models.ResponseEntities.OpenShift.Batch.Response>(xResponse.ToString());
        }

        private string CreateOpenShiftRequest(
            string openShiftStartDate,
            string openShiftEndDate,
            bool overADateBorder,
            string jobPath,
            string openShiftLabel,
            string startTime,
            string endTime)
        {
            // If the open shift spans across 2 days then secondDayNumber needs to be 2.
            var secondDayNumber = overADateBorder ? 2 : 1;

            var req = new CreateOpenShiftRequest
            {
                Action = ApiConstants.AddScheduleItems,
                Schedule = new OpenShiftSchedule
                {
                    OrgJobPath = jobPath,
                    QueryDateSpan = $"{openShiftStartDate}-{openShiftEndDate}",
                    IsOpenShift = true,
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                StartDate = openShiftStartDate,
                                IsOpenShift = true,
                                ShiftLabel = openShiftLabel,
                                ShiftSegments = new CommonSegments().Create(startTime, endTime, 1, secondDayNumber, jobPath),
                            },
                        },
                    },
                },
            };

            return req.XmlSerialize();
        }

        /// <summary>
        /// Method to create the DraftOpenShift.
        /// </summary>
        /// <param name="obj">The OpenShift object.</param>
        /// <returns>The XML string.</returns>
        private string CreateOpenShiftDraftRequest(OpenShiftObj obj)
        {
            OpenShiftSubmitReq.Request rq = new OpenShiftSubmitReq.Request()
            {
                Action = ApiConstants.AddRequests,
                EmployeeRequestMgmt = new OpenShiftSubmitReq.EmployeeRequestMgmt
                {
                    Employee = new Models.RequestEntities.ShiftsToKronos.AddRequest.Employee
                    {
                        PersonIdentity = new Models.RequestEntities.ShiftsToKronos.AddRequest.PersonIdentity
                        {
                            PersonNumber = obj.PersonNumber,
                        },
                    },
                    QueryDateSpan = obj.QueryDateSpan,
                    RequestItems = new OpenShiftSubmitReq.RequestItems
                    {
                        GlobalOpenShiftRequestItem = new GlobalOpenShiftRequestItem
                        {
                            Employee = new Models.RequestEntities.ShiftsToKronos.AddRequest.Employee
                            {
                                PersonIdentity = new Models.RequestEntities.ShiftsToKronos.AddRequest.PersonIdentity
                                {
                                    PersonNumber = obj.PersonNumber,
                                },
                            },
                            RequestFor = ApiConstants.OpenShiftRequest,
                            ShiftDate = obj.ShiftDate,
                            ShiftSegments = obj.OpenShiftSegments,
                        },
                    },
                },
            };

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Method to create the XML string for updating the OpenShift Request in Kronos from DRAFT to SUBMITTED.
        /// </summary>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="reqId">The RequestId.</param>
        /// <param name="querySpan">The query date span.</param>
        /// <param name="comment">The comment.</param>
        /// <returns>The XML string of the update status request.</returns>
        private string CreateOpenShiftRequestStatusUpdate(
            string personNumber,
            string reqId,
            string querySpan,
            string comment)
        {
            Request rq = new Request()
            {
                Action = ApiConstants.UpdateStatus,
                RequestMgmt = new RequestMgmt()
                {
                    Employees = new OpenShiftSubmitReq.RequestManagement.Employee()
                    {
                        PersonIdentity = new OpenShiftSubmitReq.RequestManagement.PersonIdentity()
                        {
                            PersonNumber = personNumber,
                        },
                    },
                    QueryDateSpan = querySpan,
                    RequestStatusChanges = new RequestStatusChanges(),
                },
            };

            rq.RequestMgmt.RequestStatusChanges.RequestStatusChange = new RequestStatusChange[]
            {
                new RequestStatusChange
                {
                    Comments = new Comment
                    {
                        CommentText = comment,
                    },
                    RequestId = reqId,
                    ToStatusName = ApiConstants.Submitted,
                },
            };

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Fetch TimeOff request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <returns>XML request string.</returns>
        private string CreateOpenShiftsApprovedDeclinedRequest(
            string queryDateSpan,
            string personNumber)
        {
            OpenShiftApproveDecline.RequestManagementApproveDecline.Request request =
                new OpenShiftApproveDecline.RequestManagementApproveDecline.Request
                {
                    Action = ApiConstants.Retrieve,
                    RequestMgmt = new OpenShiftApproveDecline.RequestManagementApproveDecline.RequestMgmt
                    {
                        Employees = new OpenShiftApproveDecline.RequestManagementApproveDecline.Employee
                        {
                            PersonIdentity = new OpenShiftApproveDecline.RequestManagementApproveDecline.PersonIdentity
                            {
                                PersonNumber = personNumber,
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestFor = ApiConstants.OpenShiftRequest,
                    },
                };

            return request.XmlSerialize();
        }

        /// <summary>
        /// This method builds the request XML for getting the open shifts in a batch manner.
        /// </summary>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="orgJobPaths">The list of org job paths.</param>
        /// <returns>The XML Request string.</returns>
        private string CreateOpenShiftDetailsBatchRequest(
            string queryDateSpan,
            List<string> orgJobPaths)
        {
            if (orgJobPaths is null)
            {
                throw new ArgumentNullException(nameof(orgJobPaths));
            }

            Models.RequestEntities.OpenShift.BatchRequest.Request rq = new Models.RequestEntities.OpenShift.BatchRequest.Request()
            {
                Schedule = BuildScheduleList(queryDateSpan, orgJobPaths),
                Action = ApiConstants.LoadOpenShifts,
            };

            return rq.XmlSerialize();
        }

        private Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response ProcessOpenShiftsApprovedDeclinedResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response>(
                xResponse.ToString());
        }

        /// <summary>
        /// Creates an Approval/Denial request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="approved">Whether the request needs to be approved or denied.</param>
        /// <param name="id">The Kronos id of the request.</param>
        /// <returns>XML request string.</returns>
        private string CreateApproveOrDeclineRequest(
            string queryDateSpan,
            string personNumber,
            bool approved,
            string id)
        {
            var request =
                new OpenShiftApproveDecline.RequestManagementApproveDecline.Request
                {
                    Action = approved ? ApiConstants.ApproveRequests : ApiConstants.RefuseRequests,
                    RequestMgmt = new OpenShiftApproveDecline.RequestManagementApproveDecline.RequestMgmt
                    {
                        Employees = new OpenShiftApproveDecline.RequestManagementApproveDecline.Employee
                        {
                            PersonIdentity = new OpenShiftApproveDecline.RequestManagementApproveDecline.PersonIdentity
                            {
                                PersonNumber = personNumber
                            }
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new OpenShiftApproveDecline.RequestManagementApproveDecline.RequestIds
                        {
                            RequestId = new OpenShiftApproveDecline.RequestManagementApproveDecline.RequestId[1]
                            {
                                new OpenShiftApproveDecline.RequestManagementApproveDecline.RequestId() { Id = id }
                            }
                        }
                    }
                };
            return request.XmlSerialize();
        }
    }
}