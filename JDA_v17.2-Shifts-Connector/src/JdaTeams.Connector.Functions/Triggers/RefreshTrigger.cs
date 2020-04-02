using IdentityModel.Client;
using JdaTeams.Connector.Http;
using JdaTeams.Connector.MicrosoftGraph.Extensions;
using JdaTeams.Connector.MicrosoftGraph.Options;
using JdaTeams.Connector.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class RefreshTrigger
    {
        private readonly MicrosoftGraphOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public RefreshTrigger(MicrosoftGraphOptions options, IHttpClientFactory httpClientFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [FunctionName(nameof(RefreshTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "refresh")] TokenModel tokenModel,
            ILogger log)
        {
            var tokenResponse = await _httpClientFactory.Client.RequestRefreshTokenAsync(_options, tokenModel);

            return new JsonResult(tokenResponse);
        }
    }
}
