namespace JdaTeams.Connector.AzureStorage.Options
{
    public class AzureStorageOptions
    {
        public string ConnectionString { get; set; }
        public string ShiftsContainerName { get; set; } = "shifts";
        public string AppContainerName { get; set; } = "app";
        public string AppBlobName { get; set; } = "index.html";
        public string TeamTableName { get; set; } = "teams";
        public int TakeCount { get; set; } = 1000;
        public string TimeZoneTableName { get; set; } = "timezones";
    }
}
