// <copyright file="UpcomingShiftsActivity.cs" company="Microsoft">
//  Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Shifts
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.BusinessLogic.Common;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Service;
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
                ApiConstants.SoapEnvOpen,
                xmlScheduleRequest,
                ApiConstants.SoapEnvClose,
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
            string startTime,
            string endTime,
            Comments shiftComments)
        {
            var createShiftRequest = this.CreateShiftRequest(shiftStartDate, shiftEndDate, overADateBorder, jobPath, kronosId, startTime, endTime, shiftComments);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                ApiConstants.SoapEnvOpen,
                createShiftRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return response.ProcessResponse<CRUDResponse>(this.telemetryClient);
        }

        /// <inheritdoc/>
        public async Task<CRUDResponse> EditShift(
            Uri endpoint,
            string jSession,
            string replacementShiftStartDate,
            string replacementShiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string replacementShiftStartTime,
            string replacementShiftEndTime,
            string shiftToReplaceStartDate,
            string shiftToReplaceEndDate,
            string shiftToReplaceStartTime,
            string shiftToReplaceEndTime,
            Comments shiftComments)
        {
            var createShiftRequest = this.CreateEditRequest(
                replacementShiftStartDate,
                replacementShiftEndDate,
                overADateBorder,
                jobPath,
                kronosId,
                replacementShiftStartTime,
                replacementShiftEndTime,
                shiftToReplaceStartDate,
                shiftToReplaceEndDate,
                shiftToReplaceStartTime,
                shiftToReplaceEndTime,
                shiftComments);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                ApiConstants.SoapEnvOpen,
                createShiftRequest,
                ApiConstants.SoapEnvClose,
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
            var deleteShiftRequest = this.DeleteShiftRequest(shiftStartDate, shiftEndDate, overADateBorder, jobPath, kronosId, startTime, endTime);

            var response = await this.apiHelper.SendSoapPostRequestAsync(
                endpoint,
                ApiConstants.SoapEnvOpen,
                deleteShiftRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return response.ProcessResponse<CRUDResponse>(this.telemetryClient);
        }

        private string CreateShiftRequest(
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime,
            Comments shiftComments)
        {
            var secondDayNumber = overADateBorder ? 2 : 1;
            Request req = new Request
            {
                Action = ApiConstants.AddScheduleItems,
                Schedule = new Schedule
                {
                    Employees = new Employees().Create(kronosId),
                    QueryDateSpan = $"{shiftStartDate}-{shiftEndDate}",
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                StartDate = shiftStartDate,
                                ShiftSegments = new ShiftSegments().Create(startTime, endTime, 1, secondDayNumber, jobPath),
                                Comments = shiftComments,
                            },
                        },
                    },
                },
            };

            return req.XmlSerialize();
        }

        private string CreateEditRequest(
            string shiftStartDate,
            string shiftEndDate,
            bool overADateBorder,
            string jobPath,
            string kronosId,
            string startTime,
            string endTime,
            string shiftToReplaceStartDate,
            string shiftToReplaceEndDate,
            string shiftToReplaceStartTime,
            string shiftToReplaceEndTime,
            Comments comments)
        {
            // Ensure the query date range spans both the old shift and the shift to replace with
            var queryDateSpanStart = DateTime.Parse(shiftStartDate, CultureInfo.InvariantCulture) <= DateTime.Parse(shiftToReplaceStartDate, CultureInfo.InvariantCulture) ? shiftStartDate : shiftToReplaceStartDate;
            var queryDateSpanEnd = DateTime.Parse(shiftEndDate, CultureInfo.InvariantCulture) >= DateTime.Parse(shiftToReplaceEndDate, CultureInfo.InvariantCulture) ? shiftEndDate : shiftToReplaceEndDate;

            var secondDayNumber = overADateBorder ? 2 : 1;
            Request req = new Request
            {
                Action = ApiConstants.ReplaceShift,
                Schedule = new Schedule
                {
                    Employees = new Employees().Create(kronosId),
                    QueryDateSpan = $"{queryDateSpanStart}-{queryDateSpanEnd}",
                    ScheduleItems = new ScheduleItems
                    {
                        ScheduleShift = new List<ScheduleShift>
                        {
                            new ScheduleShift
                            {
                                StartDate = shiftStartDate,
                                ReplaceStartDate = shiftToReplaceStartDate,
                                ReplaceEndDate = shiftToReplaceEndDate,
                                ReplaceStartTime = shiftToReplaceStartTime,
                                ReplaceEndTime = shiftToReplaceEndTime,
                                ShiftSegments = new ShiftSegments().Create(startTime, endTime, 1, secondDayNumber, jobPath),
                                Comments = comments,
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
                Action = ApiConstants.RemoveSpecifiedScheduleItems,
                Schedule = new Schedule
                {
                    Employees = new Employees().Create(kronosId),
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
                Action = ApiConstants.LoadAction,
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