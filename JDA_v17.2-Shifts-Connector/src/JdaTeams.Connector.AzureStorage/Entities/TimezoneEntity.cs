using Microsoft.WindowsAzure.Storage.Table;

namespace JdaTeams.Connector.AzureStorage.Entities
{
    public class TimezoneEntity : TableEntity
    {
        public const string DefaultPartitionKey = "timezones";

        public TimezoneEntity()
        {
            PartitionKey = DefaultPartitionKey;
        }

        public string TimezoneInfoId { get; set; }
    }
}