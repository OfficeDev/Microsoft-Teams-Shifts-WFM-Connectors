using JdaTeams.Connector.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.Functions.Triggers
{
    public class AppTrigger
    {
        private readonly IAppService _appService;

        public AppTrigger(IAppService appService)
        {
            _appService = appService ?? throw new ArgumentNullException(nameof(appService));
        }

        [FunctionName(nameof(AppTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "app")] HttpRequest req,
            ILogger log)
        {
            var appStream = await _appService.OpenAppStreamAsync();
            return new FileStreamResult(appStream, "text/html");
        }
    }
}
