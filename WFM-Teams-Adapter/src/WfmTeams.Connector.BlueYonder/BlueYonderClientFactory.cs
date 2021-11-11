// ---------------------------------------------------------------------------
// <copyright file="BlueYonderClientFactory.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder
{
    using System;
    using System.Net.Http;
    using Flurl;
    using WfmTeams.Adapter.Http;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Http;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderClientFactory : IBlueYonderClientFactory
    {
        private readonly ICacheService _cacheService;

        private readonly IHttpClientFactory _httpClientFactory;

        private readonly BlueYonderPersonaOptions _options;

        private readonly IJwtTokenService _tokenService;

        public BlueYonderClientFactory(BlueYonderPersonaOptions byPersonaOptions, IHttpClientFactory httpClientFactory, IJwtTokenService tokenService, ICacheService cacheService)
        {
            _options = byPersonaOptions ?? throw new ArgumentNullException(nameof(byPersonaOptions));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        }

        public IBlueYonderClient CreatePublicClient(CredentialsModel credentialsModel, string principalId, string apiPath)
        {
            var httpHandler = _httpClientFactory.Handler ?? new CookieHttpHandler(_options, _cacheService, credentialsModel, principalId);
            var apiBaseAddress = credentialsModel.BaseAddress.AppendPathSegment(apiPath);

            return new BlueYonderClient(new Uri(apiBaseAddress), httpHandler);
        }

        public IBlueYonderClient CreateUserClient(CredentialsModel credentialsModel, string principalId, string apiPath)
        {
            DelegatingHandler httpHandler = _httpClientFactory.Handler ?? new FederatedHttpHandler(_options, _cacheService, _tokenService, credentialsModel, principalId);
            var apiBaseAddress = credentialsModel.BaseAddress.AppendPathSegment(apiPath);

            return new BlueYonderClient(new Uri(apiBaseAddress), httpHandler);
        }
    }
}
