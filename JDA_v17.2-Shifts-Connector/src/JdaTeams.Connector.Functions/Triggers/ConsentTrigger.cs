using Flurl;
using JdaTeams.Connector.MicrosoftGraph.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class ConsentTrigger
    {
        private readonly MicrosoftGraphOptions _options;

        public ConsentTrigger(MicrosoftGraphOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        [FunctionName(nameof(ConsentTrigger))]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "consent")] HttpRequest req,
            ILogger log)
        {
            var error = req.Query["error"];

            if (!string.IsNullOrEmpty(error))
            {
                return TextResult($"Rejected: {req.Query["error_description"]}");
            }

            var adminConsent = req.Query["admin_consent"];

            if (!string.IsNullOrEmpty(adminConsent) && bool.TryParse(adminConsent, out var consented))
            {
                if (consented)
                {
                    return TextResult("Complete: The admin approved the request");
                }
                else
                {
                    return TextResult("Rejected: The admin did not approve the request");
                }
            }

            var redirectUri = new Url(req.GetEncodedUrl()).Path;
            var adminConsentUrl = _options.AdminConsentUrl
                .SetQueryParam("client_id", _options.ClientId)
                .SetQueryParam("redirect_uri", redirectUri)
                .SetQueryParam("state", null);

            return new RedirectResult(adminConsentUrl);
        }

        private IActionResult TextResult(string text)
        {
            return new ContentResult
            {
                Content = text,
                ContentType = "text/plain"
            };
        }
    }
}
