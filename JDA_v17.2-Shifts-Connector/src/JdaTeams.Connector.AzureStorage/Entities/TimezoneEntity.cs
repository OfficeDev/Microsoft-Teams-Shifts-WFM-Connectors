using Microsoft.WindowsAzure.Storage.Table;

namespace JdaTeams.Connector.AzureStorage.Entities
{
    public class TimeZoneEntity : TableEntity
    {
        public const string DefaultPartitionKey = "timezones";

        public TimeZoneEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public string TimeZoneInfoId { get; set; }
    }
}