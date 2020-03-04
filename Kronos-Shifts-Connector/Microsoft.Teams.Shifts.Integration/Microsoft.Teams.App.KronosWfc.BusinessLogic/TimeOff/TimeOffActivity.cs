// <copyright file="TimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Service;
    using TimeOffRequest = Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests;
    using TimeOffResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;

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

        /// <summary>
        /// Fecth time off request details for displaying history.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="employees">Employees who created request.</param>
        /// <returns>Request details response object.</returns>
        public async Task<TimeOffResponse.Response> GetTimeOffRequestDetailsByBatchAsync(
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

            TimeOffResponse.Response timeOffResponse = this.ProcessTimeOffResponse(tupleResponse.Item1);

            return timeOffResponse;
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
        /// Process response for request details.
        /// </summary>
        /// <param name="strResponse">Response received from request for TOR detail.</param>
        /// <returns>Response object.</returns>
        private TimeOffResponse.Response ProcessTimeOffResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<TimeOffResponse.Response>(xResponse.ToString());
        }
    }
}