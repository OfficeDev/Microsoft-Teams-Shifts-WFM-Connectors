using JdaTeams.Connector.Http;
using JdaTeams.Connector.MicrosoftGraph.Http;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Services;
using System;
using System.Collections.Generic;
using System.Text;

namespace JdaTeams.Connector.MicrosoftGraph
{
    public class MicrosoftGraphClientFactory : IMicrosoftGraphClientFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ISecretsService _secretsService;

        public MicrosoftGraphClientFactory(IHttpClientFactory httpClientFactory, ISecretsService secretsService)
        {
            _httpClientFactory = httpClientFactory;
            _secretsService = secretsService;
        }

        public IMicrosoftGraphClient CreateClient(MicrosoftGraphOptions options, string teamId)
        {
            var httpHandler = _httpClientFactory.Handler ?? new TokenHttpHandler(options, _httpClientFactory.Client, teamId, _secretsService);
            return new MicrosoftGraphClient(new Uri(options.BaseAddress), httpHandler);
        }
    }
}
