// ---------------------------------------------------------------------------
// <copyright file="TokenDelegatingHandler.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.MicrosoftGraph.Handlers
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading;
    using System.Threading.Tasks;
    using IdentityModel.Client;
    using WfmTeams.Adapter.MicrosoftGraph.Exceptions;
    using WfmTeams.Adapter.MicrosoftGraph.Extensions;
    using WfmTeams.Adapter.MicrosoftGraph.Options;

    public class TokenDelegatingHandler : DelegatingHandler
    {
        // the token is for the application
        private static string _appToken;

        private readonly HttpClient _httpClient;
        private readonly MicrosoftGraphOptions _options;
        private readonly string _userId;

        public TokenDelegatingHandler(MicrosoftGraphOptions options, HttpClient httpClient, string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new ArgumentException("message", nameof(userId));
            }

            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _userId = userId;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_appToken))
            {
                await RequestAppTokenAsync();
            }

            AddAuthenticationHeader(request, _appToken);
            AddRequiredHeaders(request);

            var response = await base.SendAsync(request, cancellationToken);

            if (response.StatusCode != HttpStatusCode.Unauthorized)
            {
                return response;
            }

            await RequestAppTokenAsync();

            AddAuthenticationHeader(request, _appToken);

            return await base.SendAsync(request, cancellationToken);
        }

        private void AddAuthenticationHeader(HttpRequestMessage request, string accessToken)
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        private void AddRequiredHeaders(HttpRequestMessage request)
        {
            const string ACTS_AS_HEADER = "MS-APP-ACTS-AS";

            if (request.Headers.Contains(ACTS_AS_HEADER))
            {
                request.Headers.Remove(ACTS_AS_HEADER);
            }

            request.Headers.Add(ACTS_AS_HEADER, _userId);
        }

        private async Task RequestAppTokenAsync()
        {
            var tokenResponse = await _httpClient.RequestTokenAsync(_options);
            if (tokenResponse.IsError)
            {
                throw new MicrosoftGraphException(new Models.GraphError
                {
                    Code = tokenResponse.Error,
                    Message = tokenResponse.ErrorDescription
                });
            }
            else
            {
                _appToken = tokenResponse.AccessToken;
            }
        }
    }
}
