using Microsoft.WindowsAzure.Storage.Table;

namespace JdaTeams.Connector.AzureStorage.Entities
{
    public class TimeZoneEntity : TableEntity
    {
        public const string DefaultPartitionKey = "TimeZones";

        public TimeZoneEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public string JdaTimeZoneName { get; set; }
        public string TimeZoneInfoId { get; set; }
        public string TeamsTimeZone { get; set; }
    }
}