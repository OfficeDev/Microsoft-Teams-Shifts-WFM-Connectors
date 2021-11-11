// ---------------------------------------------------------------------------
// <copyright file="FederatedHttpHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Http
{
    using System;
    using System.Net.Http;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Options;

    public class FederatedHttpHandler : CookieHttpHandler
    {
        private readonly IJwtTokenService _tokenService;

        public FederatedHttpHandler(BlueYonderPersonaOptions options, ICacheService cacheService, IJwtTokenService tokenService, CredentialsModel credentials, string principalId)
            : base(options, cacheService, credentials, principalId)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        protected override void AddAuthHeader(HttpRequestMessage request)
        {
            var encodedJwt = _tokenService.CreateToken(_credentials.Username);
            request.Headers.TryAddWithoutValidation(_options.FederatedAuthTokenName, encodedJwt);

            base.AddAuthHeader(request);
        }

        protected override HttpContent BuildAuthContent()
        {
            // for the federated authentication scenario, the request does not need any content
            return null;
        }
    }
}
