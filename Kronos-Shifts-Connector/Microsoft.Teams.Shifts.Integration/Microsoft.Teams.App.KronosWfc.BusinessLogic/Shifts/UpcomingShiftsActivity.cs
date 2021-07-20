// <copyright file="UpcomingShiftsActivity.cs" company="Microsoft">
//  Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Service;
    using static Microsoft.Teams.App.KronosWfc.BusinessLogic.Common.XmlHelper;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using CreateRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts.ShiftRequest;
    using CreateResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using CreateScheduleRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts.Schedule;
    using Response = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.Response;
    using ScheduleRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule;

    /// <summary>
    /// Upcoming shifts activity class.
    /// </summary>
    [Serializable]
    public class UpcomingShiftsActivity : IUpcomingShiftsActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpcomingShiftsActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">Having the telemetry capturing mechanism.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public UpcomingShiftsActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// This method retrieves all the upcoming shifts.
        /// </summary>
        /// <param name="endPointUrl">The Kronos API endpoint.</param>
        /// <param name="jSession">The Kronos "token".</param>
        /// <param name="startDate">The query start date.</param>
        /// <param name="endDate">The query end date.</param>
        /// <param name="employees">The list of users to query.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        public async Task<Response> ShowUpcomingShiftsInBatchAsync(
            Uri endPointUrl,
            string jSession,
            string startDate,
            string endDate,
            List<ResponseHyperFindResult> employees)
        {
            if (employees is null)
            {
                throw new ArgumentNullException(nameof(employees));
            }

            var xmlScheduleRequest = this.CreateUpcomingShiftsRequestEmployees(
                startDate,
                endDate,
                employees);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                SoapEnvOpen,
                xmlScheduleRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            var scheduleResponse = tupleResponse.ProcessResponse<Response>(this.telemetryClient);
            scheduleResponse.Jsession = tupleResponse.Item2;
            return scheduleResponse;
        }

        /// <inheritdoc/>
        public async Task<CreateResponse> CreateShift(
            Uri endpoint,
            string jSession,
            string shiftDate,
            string jobPath,
            string kronosId,
            string shiftLabel,
            string startTime,
            string endTime)
        {
            var createShiftRequest = this.CreateShiftRequest(
                shiftDate,
                jobPath,
                kronosId,
                shiftLabel,
                startTime,
                endTime);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                SoapEnvOpen,
                createShiftRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            return response.ProcessResponse<CreateResponse>(this.telemetryClient);
        }

        private string CreateShiftRequest(
            string shiftDate,
            string jobPath,
            string kronosId,
            string shiftLabel,
            string startTime,
            string endTime)
        {
            CreateRequest req = new CreateRequest
            {
                Action = AddScheduleItems,
                Schedule = new CreateScheduleRequest
                {
                    Employees = new Employees().Create(kronosId),
                    OrgJobPath = jobPath,
                    QueryDateSpan = $"{shiftDate}-{shiftDate}",
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                Employee = new Employee().Create(kronosId),
                                ShiftLabel = shiftLabel,
                                StartDate = shiftDate,
                                ShiftSegments = new ShiftSegments().Create(startTime, endTime, 1, 1, jobPath),
                            },
                        },
                    },
                },
            };

            return req.XmlSerialize();
        }

        private string CreateUpcomingShiftsRequestEmployees(string startDate, string endDate, List<ResponseHyperFindResult> employees)
        {
            ScheduleRequest.Request request = new ScheduleRequest.Request()
            {
                Action = ApiConstants.LoadAction,
                Schedule = new ScheduleRequest.ScheduleReq()
                {
                    Employees = new List<ScheduleRequest.PersonIdentity>(),
                    QueryDateSpan = $"{startDate} - {endDate}",
                },
            };

            var scheduledEmployees = employees.ConvertAll(x => new ScheduleRequest.PersonIdentity { PersonNumber = x.PersonNumber });
            request.Schedule.Employees.AddRange(scheduledEmployees);

            return request.XmlSerialize();
        }
    }
}