// <copyright file="CustomAuthorize.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.Primitives;
    using Microsoft.IdentityModel.Protocols;
    using Microsoft.IdentityModel.Protocols.OpenIdConnect;
    using Microsoft.IdentityModel.Tokens;
    using Microsoft.Teams.Shifts.Integration.API.Common;
    using Microsoft.Teams.Shifts.Integration.API.Models;

    /// <summary>
    /// Custom Authorize Token.
    /// </summary>
    public class CustomAuthorize : AuthorizationHandler<HasAuthRequirement>
    {
        private readonly TelemetryClient telemetryClient;
        private readonly AppSettings appSettings;
        private IConfiguration configuration;

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomAuthorize"/> class.
        /// </summary>
        /// <param name="configuration">Configuration object.</param>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="appSettings">App settings DI.</param>
        public CustomAuthorize(
            IConfiguration configuration,
            TelemetryClient telemetryClient,
            AppSettings appSettings)
        {
            this.configuration = configuration;
            this.telemetryClient = telemetryClient;
            this.appSettings = appSettings;
        }

        /// <summary>
        /// Override HandleRequirementAsync of AuthorizationHandler.
        /// </summary>
        /// <param name="context">AuthorizationHandlerContext.</param>
        /// <param name="requirement">HasAuthRequirement.</param>
        /// <returns>Succeed if authorization is done.</returns>
        protected override async Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            HasAuthRequirement requirement)
        {
            if (context is null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var authFilterCtx = (AspNetCore.Mvc.Filters.AuthorizationFilterContext)context.Resource;
            var httpContext = authFilterCtx.HttpContext;
            StringValues accessToken = string.Empty;
            var hasAccessTokenFromConfig = httpContext.Request.Headers.TryGetValue("AccessToken", out accessToken);
            bool hasToken = false;

            // Checking to see if the token is coming from the Logic App on Azure or the Config Web App.
            if (!hasAccessTokenFromConfig)
            {
                hasToken = httpContext.Request.Headers.TryGetValue("Authorization", out accessToken);
            }

            if (hasToken || hasAccessTokenFromConfig)
            {
                var tokenValue = accessToken[0].Split(new char[] { ' ' })[1];
                var jwtToken = await this.ValidateAsync(tokenValue, hasAccessTokenFromConfig).ConfigureAwait(false);

                var isAppIdFound = jwtToken != null ? jwtToken.Claims.Where(c => c.Type == "appid" && c.Value == requirement.AppID).FirstOrDefault() : null;
                if (isAppIdFound != null)
                {
                    context.Succeed(requirement);
                }
                else
                {
                    httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
                }
            }
            else
            {
                httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;
            }
        }

        /// <summary>
        /// This method will validate the incoming token properly, and check the signature.
        /// </summary>
        /// <param name="tokenValue">The JWT token.</param>
        /// <returns>The validated token.</returns>
        private async Task<JwtSecurityToken> ValidateAsync(string tokenValue, bool hasAccessTokenFromConfig)
        {
            try
            {
                var tenantId = this.configuration["TenantId"];
                var audience = this.configuration["ClientId"];
                var issuer = $"https://sts.windows.net/{tenantId}/";

                TokenValidationParameters validationParameters;
                if (hasAccessTokenFromConfig)
                {
                    var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(this.appSettings.ClientSecret));

                    validationParameters = new TokenValidationParameters
                    {
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = securityKey,
                        ValidateLifetime = true,
                    };
                }
                else
                {
                    string stsDiscoveryEndpoint = issuer + ".well-known/openid-configuration";

                    ConfigurationManager<OpenIdConnectConfiguration> configManager = new ConfigurationManager<OpenIdConnectConfiguration>(
                    stsDiscoveryEndpoint,
                    new OpenIdConnectConfigurationRetriever(),
                    new HttpDocumentRetriever());

                    var config = await configManager.GetConfigurationAsync().ConfigureAwait(false);
                    validationParameters = new TokenValidationParameters
                    {
                        RequireExpirationTime = true,
                        RequireSignedTokens = true,
                        ValidateAudience = true,
                        ValidAudience = audience,
                        ValidateIssuer = true,
                        ValidIssuer = issuer,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKeys = config.SigningKeys,
                        ValidateLifetime = true,
                    };
                }

                JwtSecurityTokenHandler tokendHandler = new JwtSecurityTokenHandler();

                SecurityToken jwt;

                // The actual validation happens here as part of the JwtSecurityTokenHandler library.
                var result = tokendHandler.ValidateToken(tokenValue, validationParameters, out jwt);

                return jwt as JwtSecurityToken;
            }
            catch (Exception ex)
            {
                // Checking if the exception coming is a SecurityTokenInvalidSignatureException type.
                if (ex is SecurityTokenInvalidSignatureException)
                {
                    this.telemetryClient.TrackException(ex);
                    throw;
                }

                return null;
            }
        }
    }
}