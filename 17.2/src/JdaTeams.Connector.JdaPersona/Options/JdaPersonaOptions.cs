using JdaTeams.Connector.Models;
using JdaTeams.Connector.Options;

namespace JdaTeams.Connector.JdaPersona.Options
{
    public class JdaPersonaOptions : ConnectorOptions
    {
        public string JdaBaseAddress { get; set; }
        public string JdaUsername { get; set; }
        public string JdaPassword { get; set; }
        public string JdaApiPath { get; set; } = "/data/retailwebapi/api/v1-beta5";
        public string JdaCookieAuthPath { get; set; } = "/data/login";
        public int MaximumUsers { get; set; } = 100;

        public CredentialsModel AsCredentials()
        {
            return new CredentialsModel
            {
                BaseAddress = JdaBaseAddress,
                Username = JdaUsername,
                Password = JdaPassword
            };
        }
    }
}
