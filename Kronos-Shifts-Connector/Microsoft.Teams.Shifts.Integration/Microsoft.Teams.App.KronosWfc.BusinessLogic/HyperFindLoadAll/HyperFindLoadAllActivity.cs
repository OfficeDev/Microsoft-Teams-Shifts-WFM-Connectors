// <copyright file="HyperFindLoadAllActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFindLoadAll
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.HyperFind;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFindLoadAll;
    using Microsoft.Teams.App.KronosWfc.Service;

    /// <summary>
    /// Hyper Find Load All Activity class.
    /// </summary>
    public class HyperFindLoadAllActivity : IHyperFindLoadAllActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="HyperFindLoadAllActivity"/> class.
        /// </summary>
        /// <param name="telemetryClient">The mechanisms to capture telemetry.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public HyperFindLoadAllActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Returns all the home employees.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">The jSession string.</param>
        /// <returns>A unit of execution that contains the type <see cref="Response"/>.</returns>
        public async Task<Response> GetHyperFindQueryValuesAsync(Uri endPointUrl, string jSession)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            var hyperFindRequest = this.CreateHyperFindRequest();
            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                hyperFindRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);
            Response hyperFindResponse = this.ProcessResponse(tupleResponse.Item1);

            return hyperFindResponse;
        }

        /// <summary>
        /// Creates hyper find request.
        /// </summary>
        /// <returns>Request string.</returns>
        private string CreateHyperFindRequest()
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            Request rq = new Request()
            {
                HyperFindQuery = new RequestHyperFindQuery()
                {
                    VisibilityCode = ApiConstants.PublicVisibilityCode,
                },
                Action = ApiConstants.LoadAllQueries,
            };

            return rq.XmlSerialize();
        }

        /// <summary>
        /// Process response class.
        /// </summary>
        /// <param name="strResponse">String response.</param>
        /// <returns>Process response.</returns>
        private Response ProcessResponse(string strResponse)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));

            return XmlConvertHelper.DeserializeObject<Response>(xResponse.ToString());
        }
    }
}