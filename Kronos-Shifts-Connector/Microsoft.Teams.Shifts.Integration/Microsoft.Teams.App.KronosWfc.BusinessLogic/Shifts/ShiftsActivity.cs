// <copyright file="UpcomingShiftsActivity.cs" company="Microsoft">
//  Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Service;
    using static System.Globalization.CultureInfo;
    using static Microsoft.Teams.App.KronosWfc.BusinessLogic.Common.XmlHelper;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using CRUDResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Common.Response;
    using Request = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts.ShiftRequest;
    using Response = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Shifts.UpcomingShifts.Response;

    /// <summary>
    /// Upcoming shifts activity class.
    /// </summary>
    [Serializable]
    public class ShiftsActivity : IShiftsActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="ShiftsActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">Having the telemetry capturing mechanism.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public ShiftsActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <inheritdoc/>
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
        public async Task<CRUDResponse> CreateShift(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string shiftLabel,
            string startTime,
            string endTime)
        {
            var createShiftRequest = this.CreateShiftRequest(
                shiftStartDate,
                shiftEndDate,
                overADateBorder,
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

            return response.ProcessResponse<CRUDResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CRUDResponse> DeleteShift(
            Uri endpoint,
            string jSession,
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime)
        {
            var deleteShiftRequest = this.DeleteShiftRequest(
                shiftStartDate,
                shiftEndDate,
                overADateBorder,
                jobPath,
                kronosId,
                startTime,
                endTime);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                SoapEnvOpen,
                deleteShiftRequest,
                SoapEnvClose,
                jSession).ConfigureAwait(false);

            return response.ProcessResponse<CRUDResponse>(this.telemetryClient);
        }

        private string CreateShiftRequest(
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string shiftLabel,
            string startTime,
            string endTime)
        {
            var secondDayNumber = overADateBorder ? 2 : 1;
            Request req = new Request
            {
                Action = AddScheduleItems,
                Schedule = new Schedule
                {
                    Employees = new Employees().Create(kronosId),
                    OrgJobPath = jobPath,
                    QueryDateSpan = $"{shiftStartDate}-{shiftEndDate}",
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                Employee = new Employee().Create(kronosId),
                                ShiftLabel = shiftLabel,
                                StartDate = shiftStartDate,
                                ShiftSegments = new ShiftSegments().Create(startTime, endTime, 1, secondDayNumber, jobPath),
                            },
                        },
                    },
                },
            };

            return req.XmlSerialize();
        }

        private string DeleteShiftRequest(
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime)
        {
            var secondDayNumber = overADateBorder ? 2 : 1;
            Request req = new Request
            {
                Action = RemoveSpecifiedScheduleItems,
                Schedule = new Schedule
                {
                    Employees = new Employees().Create(kronosId),
                    OrgJobPath = jobPath,
                    QueryDateSpan = $"{shiftStartDate}-{shiftEndDate}",
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                StartDate = shiftStartDate,
                                ShiftSegments = new ShiftSegments().Create(startTime, endTime, 1, secondDayNumber, jobPath),
                            },
                        },
                    },
                },
            };

            return req.XmlSerialize();
        }

        private string CreateUpcomingShiftsRequestEmployees(string startDate, string endDate, List<ResponseHyperFindResult> employees)
        {
            Request request = new Request()
            {
                Action = LoadAction,
                Schedule = new Schedule()
                {
                    Employees = new Employees().Create(employees.Select(x => x.PersonNumber).ToArray()),
                    QueryDateSpan = $"{startDate} - {endDate}",
                },
            };

            return request.XmlSerialize();
        }
    }
}