// <copyright file="SwapShiftEligibilityActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.SwapShiftEligibility
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShiftEligibility;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShiftEligibility;
    using Microsoft.Teams.App.KronosWfc.Service;
    using static Microsoft.Teams.App.KronosWfc.BusinessLogic.Common.XmlHelper;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;
    using Request = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShiftEligibility.Request;
    using Response = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShiftEligibility.Response;

    /// <summary>
    /// Class that implements all of the methods in the <see cref="ISwapShiftEligibilityActivity"/> interface.
    /// </summary>
    public class SwapShiftEligibilityActivity : ISwapShiftEligibilityActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="SwapShiftEligibilityActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public SwapShiftEligibilityActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Sends the swap eligibility request.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="startTime">The start time for the requestor's shift.</param>
        /// <param name="endTime">The end time for the requestor's shift.</param>
        /// <param name="queryDate">The date for the requestor's shift.</param>
        /// <param name="shiftSwapDate">The date time for the potential requested shift.</param>
        /// <param name="employeeNumber">The employee number of the requestor.</param>
        /// <returns>Response object.</returns>
        public async Task<Response> SendEligibilityRequestAsync(
            Uri endPointUrl,
            string jSession,
            string startTime,
            string endTime,
            string queryDate,
            string shiftSwapDate,
            string employeeNumber)
        {
            var request = this.CreateEligibilityRequest(startTime, endTime, queryDate, shiftSwapDate, employeeNumber);
            var response = await this.apiHelper.SendSoapPostRequestAsync(endPointUrl, SoapEnvOpen, request, SoapEnvClose, jSession).ConfigureAwait(false);
            return response.ProcessResponse<Response>(this.telemetryClient);
        }

        /// <summary>
        /// Creates the swap eligibility request.
        /// </summary>
        /// <param name="startTime">The start time for the requestor's shift.</param>
        /// <param name="endTime">The end time for the requestor's shift.</param>
        /// <param name="queryDate">The date for the requestor's shift.</param>
        /// <param name="shiftSwapDate">The date time for the potential requested shift.</param>
        /// <param name="employeeNumber">The employee number of the requestor.</param>
        /// <returns>The XML request as a string.</returns>
        private string CreateEligibilityRequest(
            string startTime,
            string endTime,
            string queryDate,
            string shiftSwapDate,
            string employeeNumber)
        {
            var request =
                new Request
                {
                    Action = LoadEligibleEmployees,
                    SwapShiftEmployees = new SwapShiftEmployees()
                    {
                        StartTime = startTime,
                        EndTime = endTime,
                        QueryDate = queryDate,
                        ShiftSwapDate = shiftSwapDate,
                        Employee = new Employee()
                        {
                            PersonIdentity = new PersonIdentity() { PersonNumber = employeeNumber },
                        },
                    },
                };
            return request.XmlSerialize();
        }
    }
}
