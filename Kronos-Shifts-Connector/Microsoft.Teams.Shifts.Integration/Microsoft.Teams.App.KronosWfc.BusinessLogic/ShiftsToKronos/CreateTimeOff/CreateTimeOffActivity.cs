// <copyright file="CreateTimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.ShiftsToKronos.CreateTimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Service;
    using TimeOffAddRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;
    using TimeOffAddResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests;
    using TimeOffSubmitRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest;
    using TimeOffSubmitResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests.SubmitResponse;

    /// <summary>
    /// Create TimeOff Activity Class.
    /// </summary>
    public class CreateTimeOffActivity : ICreateTimeOffActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateTimeOffActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">The mechanisms to capture telemetry.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public CreateTimeOffActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Submit time of request which is in draft.
        /// </summary>
        /// <param name="jSession">jSession object.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reqId">RequestId of the time off request.</param>
        /// <param name="queryStartDate">Query Start.</param>
        /// <param name="queryEndDate">Query End.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <returns>Time of submit response.</returns>
        public async Task<TimeOffSubmitResponse.Response> SubmitTimeOffRequestAsync(
            string jSession,
            string personNumber,
            string reqId,
            string queryStartDate,
            string queryEndDate,
            Uri endPointUrl)
        {
            string querySpan = queryStartDate + '-' + queryEndDate;
            string xmlTimeOffRequest = this.CreateSubmitTimeOffRequest(personNumber, reqId, querySpan);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            TimeOffSubmitResponse.Response timeOffSubmitResponse = this.ProcessSubmitResponse(tupleResponse.Item1);

            return timeOffSubmitResponse;
        }

        /// <summary>
        /// Send time off request to Kronos API and get response.
        /// </summary>
        /// <param name="jSession">J Session.</param>
        /// <param name="startDateTime">Start Date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="endPointUrl">Endpoint url for Kronos.</param>
        /// <param name="kronosTimeZone">The time zone for Kronos WFC.</param>
        /// <returns>Time of add response.</returns>
        public async Task<TimeOffAddResponse.Response> TimeOffRequestAsync(
            string jSession,
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string personNumber,
            string reason,
            Uri endPointUrl,
            TimeZoneInfo kronosTimeZone)
        {
            string xmlTimeOffRequest = this.CreateAddTimeOffRequest(startDateTime, endDateTime, personNumber, reason, kronosTimeZone);
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlTimeOffRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            TimeOffAddResponse.Response timeOffResponse = this.ProcessResponse(tupleResponse.Item1);

            return timeOffResponse;
        }

        /// <summary>
        /// This method will calculate the necessary duration of the time off.
        /// </summary>
        /// <param name="startDateTime">The start date/time stamp for the time off request.</param>
        /// <param name="endDateTime">The end date/time stamp for the time off request.</param>
        /// <param name="reason">The time off reason from Shifts.</param>
        /// <param name="timeOffPeriod">The list of Time Off periods.</param>
        /// <param name="kronosTimeZone">The time zone of Kronos WFC.</param>
        /// <returns>A string that represents the duration period.</returns>
        private static string CalculateTimeOffPeriod(
            DateTimeOffset startDateTime,
            DateTimeOffset endDateTime,
            string reason,
            List<TimeOffAddRequest.TimeOffPeriod> timeOffPeriod,
            TimeZoneInfo kronosTimeZone)
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
                        StartTime = TimeZoneInfo.ConvertTime(startDateTime, kronosTimeZone).ToString("hh:mm tt", CultureInfo.InvariantCulture),
                        Length = Convert.ToString(length, CultureInfo.InvariantCulture),
                    });
            }

            return duration;
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
            var monthStartDt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var monthEndDt = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.DaysInMonth(monthStartDt.Year, monthStartDt.Month));
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
        /// Deserialize xml response for submit request operation.
        /// </summary>
        /// <param name="strResponse">Response string.</param>
        /// <returns>Process submit response.</returns>
        private TimeOffSubmitResponse.Response ProcessSubmitResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<TimeOffSubmitResponse.Response>(xResponse.ToString());
        }

        /// <summary>
        /// Create XML to add time off request.
        /// </summary>
        /// <param name="startDateTime">Start date.</param>
        /// <param name="endDateTime">End Date.</param>
        /// <param name="personNumber">Person number.</param>
        /// <param name="reason">Reason string.</param>
        /// <param name="kronosTimeZone">The time zone of Kronos WFC.</param>
        /// <returns>Add time of request.</returns>
        private string CreateAddTimeOffRequest(DateTimeOffset startDateTime, DateTimeOffset endDateTime, string personNumber, string reason, TimeZoneInfo kronosTimeZone)
        {
            var duration = string.Empty;
            var timeOffPeriod = new List<TimeOffAddRequest.TimeOffPeriod>();
            duration = CalculateTimeOffPeriod(startDateTime, endDateTime, reason, timeOffPeriod, kronosTimeZone);

            TimeOffAddRequest.Request rq = new TimeOffAddRequest.Request()
            {
                Action = ApiConstants.AddRequests,
                EmployeeRequestMgm = new TimeOffAddRequest.EmployeeRequestMgmt()
                {
                    Employees = new TimeOffAddRequest.Employee() { PersonIdentity = new TimeOffAddRequest.PersonIdentity() { PersonNumber = personNumber } },
                    QueryDateSpan = $"{startDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture)} - {endDateTime.ToString("M/d/yyyy", CultureInfo.InvariantCulture)}",
                    RequestItems = new TimeOffAddRequest.RequestItems()
                    {
                        GlobalTimeOffRequestItem = new TimeOffAddRequest.GlobalTimeOffRequestItem()
                        {
                            Employee = new TimeOffAddRequest.Employee() { PersonIdentity = new TimeOffAddRequest.PersonIdentity() { PersonNumber = personNumber } },
                            RequestFor = ApiConstants.TOR,
                            TimeOffPeriods = new TimeOffAddRequest.TimeOffPeriods() { TimeOffPeriod = timeOffPeriod },
                        },
                    },
                },
            };

            return rq.XmlSerialize<TimeOffAddRequest.Request>();
        }

        /// <summary>
        /// Read the xml response into Response object.
        /// </summary>
        /// <param name="strResponse">xml response string.</param>
        /// <returns>Response object.</returns>
        private TimeOffAddResponse.Response ProcessResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<TimeOffAddResponse.Response>(xResponse.ToString());
        }
    }
}