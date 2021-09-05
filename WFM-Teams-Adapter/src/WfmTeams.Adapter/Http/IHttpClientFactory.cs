// ---------------------------------------------------------------------------
// <copyright file="IHttpClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Http
{
    using System.Net.Http;

    /// <summary>
    /// Defines the interface of the Http client factory.
    /// </summary>
    public interface IHttpClientFactory
    {
        HttpClient Client { get; }
        DelegatingHandler Handler { get; }
    }
}
