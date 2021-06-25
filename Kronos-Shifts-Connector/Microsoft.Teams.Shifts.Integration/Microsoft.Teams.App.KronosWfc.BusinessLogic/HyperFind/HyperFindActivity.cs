// <copyright file="HyperFindActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind
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
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Service;
    using HyperFindResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind.Response;

    /// <summary>
    /// Hyper Find Activity class.
    /// </summary>
    [Serializable]
    public class HyperFindActivity : IHyperFindActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperFindActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">Having the telemetry capturing mechanism.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public HyperFindActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Returns all the home employees.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">The jSession string.</param>
        /// <param name="startDate">The startDate.</param>
        /// <param name="endDate">The endDate.</param>
        /// <param name="hyperFindQueryName">The name of the hyper find query.</param>
        /// <param name="visibilityCode">The visibility code.</param>
        /// <returns>A unit of execution that contains the type <see cref="Response"/>.</returns>
        public async Task<Response> GetHyperFindQueryValuesAsync(
            Uri endPointUrl,
            string jSession,
            string startDate,
            string endDate,
            string hyperFindQueryName,
            string visibilityCode)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace("Get all home employees from Kronos.", telemetryProps);

            string hyperFindRequest = this.CreateHyperFindRequest(startDate, endDate, hyperFindQueryName, visibilityCode);

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                hyperFindRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            return tupleResponse.ProcessResponse<HyperFindResponse>(this.telemetryClient);
        }

        /// <summary>
        /// Creates hyper find request.
        /// </summary>
        /// <param name="startDate">Start Date.</param>
        /// <param name="endDate">End Date.</param>
        /// <param name="hyperFindQueryName">The name of the hyper find query.</param>
        /// <param name="visibilityCode">The visibility code.</param>
        /// <returns>Request string.</returns>
        private string CreateHyperFindRequest(string startDate, string endDate, string hyperFindQueryName, string visibilityCode)
        {
            Request rq = new Request()
            {
                HyperFindQuery = new RequestHyperFindQuery()
                {
                    HyperFindQueryName = hyperFindQueryName,
                    VisibilityCode = visibilityCode,
                    QueryDateSpan = $"{startDate} -{endDate}",
                },
                Action = ApiConstants.RunQueryAction,
            };
            return rq.XmlSerialize();
        }
    }
}