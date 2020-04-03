// <copyright file="LogonActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Logon;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon;
    using Microsoft.Teams.App.KronosWfc.Service;

    /// <summary>
    /// Logon Activity class.
    /// </summary>
    [Serializable]
    public class LogonActivity : ILogonActivity
    {
        private readonly TelemetryClient telemetryClient;
        private readonly IApiHelper apiHelper;

        /// <summary>
        /// Login request.
        /// </summary>
        private Request loginRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="LogonActivity" /> class.
        /// </summary>
        /// <param name="loginRequest">Login Request.</param>
        /// <param name="telemetryClient">The telemetry mechanism.</param>
        /// <param name="apiHelper">API helper to fetch tuple response by post soap requests.</param>
        public LogonActivity(
            Request loginRequest,
            TelemetryClient telemetryClient,
            IApiHelper apiHelper)
        {
            this.loginRequest = loginRequest;
            this.telemetryClient = telemetryClient;
            this.apiHelper = apiHelper;
        }

        /// <summary>
        /// This method calls the logOn api to log the user to Kronos.
        /// </summary>
        /// <param name="username">The user name string.</param>
        /// <param name="password">The user password.</param>
        /// <param name="endPointUrl">Kronos endpoint url.</param>
        /// <returns>Response object.</returns>
        public async Task<Response> LogonAsync(string username, string password, Uri endPointUrl)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            try
            {
                string xmlLoginRequest = this.CreateLogOnRequest(username, password);

                var tupleResponse = await this.apiHelper.SendSoapPostRequestAsync(
                    endPointUrl,
                    ApiConstants.SoapEnvOpen,
                    xmlLoginRequest,
                    ApiConstants.SoapEnvClose,
                    string.Empty).ConfigureAwait(false);

                Response logonResponse = this.ProcessResponse(tupleResponse.Item1);

                // Fetch the session of Kronos when login to Kronos is successful.
                if (logonResponse != null)
                {
                    logonResponse.Jsession = tupleResponse.Item2;
                }

                return logonResponse;
            }
            catch (Exception ex)
            {
                this.telemetryClient.TrackException(ex);
                throw;
            }
        }

        /// <summary>
        /// Used to create xml logon request string.
        /// </summary>
        /// <param name="username">User request object.</param>
        /// <param name="password">User password.</param>
        /// <returns>Logon request xml string.</returns>
        private string CreateLogOnRequest(string username, string password)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            this.loginRequest.Object = ApiConstants.System;
            this.loginRequest.Action = ApiConstants.LogonAction;
            this.loginRequest.Username = username;
            this.loginRequest.Password = password;
            return this.loginRequest.XmlSerialize<Request>();
        }

        /// <summary>
        /// Read the xml response into Response object.
        /// </summary>
        /// <param name="strResponse">xml response string.</param>
        /// <returns>Response object.</returns>
        private Response ProcessResponse(string strResponse)
        {
            var telemetryProps = new Dictionary<string, string>()
            {
                { "AssemblyName", Assembly.GetExecutingAssembly().FullName },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, telemetryProps);

            XDocument xDoc = XDocument.Parse(strResponse);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(ApiConstants.Response, StringComparison.Ordinal));

            // xResponse will be null when provided Kronos URL is incorrect.
            if (xResponse == null)
            {
                return null;
            }

            return XmlConvertHelper.DeserializeObject<Response>(xResponse.ToString());
        }
    }
}