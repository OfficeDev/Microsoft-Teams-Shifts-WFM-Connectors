// ---------------------------------------------------------------------------
// <copyright file="HttpClientExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Extensions
{
    using System.Net.Http;
    using System.Threading.Tasks;
    using IdentityModel;
    using IdentityModel.Client;
    using WfmTeams.Adapter.MicrosoftGraph.Options;

    public static class HttpClientExtensions
    {
        public static Task<TokenResponse> RequestTokenAsync(this HttpClient httpClient, MicrosoftGraphOptions options, string redirectUri, string code)
        {
            Guard.ArgumentNotEmpty(redirectUri, nameof(redirectUri));
            Guard.ArgumentNotEmpty(code, nameof(code));

            var tokenRequest = new TokenRequest
            {
                Address = options.TokenUrl,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                GrantType = OidcConstants.GrantTypes.AuthorizationCode,
                Parameters =
                {
                    { "code", code },
                    { "redirect_uri", redirectUri },
                    { "scope", options.Scope }
                }
            };

            return httpClient.RequestTokenAsync(tokenRequest);
        }

        public static Task<TokenResponse> RequestTokenAsync(this HttpClient httpClient, MicrosoftGraphOptions options)
        {
            var tokenRequest = new TokenRequest
            {
                Address = string.Format(options.AppTokenUrl, options.TenantId),
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                GrantType = OidcConstants.GrantTypes.ClientCredentials,
                Parameters =
                {
                    { "scope", options.AppScope }
                }
            };

            return httpClient.RequestTokenAsync(tokenRequest);
        }
    }
}
