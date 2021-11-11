// ---------------------------------------------------------------------------
// <copyright file="BlueYonderJwtTokenService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Connector.BlueYonder.Services
{
    using System;
    using System.Collections.Generic;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using System.Text;
    using Microsoft.IdentityModel.Tokens;
    using WfmTeams.Adapter.Services;
    using WfmTeams.Connector.BlueYonder.Options;

    public class BlueYonderJwtTokenService : IJwtTokenService
    {
        private readonly BlueYonderPersonaOptions _options;

        private readonly ISystemTimeService _timeService;

        public BlueYonderJwtTokenService(BlueYonderPersonaOptions options, ISystemTimeService timeService)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _timeService = timeService ?? throw new ArgumentNullException(nameof(timeService));
        }

        public string CreateToken(string sub)
        {
            var now = _timeService.UtcNow;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, sub)
            };

            var jwt = new JwtSecurityToken(
                issuer: _options.FederatedAuthTokenIssuer,
                audience: _options.FederatedAuthTokenIssuer,
                claims: claims,
                notBefore: now,
                expires: now.AddSeconds(_options.FederatedAuthTokenExpiration),
                signingCredentials: GetSigningCredentials());

            return new JwtSecurityTokenHandler().WriteToken(jwt);
        }

        public string ParseToken(string token)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var validationParams = new TokenValidationParameters
            {
                ValidIssuer = _options.FederatedAuthTokenIssuer,
                ValidAudience = _options.FederatedAuthTokenIssuer,
                IssuerSigningKey = GetSigningKey()
            };

            tokenHandler.ValidateToken(token, validationParams, out var validatedToken);

            return ((JwtSecurityToken)validatedToken).Subject;
        }

        private SigningCredentials GetSigningCredentials()
        {
            var signingKey = GetSigningKey();
            return new SigningCredentials(signingKey, _options.FederatedAuthTokenAlgorithm);
        }

        private SecurityKey GetSigningKey()
        {
            var symmetricKeyAsBase64 = _options.FederatedAuthTokenSigningSecret;
            var keyByteArray = Encoding.UTF8.GetBytes(symmetricKeyAsBase64);
            return new SymmetricSecurityKey(keyByteArray);
        }
    }
}
