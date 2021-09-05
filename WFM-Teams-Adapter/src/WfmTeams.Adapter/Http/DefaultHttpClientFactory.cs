// ---------------------------------------------------------------------------
// <copyright file="DefaultHttpClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Http
{
    using System.Net.Http;

    /// <summary>
    /// Http client factory providing a single static instance of HttpClient.
    /// </summary>
    public class DefaultHttpClientFactory : IHttpClientFactory
    {
        private static HttpClient _httpClient = new HttpClient();
        public HttpClient Client => _httpClient;
        public DelegatingHandler Handler => null;
    }
}
