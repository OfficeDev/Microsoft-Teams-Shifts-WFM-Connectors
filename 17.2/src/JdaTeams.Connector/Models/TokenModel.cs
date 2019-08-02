using System;

namespace JdaTeams.Connector.Models
{
    public class TokenModel
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }
        public DateTime? ExpiresDate { get; set; }
    }
}
