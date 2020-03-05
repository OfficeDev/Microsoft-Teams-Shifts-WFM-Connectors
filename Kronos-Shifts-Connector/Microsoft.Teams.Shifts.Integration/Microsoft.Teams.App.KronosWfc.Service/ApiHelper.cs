// <copyright file="ApiHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Service
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Common;

    /// <summary>
    /// API helper Class.
    /// </summary>
    public sealed class ApiHelper : IApiHelper
    {
        /// <summary>
        /// Send Soap Post request.
        /// </summary>
        /// <param name="endpointUrl">End point URL.</param>
        /// <param name="soapEnvOpen">Soap ENv open.</param>
        /// <param name="reqXml">Request XML.</param>
        /// <param name="soapEnvClose">Soap Env Close.</param>
        /// <param name="jSession">Session Id.</param>
        /// <returns>Soap request response.</returns>
        public async Task<Tuple<string, string>> SendSoapPostRequestAsync(
            Uri endpointUrl,
            string soapEnvOpen,
            string reqXml,
            string soapEnvClose,
            string jSession)
        {
            string soapString = $"{soapEnvOpen}{reqXml}{soapEnvClose}";

            HttpResponseMessage response = await this.PostXmlRequestAsync(endpointUrl, soapString, jSession).ConfigureAwait(false);
            string content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            if (string.IsNullOrEmpty(jSession))
            {
                jSession = response.Headers.Where(x => x.Key == "Set-Cookie")
                    .FirstOrDefault().Value
                    .FirstOrDefault()
                    .ToString(CultureInfo.InvariantCulture);
            }

            return new Tuple<string, string>(content, jSession);
        }

        /// <summary>
        /// Post XMl request.
        /// </summary>
        /// <param name="baseUrl">Base URL.</param>
        /// <param name="xmlString">XML string.</param>
        /// <param name="jSession">Session Id.</param>
        /// <returns>Response message.</returns>
        private async Task<HttpResponseMessage> PostXmlRequestAsync(Uri baseUrl, string xmlString, string jSession)
        {
            if (string.IsNullOrEmpty(jSession))
            {
                using (var httpClient = new HttpClient())
                {
                    using (var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml"))
                    {
                        httpContent.Headers.Add("SOAPAction", ApiConstants.SoapAction);
                        return await httpClient.PostAsync(baseUrl, httpContent).ConfigureAwait(false);
                    }
                }
            }
            else
            {
                using (var httpClientHandler = new HttpClientHandler { UseCookies = false })
                {
                    using (var httpClient = new HttpClient(httpClientHandler))
                    {
                        using (var httpContent = new StringContent(xmlString, Encoding.UTF8, "text/xml"))
                        {
                            httpContent.Headers.Add("SOAPAction", ApiConstants.SoapAction);
                            httpContent.Headers.Add("Cookie", jSession);
                            return await httpClient.PostAsync(baseUrl, httpContent).ConfigureAwait(false);
                        }
                    }
                }
            }
        }
    }
}