using JdaTeams.Connector.Options;

namespace JdaTeams.Connector.MicrosoftGraph.Options
{
    public class MicrosoftGraphOptions : ConnectorOptions
    {
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string Scope { get; set; } = "offline_access Group.ReadWrite.All User.Read.All";
        public string AdminConsentUrl { get; set; } = "https://login.microsoftonline.com/common/adminconsent";
        public string AuthorizeUrl { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/authorize";
        public string TokenUrl { get; set; } = "https://login.microsoftonline.com/common/oauth2/v2.0/token";
        public string BaseAddress { get; set; } = "https://graph.microsoft.com/beta";
        public string UserPrincipalNameFormatString { get; set; } = "{0}";
        public string ThemeMap { get; set; }
        public string ShiftsAppUrl { get; set; }
    }
}
