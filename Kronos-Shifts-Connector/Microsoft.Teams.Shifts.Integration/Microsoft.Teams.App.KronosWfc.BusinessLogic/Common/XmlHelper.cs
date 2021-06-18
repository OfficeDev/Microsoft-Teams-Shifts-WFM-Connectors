// <copyright file="XmlHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Common
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Xml.Linq;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.App.KronosWfc.Common;
    using static Microsoft.Teams.App.KronosWfc.Common.ApiConstants;

    /// <summary>
    /// A static helper class that contains extension methods to help with XML request/responses.
    /// </summary>
    public static class XmlHelper
    {
        /// <summary>
        /// Process response for an xml string.
        /// </summary>
        /// <param name="response">Response received.</param>
        /// <param name="telemetryClient">The telemetry client for logging.</param>
        /// <typeparam name="T">The type to return.</typeparam>
        /// <returns>Response object.</returns>
        public static T ProcessResponse<T>(this Tuple<string, string> response, TelemetryClient telemetryClient)
            where T : new()
        {
            if (response == null)
            {
                telemetryClient.TrackTrace($"Response of type {new T().GetType().FullName} was unable to be retrieved.");
                return default;
            }

            XDocument xDoc = XDocument.Parse(response.Item1);
            var xResponse = xDoc.Root.Descendants().FirstOrDefault(d => d.Name.LocalName.Equals(Response, StringComparison.Ordinal));
            return XmlConvertHelper.DeserializeObject<T>(xResponse.ToString());
        }
    }
}
