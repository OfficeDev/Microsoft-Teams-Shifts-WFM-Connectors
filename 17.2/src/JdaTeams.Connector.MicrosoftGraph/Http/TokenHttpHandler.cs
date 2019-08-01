using IdentityModel.Client;
using JdaTeams.Connector.MicrosoftGraph.Extensions;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace JdaTeams.Connector.MicrosoftGraph.Http
{
    public class TokenHttpHandler : DelegatingHandler
    {
        private static ConcurrentDictionary<string, TokenModel> _tokens = new ConcurrentDictionary<string, TokenModel>();
        private readonly MicrosoftGraphOptions _options;
        private readonly HttpClient _httpClient;
        private readonly string _teamId;
        private readonly ISecretsService _secretsService;

        public TokenHttpHandler(MicrosoftGraphOptions options, HttpClient httpClient, string teamId, ISecretsService secretsService)
        {
            _options = options;
            _httpClient = httpClient;
            _teamId = teamId;
            _secretsService = secretsService;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tokenModel = await GetTokenAsync();

            AddAuthenticationHeader(request, tokenModel);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            var tokenResponse = await _httpClient.RequestRefreshTokenAsync(_options, tokenModel);

            if (tokenResponse.IsError)
            {
                return response;
            }

            tokenModel = await SaveTokenAsync(tokenResponse);

            AddAuthenticationHeader(request, tokenModel);

            return await base.SendAsync(request, cancellationToken);
        }

        private async Task<TokenModel> GetTokenAsync()
        {
            if (!_tokens.TryGetValue(_teamId, out var tokenModel))
            {
                tokenModel = await _secretsService.GetTokenAsync(_teamId);

                _tokens[_teamId] = tokenModel;
            }

            return tokenModel;
        }

        private async Task<TokenModel> SaveTokenAsync(TokenResponse tokenResponse)
        {
            var tokenModel = tokenResponse.AsTokenModel();

            _tokens[_teamId] = tokenModel;

            await _secretsService.SaveTokenAsync(_teamId, tokenModel);

            return tokenModel;
        }

        private void AddAuthenticationHeader(HttpRequestMessage request, TokenModel tokenModel)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenModel.AccessToken);
        }
    }
}
