// ---------------------------------------------------------------------------
// <copyright file="BlueYonderAuthService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using System;
    using System.Net;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.Extensions.Localization;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Exceptions;
    using WfmTeams.Connector.BlueYonder.Extensions;
    using WfmTeams.Connector.BlueYonder.Models;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderAuthService : BlueYonderBaseService, IWfmAuthService
    {
        private readonly IJwtTokenService _tokenService;

        public BlueYonderAuthService(BlueYonderPersonaOptions options, IJwtTokenService tokenService, ISecretsService secretsService, IBlueYonderClientFactory clientFactory, IStringLocalizer<BlueYonderConfigService> stringLocalizer)
            : base(options, secretsService, clientFactory, stringLocalizer)
        {
            _tokenService = tokenService ?? throw new ArgumentNullException(nameof(tokenService));
        }

        public IActionResult HandleFederatedAuthRequest(HttpRequest request, ILogger log)
        {
            if (!request.Form.ContainsKey(_options.FederatedAuthTokenName))
            {
                log.LogTokenError("Federated authentication token missing");
                return new BadRequestResult();
            }

            var token = request.Form[_options.FederatedAuthTokenName][0];

            try
            {
                var userId = _tokenService.ParseToken(token);
                log.LogTokenSuccess(userId);
                return new ContentResult
                {
                    Content = $"<userId>{userId}</userId>",
                    ContentType = "application/xml",
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                log.LogTokenError(ex, "Federated authentication token invalid or expired", token);
                return new UnauthorizedResult();
            }
        }

        public async Task RefreshEmployeeTokenAsync(EmployeeModel employee, string wfmBuId, ILogger log)
        {
            try
            {
                IBlueYonderClient client;
                if (employee.IsManager)
                {
                    // make a call to get the site information and if the manager's token has
                    // expired then a new one will be obtained as part of the call
                    client = CreateSiteManagerPublicClient(employee);
                    var response = await client.GetSiteInfoAsync(wfmBuId).ConfigureAwait(false);
                    if (response is Error)
                    {
                        var error = (Error)response;
                        log.LogEmployeeTokenFailure(employee.WfmEmployeeId, error.ErrorCode, error.UserMessage);
                    }
                }
                else
                {
                    // make a call to get the employee info and if the employee's token has expired
                    // then a new one will be obtained as part of the call
                    client = CreateEssPublicClient(employee);
                    await client.GetMyInfoAsync().ConfigureAwait(false);
                }
            }
            catch (BlueYonderUnauthorizedAccessException ex)
            {
                log.LogBlueYonderUnauthorizedAccessException(ex, "", $"Failed to refresh the token for employee: {employee.DisplayName} ({employee.WfmEmployeeId}-{employee.WfmLoginName})");
            }
            catch (Exception ex)
            {
                log.LogEmployeeTokenRefreshError(ex, $"Failed to refresh token for employee: {employee.DisplayName} ({employee.WfmEmployeeId}-{employee.WfmLoginName})");
            }
        }
    }
}
