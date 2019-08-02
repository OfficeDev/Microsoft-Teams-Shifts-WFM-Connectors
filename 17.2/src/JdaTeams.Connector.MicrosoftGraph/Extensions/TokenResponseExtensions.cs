using IdentityModel.Client;
using JdaTeams.Connector.Models;
using System;

namespace JdaTeams.Connector.MicrosoftGraph.Extensions
{
    public static class TokenResponseExtensions
    {
        public static TokenModel AsTokenModel(this TokenResponse tokenResponse)
        {
            var tokenModel = new TokenModel
            {
                AccessToken = tokenResponse.AccessToken,
                RefreshToken = tokenResponse.RefreshToken
            };

            if (tokenResponse.ExpiresIn > 0)
            {
                tokenModel.ExpiresDate = DateTime.UtcNow
                    .AddSeconds(tokenResponse.ExpiresIn);
            }

            return tokenModel;
        }
    }
}
