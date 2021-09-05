// ---------------------------------------------------------------------------
// <copyright file="BlueYonderUnauthorizedAccessException.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Exceptions
{
    using System;
    using System.Linq;
    using System.Net;
    using System.Net.Http;

    public class BlueYonderUnauthorizedAccessException : UnauthorizedAccessException
    {
        public BlueYonderUnauthorizedAccessException()
        {
        }

        public BlueYonderUnauthorizedAccessException(string message) : base(message)
        {
        }

        public BlueYonderUnauthorizedAccessException(string message, Exception innerException) : base(message, innerException)
        {
        }

        public BlueYonderUnauthorizedAccessException(string message, HttpRequestMessage request, HttpResponseMessage response, string loginName, string principalId) : base(message)
        {
            RequestUrl = request.RequestUri.AbsoluteUri;
            if (request.Headers != null)
            {
                var dict = request.Headers.ToDictionary(a => a.Key, a => string.Join(";", a.Value));
                RequestHeaders = string.Join("|", dict.Select(x => $"{x.Key}={x.Value}"));
            }

            ResponseStatusCode = response.StatusCode;
            ResponseContent = response.Content.ReadAsStringAsync().Result;
            if (response.Headers != null)
            {
                var dict = response.Headers.ToDictionary(a => a.Key, a => string.Join(";", a.Value));
                ResponseHeaders = string.Join("|", dict.Select(x => $"{x.Key}={x.Value}"));
            }

            LoginName = loginName;
            PrincipalId = principalId;
        }

        public string LoginName { get; }
        public string PrincipalId { get; }
        public string RequestHeaders { get; }
        public string RequestUrl { get; }
        public string ResponseContent { get; }
        public string ResponseHeaders { get; }
        public HttpStatusCode ResponseStatusCode { get; }
    }
}
