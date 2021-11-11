// ---------------------------------------------------------------------------
// <copyright file="AppTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Services;

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
            var appStream = await _appService.OpenAppStreamAsync().ConfigureAwait(false);
            return new FileStreamResult(appStream, "text/html");
        }
    }
}
