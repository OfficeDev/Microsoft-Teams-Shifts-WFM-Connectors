using Flurl;
using IdentityModel.Client;
using JdaTeams.Connector.Http;
using JdaTeams.Connector.MicrosoftGraph.Extensions;
using JdaTeams.Connector.MicrosoftGraph.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class DelegateTrigger
    {
        private readonly MicrosoftGraphOptions _options;
        private readonly IHttpClientFactory _httpClientFactory;

        public DelegateTrigger(MicrosoftGraphOptions options, IHttpClientFactory httpClientFactory)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [FunctionName(nameof(DelegateTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "delegate")] HttpRequest req,
            ILogger log)
        {
            var code = req.Query["code"];
            var redirectUri = new Url(req.GetEncodedUrl()).Path;

            if (string.IsNullOrEmpty(code))
            {
                var authorizeUrl = _options.AuthorizeUrl
                    .SetQueryParam("response_type", "code")
                    .SetQueryParam("response_code", "query")
                    .SetQueryParam("client_id", _options.ClientId)
                    .SetQueryParam("scope", _options.Scope)
                    .SetQueryParam("redirect_uri", redirectUri)
                    .SetQueryParam("state", null);

                return new RedirectResult(authorizeUrl);
            }

            var tokenResponse = await _httpClientFactory.Client.RequestTokenAsync(_options, redirectUri, code);

            return new JsonResult(tokenResponse);
        }
    }
}
