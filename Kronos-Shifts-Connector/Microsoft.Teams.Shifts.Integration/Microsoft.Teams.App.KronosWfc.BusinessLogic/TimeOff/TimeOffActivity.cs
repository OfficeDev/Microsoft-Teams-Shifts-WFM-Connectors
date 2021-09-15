// <copyright file="TimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Service;
    using CommonResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using CommonTimeOffRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.CommonTimeOffRequests;
    using TimeOffAddRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;
    using TimeOffAddResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.Response;
    using TimeOffRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests;
    using TimeOffResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests.Response;
    using TimeOffSubmitRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest;
    using TimeOffSubmitResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.SubmitResponse.Response;

    /// <summary>
    /// TimeOff Activity Class.
    /// </summary>
    public class TimeOffActivity : ITimeOffActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">The mechanisms to capture telemetry.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public TimeOffActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <inheritdoc/>
        public async Task<TimeOffResponse> GetTimeOffRequestDetailsByBatchAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            List<Models.ResponseEntities.HyperFind.ResponseHyperFindResult> employees)
        {
            if (employees is null)
            {
                throw new ArgumentNullException(nameof(employees));
            }

            var xmlTimeOffRequest = this.FetchTimeOffRequestsByBatch(employees, queryDateSpan);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<TimeOffResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<TimeOffResponse> GetTimeOffRequestDetailsAsync(Uri endPointUrl, string jSession, string queryDateSpan, string personNumber, string kronosRequestId)
        {
            var xmlTimeOffRequest = this.CreateRetrieveTimeOffRequest(queryDateSpan, personNumber, kronosRequestId);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<TimeOffResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<TimeOffAddResponse> CreateTimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            Comments comments,
            Uri endPointUrl)
        {
            string xmlTimeOffRequest = this.CreateAddTimeOffRequest(startDateTime, endDateTime, queryDateSpan, personNumber, reason, comments);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<TimeOffAddResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<TimeOffSubmitResponse> SubmitTimeOffRequestAsync(
            string jSession,
            string personNumber,
            string reqId,
            string queryDateSpan,
            Uri endPointUrl)
        {
            string xmlTimeOffRequest = this.CreateSubmitTimeOffRequest(personNumber, reqId, queryDateSpan);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<TimeOffSubmitResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CommonResponse> CancelTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            string kronosId)
        {
            var xmlTimeOffCancelRequest = this.CreateCancelTimeOffRequest(queryDateSpan, kronosPersonNumber, kronosId);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffCancelRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace(
                "TimeOffActivity - CancelTimeOffRequestAsync",
                new Dictionary<string, string>()
                {
                    { "Response", tupleResponse.Item1 },
                });

            return tupleResponse.ProcessResponse<CommonResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CommonResponse> ApproveOrDenyTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId)
        {
            var xmlTimeOffApprovalRequest = this.CreateApproveOrDeclineTimeOffRequest(queryDateSpan, kronosPersonNumber, approved, kronosId);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffApprovalRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace(
                "TimeOffActivity - ApproveOrDenyTimeOffRequestAsync",
                new Dictionary<string, string>()
                {
                    { "Response", tupleResponse.Item1 },
                });

            return tupleResponse.ProcessResponse<CommonResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CommonResponse> AddManagerCommentsToTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string kronosRequestId,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            Comments comments)
        {
            var xmlTimeOffRequestUpdateRequest = this.CreateUpdateTimeOffRequest(kronosRequestId, startDateTime, endDateTime, queryDateSpan, personNumber, reason, comments);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequestUpdateRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            this.telemetryClient.TrackTrace(
                "TimeOffActivity - ApproveOrDenyTimeOffRequestAsync",
                new Dictionary<string, string>()
                {
                    { "Response", tupleResponse.Item1 },
                });

            return tupleResponse.ProcessResponse<CommonResponse>(this.telemetryClient);
        }

        /// <summary>
        /// This method will calculate the necessary duration of the time off.
        /// </summary>
        /// <param name="startDateTime">The start date/time stamp for the time off request.</param>
        /// <param name="endDateTime">The end date/time stamp for the time off request.</param>
        /// <param name="reason">The time off reason from Shifts.</param>
        /// <returns>Returns the time off period for the request.</returns>
        private static TimeOffPeriod CalculateTimeOffPeriod(DateTimeOffset startDateTime, DateTimeOffset endDateTime, string reason)
        {
            string duration;
            var length = (endDateTime - startDateTime).TotalHours;
            DateTimeOffset modifiedEndDateTimeForKronos = endDateTime.AddDays(-1);
            if (length % 24 == 0 || length > 24)
            {
                duration = ApiConstants.FullDayDuration;
                return new TimeOffPeriod()
                {
                    Duration = duration,
                    EndDate = modifiedEndDateTimeForKronos.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                    PayCodeName = reason,
                    StartDate = startDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                };
            }
            else
            {
                duration = ApiConstants.HoursDuration;
                return new TimeOffPeriod()
                {
                    Duration = duration,
                    EndDate = endDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                    PayCodeName = reason,
                    StartDate = startDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                    StartTime = startDateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                    Length = Convert.ToString(length, CultureInfo.InvariantCulture),
                };
            }
        }

        /// <summary>
        /// Fetch TimeOff request.
        /// </summary>
        /// <param name="employees">Employees who created request.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <returns>XML request string.</returns>
        private string FetchTimeOffRequestsByBatch(List<Models.ResponseEntities.HyperFind.ResponseHyperFindResult> employees, string queryDateSpan)
        {
            TimeOffRequest.Request rq = new TimeOffRequest.Request
            {
                Action = ApiConstants.RetrieveWithDetails,
                RequestMgmt = new TimeOffRequest.RequestMgmt
                {
                    QueryDateSpan = $"{queryDateSpan}",
                    RequestFor = ApiConstants.TOR,
                    Employees = new Employees
                    {
                        PersonIdentity = new List<PersonIdentity>(),
                    },
                },
            };

            var timeOffEmployees = employees.ConvertAll(x => new PersonIdentity { PersonNumber = x.PersonNumber });
            rq.RequestMgmt.Employees.PersonIdentity.AddRange(timeOffEmployees);

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Creates a request to retrieve a time off request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="id">The Kronos id of the request.</param>
        /// <returns>XML request string.</returns>
        private string CreateRetrieveTimeOffRequest(string queryDateSpan, string personNumber, string id)
        {
            var request =
                new CommonTimeOffRequest.Request
                {
                    Action = ApiConstants.RetrieveWithDetails,
                    RequestMgmt = new CommonTimeOffRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new CommonTimeOffRequest.RequestIds
                        {
                            RequestId = new CommonTimeOffRequest.RequestId[1]
                            {
                                new CommonTimeOffRequest.RequestId() { Id = id },
                            },
                        },
                    },
                };

            return request.XmlSerialize();
        }

        /// <summary>
        /// Create XML to add time off request.
        /// </summary>
        /// <param name="startDateTime">Start date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="comments">The sender notes of the time off request.</param>
        /// <returns>Add time of request.</returns>
        private string CreateAddTimeOffRequest(DateTimeOffset startDateTime, DateTimeOffset endDateTime, string queryDateSpan, string personNumber, string reason, Comments comments)
        {
            // Kronos API expects a collection of periods so first calculate the actual period
            // before adding it to a list.
            var timeOffPeriod = CalculateTimeOffPeriod(startDateTime, endDateTime, reason);
            var timeOffPeriods = new List<TimeOffPeriod>() { timeOffPeriod };

            TimeOffAddRequest.Request rq = new TimeOffAddRequest.Request()
            {
                Action = ApiConstants.AddRequests,
                EmployeeRequestMgm = new TimeOffAddRequest.EmployeeRequestMgmt()
                {
                    Employees = new TimeOffAddRequest.Employee() { PersonIdentity = new TimeOffAddRequest.PersonIdentity() { PersonNumber = personNumber } },
                    QueryDateSpan = queryDateSpan,
                    RequestItems = new TimeOffAddRequest.RequestItems()
                    {
                        GlobalTimeOffRequestItem = new TimeOffAddRequest.GlobalTimeOffRequestItem()
                        {
                            Employee = new TimeOffAddRequest.Employee() { PersonIdentity = new TimeOffAddRequest.PersonIdentity() { PersonNumber = personNumber } },
                            RequestFor = ApiConstants.TOR,
                            TimeOffPeriods = new TimeOffPeriods() { TimeOffPeriod = timeOffPeriods },
                            Comments = comments,
                        },
                    },
                },
            };

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Create XML to submit time off request which is in draft.
        /// </summary>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reqId">RequestId of the time off request.</param>
        /// <param name="querySpan">Query Span.</param>
        /// <returns>Submit time off request.</returns>
        private string CreateSubmitTimeOffRequest(string personNumber, string reqId, string querySpan)
        {
            TimeOffSubmitRequest.Request rq = new TimeOffSubmitRequest.Request()
            {
                Action = ApiConstants.SubmitRequests,
                EmployeeRequestMgm = new TimeOffSubmitRequest.EmployeeRequestMgmt()
                {
                    Employees = new TimeOffSubmitRequest.Employee() { PersonIdentity = new TimeOffSubmitRequest.PersonIdentity() { PersonNumber = personNumber } },
                    QueryDateSpan = querySpan,
                    RequestIds = new TimeOffSubmitRequest.RequestIds() { RequestId = new TimeOffSubmitRequest.RequestId[] { new TimeOffSubmitRequest.RequestId() { Id = reqId } } },
                },
            };

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Creates a cancel time off request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="id">The Kronos id of the request.</param>
        /// <returns>XML request string.</returns>
        private string CreateCancelTimeOffRequest(
            string queryDateSpan,
            string personNumber,
            string id)
        {
            var request =
                new CommonTimeOffRequest.Request
                {
                    Action = ApiConstants.RetractRequests,
                    RequestMgmt = new CommonTimeOffRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new CommonTimeOffRequest.RequestIds
                        {
                            RequestId = new CommonTimeOffRequest.RequestId[1]
                            {
                                new CommonTimeOffRequest.RequestId() { Id = id },
                            },
                        },
                    },
                };

            return request.XmlSerialize();
        }

        /// <summary>
        /// Creates an Approval/Denial time off request.
        /// </summary>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="approved">Whether the request needs to be approved or denied.</param>
        /// <param name="id">The Kronos id of the request.</param>
        /// <returns>XML request string.</returns>
        private string CreateApproveOrDeclineTimeOffRequest(
            string queryDateSpan,
            string personNumber,
            bool approved,
            string id)
        {
            var request =
                new CommonTimeOffRequest.Request
                {
                    Action = approved ? ApiConstants.ApproveRequests : ApiConstants.RefuseRequests,
                    RequestMgmt = new CommonTimeOffRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new CommonTimeOffRequest.RequestIds
                        {
                            RequestId = new CommonTimeOffRequest.RequestId[1]
                            {
                                new CommonTimeOffRequest.RequestId() { Id = id },
                            },
                        },
                    },
                };

            return request.XmlSerialize();
        }

        /// <summary>
        /// Creates an update time off request.
        /// </summary>
        /// <param name="id">The Kronos id of the request.</param>
        /// <param name="startDateTime">Start date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The queryDateSpan string.</param>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="comments">Any comments to be attached to the TOR.</param>
        /// <returns>XML request string.</returns>
        private string CreateUpdateTimeOffRequest(
            string id,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string queryDateSpan,
            string personNumber,
            string reason,
            Comments comments)
        {
            // Kronos API expects a collection of periods so first calculate the actual period
            // before adding it to a list.
            var timeOffPeriod = CalculateTimeOffPeriod(startDateTime, endDateTime, reason);
            var timeOffPeriods = new List<TimeOffPeriod>() { timeOffPeriod };

            var request =
                new TimeOffRequest.Request
                {
                    Action = ApiConstants.Update,
                    RequestMgmt = new TimeOffRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestItems = new TimeOffRequest.RequestItems
                        {
                            GlobalTimeOffRequestItem = new TimeOffRequest.GlobalTimeOffRequestItem
                            {
                                Id = id,
                                RequestFor = ApiConstants.TOR,
                                TimeOffPeriods = new TimeOffPeriods() { TimeOffPeriod = timeOffPeriods },
                                Comments = comments,
                            },
                        },
                    },
                };

            return request.XmlSerialize();
        }
    }
}