using JdaTeams.Connector.AzureStorage.Entities;
using JdaTeams.Connector.AzureStorage.Options;
using JdaTeams.Connector.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Threading.Tasks;

namespace JdaTeams.Connector.AzureStorage.Services
{
    public class AzureStorageTimeZoneService : ITimeZoneService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageTimeZoneService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> GetTimeZoneInfoIdAsync(string jdaTimeZoneName)
        {
            var table = GetTableReference();
            var operation = TableOperation.Retrieve<TimeZoneEntity>(TimeZoneEntity.DefaultPartitionKey, jdaTimeZoneName);
            var tableResult = await table.ExecuteAsync(operation);
            var entity = tableResult.Result as TimeZoneEntity;

            if (entity == null)
            {
                return null;
            }

            return entity.TimeZoneInfoId;
        }

        private CloudTable GetTableReference()
        {
            var account = CloudStorageAccount.Parse(_options.ConnectionString);
            var client = account.CreateCloudTableClient();
            return client.GetTableReference(_options.TimeZoneTableName);
        }
    }
}