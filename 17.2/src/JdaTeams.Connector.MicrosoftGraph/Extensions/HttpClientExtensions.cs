using IdentityModel;
using IdentityModel.Client;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace JdaTeams.Connector.MicrosoftGraph.Extensions
{
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

        public static Task<TokenResponse> RequestRefreshTokenAsync(this HttpClient httpClient, MicrosoftGraphOptions options, TokenModel tokenModel)
        {
            var refreshTokenRequest = new RefreshTokenRequest
            {
                Address = options.TokenUrl,
                ClientId = options.ClientId,
                ClientSecret = options.ClientSecret,
                Scope = options.Scope,
                RefreshToken = tokenModel.RefreshToken,
                GrantType = OidcConstants.GrantTypes.RefreshToken
            };

            return httpClient.RequestRefreshTokenAsync(refreshTokenRequest);
        }
    }
}
