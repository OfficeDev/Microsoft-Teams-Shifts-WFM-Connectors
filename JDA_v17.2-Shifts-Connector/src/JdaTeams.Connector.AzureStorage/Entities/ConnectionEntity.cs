using JdaTeams.Connector.Models;
using Microsoft.WindowsAzure.Storage.Table;

namespace JdaTeams.Connector.AzureStorage.Entities
{
    public class ConnectionEntity : TableEntity
    {
        public const string DefaultPartitionKey = "teams";

        public ConnectionEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public string StoreId { get; set; }
        public string StoreName { get; set; }
        public string BaseAddress { get; set; }
        public string WebhookUrl { get; set; }
        public string TeamName { get; set; }
        public string TimezoneInfoId { get; set; }

        public ConnectionModel AsModel()
        {
            return new ConnectionModel
            {
                TeamId = RowKey,
                StoreId = StoreId,
                StoreName = StoreName,
                BaseAddress = BaseAddress,
                WebhookUrl = WebhookUrl,
                TeamName = TeamName,
                TimezoneInfoId = TimezoneInfoId,
            };
        }

        public static ConnectionEntity FromModel(ConnectionModel model)
        {
            return new ConnectionEntity()
            {
                RowKey = model.TeamId,
                StoreId = model.StoreId,
                StoreName = model.StoreName,
                BaseAddress = model.BaseAddress,
                WebhookUrl = model.WebhookUrl,
                TeamName = model.TeamName,
                TimezoneInfoId = model.TimezoneInfoId
            };
        }

        public static ConnectionEntity FromId(string teamId)
        {
            return new ConnectionEntity()
            {
                RowKey = teamId,
                ETag = "*"
            };
        }
    }
}
