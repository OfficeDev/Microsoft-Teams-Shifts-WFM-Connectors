using JdaTeams.Connector.Models;

namespace JdaTeams.Connector.Functions.Models
{
    public class TeamModel
    {
        public string TeamId { get; set; }
        public string StoreId { get; set; }
        public string WebhookUrl { get; set; }
        public bool Initialized { get; set; } = false;
        public string TimeZoneInfoId { get; set; }

        public static TeamModel FromConnection(ConnectionModel connectionModel)
        {
            return new TeamModel
            {
                TeamId = connectionModel.TeamId,
                StoreId = connectionModel.StoreId,
                WebhookUrl = connectionModel.WebhookUrl,
                Initialized = true,
                TimeZoneInfoId = connectionModel.TimeZoneInfoId
            };
        }
    }
}
