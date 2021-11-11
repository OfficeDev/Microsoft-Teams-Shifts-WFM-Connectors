// ---------------------------------------------------------------------------
// <copyright file="CredentialsTrigger.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Triggers
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Azure.WebJobs;
    using Microsoft.Azure.WebJobs.Extensions.Http;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class CredentialsTrigger
    {
        private readonly ISecretsService _secretsService;

        public CredentialsTrigger(ISecretsService secretsService)
        {
            _secretsService = secretsService ?? throw new ArgumentNullException(nameof(secretsService));
        }

        [FunctionName(nameof(CredentialsTrigger))]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Admin, "post", Route = "credentials/{principalId}")] CredentialsModel credentialsModel,
            string principalId,
            ILogger log)
        {
            try
            {
                await _secretsService.SaveCredentialsAsync(credentialsModel).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "Error saving credentials");
            }

            return new OkResult();
        }
    }
}
