// ---------------------------------------------------------------------------
// <copyright file="FederatedAuthTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Services;

    public class FederatedAuthTrigger
    {
        private readonly IJwtTokenService _tokenService;
        private readonly IWfmAuthService _wfmFederatedAuthService;

        public FederatedAuthTrigger(IWfmAuthService wfmFederatedAuthService, IJwtTokenService tokenService)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
            _wfmFederatedAuthService = wfmFederatedAuthService ?? throw new ArgumentNullException(nameof(wfmFederatedAuthService));
        }

        [FunctionName(nameof(FederatedAuthTrigger))]
        public IActionResult Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "auth")] HttpRequest req,
            ILogger log)
        {
            return _wfmFederatedAuthService.HandleFederatedAuthRequest(req, log);
        }
    }
}
