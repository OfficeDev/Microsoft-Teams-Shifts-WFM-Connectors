using JdaTeams.Connector.Models;
using System.ComponentModel.DataAnnotations;

namespace JdaTeams.Connector.Functions.Models
{
    public class SubscribeModel
    {
        [Required]
        public string TeamId { get; set; }

        [Required]
        public string StoreId { get; set; }

        [Required]
        public string BaseAddress { get; set; }

        [Required]
        public string Username { get; set; }

        [Required]
        public string Password { get; set; }

        public string AccessToken { get; set; }

        public string RefreshToken { get; set; }

        public string AuthorizationCode { get; set; }

        public string RedirectUri { get; set; }

        public string WebhookUrl { get; set; }

        public ConnectionModel AsConnectionModel() => new ConnectionModel
        {
            TeamId = TeamId,
            StoreId = StoreId,
            BaseAddress = BaseAddress,
            WebhookUrl = WebhookUrl
        };

        public CredentialsModel AsCredentialsModel() => new CredentialsModel
        {
            BaseAddress = BaseAddress,
            Username = Username,
            Password = Password
        };

        public TokenModel AsTokenModel() => new TokenModel
        {
            AccessToken = AccessToken,
            RefreshToken = RefreshToken
        };

        public TeamModel AsTeamModel() => new TeamModel
        {
            StoreId = StoreId,
            TeamId = TeamId,
            WebhookUrl = WebhookUrl
        };
    }
}
