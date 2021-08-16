// <copyright file="TimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Service;
    using ApproveDeclineResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using CancelResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using TimeOffAddRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;
    using TimeOffAddResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.Response;
    using TimeOffApproveDenyRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.TimeOffApproveDecline;
    using TimeOffCancelRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests.CancelTimeOff;
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
        public async Task<TimeOffAddResponse> CreateTimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTimeUtc,
            DateTimeOffset endDateTimeUtc,
            string queryDateSpan,
            string personNumber,
            string reason,
            string senderMessage,
            string senderCommentText,
            Uri endPointUrl)
        {
            string xmlTimeOffRequest = this.CreateAddTimeOffRequest(startDateTimeUtc, endDateTimeUtc, queryDateSpan, personNumber, reason, senderMessage, senderCommentText);
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
        public async Task<CancelResponse> CancelTimeOffRequestAsync(
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

            return tupleResponse.ProcessResponse<CancelResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<ApproveDeclineResponse> ApproveOrDenyTimeOffRequestAsync(
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

            return tupleResponse.ProcessResponse<ApproveDeclineResponse>(this.telemetryClient);
        }

        /// <summary>
        /// This method will calculate the necessary duration of the time off.
        /// </summary>
        /// <param name="startDateTime">The start date/time stamp for the time off request.</param>
        /// <param name="endDateTime">The end date/time stamp for the time off request.</param>
        /// <param name="reason">The time off reason from Shifts.</param>
        /// <param name="timeOffPeriod">The list of Time Off periods.</param>
        /// <returns>A string that represents the duration period.</returns>
        private static string CalculateTimeOffPeriod(
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string reason,
            List<TimeOffAddRequest.TimeOffPeriod> timeOffPeriod)
        {
            string duration;
            var length = (endDateTime - startDateTime).TotalHours;
            DateTimeOffset modifiedEndDateTimeForKronos = endDateTime.AddDays(-1);
            if (length % 24 == 0 || length > 24)
            {
                duration = ApiConstants.FullDayDuration;
                timeOffPeriod.Add(
                    new TimeOffAddRequest.TimeOffPeriod()
                    {
                        Duration = duration,
                        EndDate = modifiedEndDateTimeForKronos.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                        PayCodeName = reason,
                        StartDate = startDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                    });
            }
            else
            {
                duration = ApiConstants.HoursDuration;
                timeOffPeriod.Add(
                    new TimeOffAddRequest.TimeOffPeriod()
                    {
                        Duration = duration,
                        EndDate = endDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                        PayCodeName = reason,
                        StartDate = startDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture),
                        StartTime = startDateTime.ToString("hh:mm tt", CultureInfo.InvariantCulture),
                        Length = Convert.ToString(length, CultureInfo.InvariantCulture),
                    });
            }

            return duration;
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
                    Employees = new TimeOffRequest.Employees
                    {
                        PersonIdentity = new List<TimeOffRequest.PersonIdentity>(),
                    },
                },
            };

            var timeOffEmployees = employees.ConvertAll(x => new TimeOffRequest.PersonIdentity { PersonNumber = x.PersonNumber });
            rq.RequestMgmt.Employees.PersonIdentity.AddRange(timeOffEmployees);

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Create XML to add time off request.
        /// </summary>
        /// <param name="startDateTime">Start date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="senderMessage">The sender notes of the time off request.</param>
        /// <param name="senderCommentText">The Kronos comment text value to assign to the notes.</param>
        /// <returns>Add time of request.</returns>
        private string CreateAddTimeOffRequest(DateTimeOffset startDateTime, DateTimeOffset endDateTime, string queryDateSpan, string personNumber, string reason, string senderMessage, string senderCommentText)
        {
            var duration = string.Empty;
            var timeOffPeriod = new List<TimeOffAddRequest.TimeOffPeriod>();
            duration = CalculateTimeOffPeriod(startDateTime, endDateTime, reason, timeOffPeriod);

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
                            TimeOffPeriods = new TimeOffAddRequest.TimeOffPeriods() { TimeOffPeriod = timeOffPeriod },
                            Comments = new TimeOffAddRequest.Comments
                            {
                                Comment = new List<TimeOffAddRequest.Comment>
                                {
                                    new TimeOffAddRequest.Comment
                                    {
                                        CommentText = senderCommentText,
                                        Notes = new TimeOffAddRequest.Notes
                                        {
                                            Note = new TimeOffAddRequest.Note
                                            {
                                                Text = senderMessage,
                                            },
                                        },
                                    },
                                },
                            },
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
                new TimeOffCancelRequest.Request
                {
                    Action = ApiConstants.RetractRequests,
                    RequestMgmt = new TimeOffCancelRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new TimeOffCancelRequest.RequestIds
                        {
                            RequestId = new TimeOffCancelRequest.RequestId[1]
                            {
                                new TimeOffCancelRequest.RequestId() { Id = id },
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
                new TimeOffApproveDenyRequest.Request
                {
                    Action = approved ? ApiConstants.ApproveRequests : ApiConstants.RefuseRequests,
                    RequestMgmt = new TimeOffApproveDenyRequest.RequestMgmt
                    {
                        Employees = new Employees
                        {
                            PersonIdentity = new List<PersonIdentity>
                            {
                                new PersonIdentity { PersonNumber = personNumber },
                            },
                        },
                        QueryDateSpan = queryDateSpan,
                        RequestIds = new TimeOffApproveDenyRequest.RequestIds
                        {
                            RequestId = new TimeOffApproveDenyRequest.RequestId[1]
                            {
                                new TimeOffApproveDenyRequest.RequestId() { Id = id },
                            },
                        },
                    },
                };

            return request.XmlSerialize();
        }
    }
}