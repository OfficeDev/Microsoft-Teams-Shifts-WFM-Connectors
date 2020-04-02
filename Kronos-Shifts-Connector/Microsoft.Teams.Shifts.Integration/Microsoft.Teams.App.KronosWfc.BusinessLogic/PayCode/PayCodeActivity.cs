// <copyright file="PayCodeActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.PayCodes
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.PayCodes;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.PayCodes;
    using Microsoft.Teams.App.KronosWfc.Service;

    /// <summary>
    /// This class is used to fetch kronos payCodes.
    /// </summary>
    public class PayCodeActivity : IPayCodeActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Initializes a new instance of the <see cref="PayCodeActivity"/> class.
        /// Initialize PayCodeActivity.
        /// </summary>
        /// <param name="telemetryClient">Telemetry initialize.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public PayCodeActivity(TelemetryClient telemetryClient, IApiHelper apiHelper)
        {
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// Fetch kronos PayCodes.
        /// </summary>
        /// <param name="endPointUrl">Kronos url.</param>
        /// <param name="jSession">Kronos session.</param>
        /// <returns>List of kronos paycodes.</returns>
        public async Task<List<string>> FetchPayCodesAsync(Uri endPointUrl, string jSession)
        {
            string xmlScheduleRequest = string.Empty;

            xmlScheduleRequest = this.CreateLoadPayCodeRequest();

            var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                endPointUrl,
                ApiConstants.SoapEnvOpen,
                xmlScheduleRequest,
                ApiConstants.SoapEnvClose,
                jSession).ConfigureAwait(false);

            Response scheduleResponse = this.ProcessResponse(tupleResponse.Item1);
            
            // Reading Paycodes from Kronos
            var payCodeList = scheduleResponse.PayCode.Where(c => c.ExcuseAbsenceFlag == "true" && c.IsVisibleFlag == "true").Select(x => x.PayCodeName).ToList();
            this.telemetryClient.TrackTrace($"Number of Paycodes fetched from Kronos: {payCodeList.Count}");
            return payCodeList;
        }

        private string CreateLoadPayCodeRequest()
        {
            Request request = new Request()
            {
                Action = ApiConstants.LoadAllPayCodes,
                PayCode = string.Empty,
            };

            return request.XmlSerialize();
        }

        /// <summary>
        /// Read the xml response into Response object.
        /// </summary>
        /// <param name="strResponse">xml response string.</param>
        /// <returns>Response object.</returns>
        private Response ProcessResponse(string strResponse)
        {
            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<Response>(xResponse.ToString());
        }
    }
}
