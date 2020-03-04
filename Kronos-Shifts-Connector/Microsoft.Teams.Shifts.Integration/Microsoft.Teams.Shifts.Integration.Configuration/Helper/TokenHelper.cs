// <copyright file="TokenHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Helper
{
    using System;
    using System.IdentityModel.Tokens.Jwt;
    using System.Security.Claims;
    using Microsoft.Teams.Shifts.Integration.API.Common;

    /// <summary>
    /// Helper class to generate JWT for authorization.
    /// </summary>
    public class TokenHelper
    {
        private readonly AppSettings appSettings;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenHelper"/> class.
        /// </summary>
        /// <param name="appSettings">The application configuration settings.</param>
        public TokenHelper(AppSettings appSettings)
        {
            this.appSettings = appSettings;
        }

        /// <summary>
        /// This method will generat a JWT token.
        /// </summary>
        /// <returns>Returns the JWT token for the Configuration App.</returns>
        public string GenerateToken()
        {
            DateTime issuedAt = DateTime.UtcNow;
            var tenantId = this.appSettings.TenantId;
            var audience = this.appSettings.ClientId;
            var issuer = $"https://sts.windows.net/{tenantId}/";

            // Set the time when it expires.
            DateTime expires = DateTime.UtcNow.AddHours(1);

            var tokenHandler = new JwtSecurityTokenHandler();

            // Create a identity and add claims to the user which we want to log in.
            ClaimsIdentity claimsIdentity = new ClaimsIdentity(new[]
            {
                new Claim("appid", audience),
            });

            // Need to get the below secret key from key vault
            var securityKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(System.Text.Encoding.Default.GetBytes(this.appSettings.ClientSecret));
            var signingCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(securityKey, Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature);

            // Creates the JWT token.
            var token = (JwtSecurityToken)tokenHandler.CreateJwtSecurityToken(
                issuer: issuer,
                audience: audience,
                subject: claimsIdentity,
                notBefore: issuedAt,
                expires: expires,
                signingCredentials: signingCredentials);

            return tokenHandler.WriteToken(token);
        }
    }
}