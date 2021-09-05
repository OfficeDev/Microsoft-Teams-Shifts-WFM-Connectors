// ---------------------------------------------------------------------------
// <copyright file="MicrosoftGraphClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph
{
    using System;
    using System.Net.Http;
    using WfmTeams.Adapter.Http;
    using WfmTeams.Adapter.MicrosoftGraph.Handlers;
    using WfmTeams.Adapter.MicrosoftGraph.Options;
    using WfmTeams.Adapter.Services;

    public class MicrosoftGraphClientFactory : IMicrosoftGraphClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;

        private readonly ISecretsService _secretsService;

        public MicrosoftGraphClientFactory(IHttpClientFactory httpClientFactory, ISecretsService secretsService)
        {
            _httpClientFactory = httpClientFactory;
            _secretsService = secretsService;
        }

        public IMicrosoftGraphClient CreateUserClient(MicrosoftGraphOptions options, string userId)
        {
            var httpHandler = _httpClientFactory.Handler ?? new TokenDelegatingHandler(options, _httpClientFactory.Client, userId);
            DelegatingHandler[] handlers = { httpHandler, new PassThroughDelegatingHandler() };
            return new MicrosoftGraphClient(new Uri(options.MSBaseAddress), handlers);
        }

        public IMicrosoftGraphClient CreateClient(MicrosoftGraphOptions options, string teamId)
        {
            var httpHandler = _httpClientFactory.Handler ?? new TokenDelegatingHandler(options, _httpClientFactory.Client, options.GraphApiUserId);
            DelegatingHandler[] handlers = { httpHandler, new PassThroughDelegatingHandler() };
            return new MicrosoftGraphClient(new Uri(options.MSBaseAddress), handlers);
        }

        public IMicrosoftGraphClient CreateClientNoPassthrough(MicrosoftGraphOptions options, string teamId)
        {
            var httpHandler = _httpClientFactory.Handler ?? new TokenDelegatingHandler(options, _httpClientFactory.Client, options.GraphApiUserId);
            DelegatingHandler[] handlers = { httpHandler };
            return new MicrosoftGraphClient(new Uri(options.MSBaseAddress), handlers);
        }
    }
}
