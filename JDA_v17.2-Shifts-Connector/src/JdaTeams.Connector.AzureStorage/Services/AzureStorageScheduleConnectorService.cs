using JdaTeams.Connector.AzureStorage.Entities;
using JdaTeams.Connector.AzureStorage.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JdaTeams.Connector.AzureStorage.Services
{
    public class AzureStorageScheduleConnectorService : IScheduleConnectorService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageScheduleConnectorService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<IEnumerable<ConnectionModel>> ListConnectionsAsync()
        {
            var table = GetTableReference(_options.TeamTableName);
            var filter = TableQuery.GenerateFilterCondition(nameof(ConnectionEntity.PartitionKey), QueryComparisons.Equal, ConnectionEntity.DefaultPartitionKey);
            var query = new TableQuery<ConnectionEntity>()
                .Where(filter)
                .Take(_options.TakeCount);
            var tableResult = await table.ExecuteQuerySegmentedAsync(query, null);

            return tableResult.Results
                .Select(t => t.AsModel());
        }

        public async Task<ConnectionModel> GetConnectionAsync(string teamId)
        {
            var table = GetTableReference(_options.TeamTableName);
            var operation = TableOperation.Retrieve<ConnectionEntity>(ConnectionEntity.DefaultPartitionKey, teamId);
            var tableResult = await table.ExecuteAsync(operation);
            var entity = tableResult.Result as ConnectionEntity;

            if (entity == null)
            {
                throw new KeyNotFoundException();
            }

            return entity.AsModel();
        }

        public async Task SaveConnectionAsync(ConnectionModel model)
        {
            var table = GetTableReference(_options.TeamTableName);
            var entity = ConnectionEntity.FromModel(model);
            var operation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(operation);
        }

        public async Task DeleteConnectionAsync(string teamId)
        {
            var table = GetTableReference(_options.TeamTableName);
            var entity = ConnectionEntity.FromId(teamId);
            var operation = TableOperation.Delete(entity);
            await table.ExecuteAsync(operation);
        }

        public async Task<string> GetTimezoneInfoIdAsync(string timezoneName)
        {
            var table = GetTableReference(_options.TimezoneTableName);
            var operation = TableOperation.Retrieve(_options.TimezoneTableName, timezoneName);
            var tableResult = await table.ExecuteAsync(operation);
            return tableResult.Result.ToString();
        }

        private CloudTable GetTableReference(string tableName)
        {
            var account = CloudStorageAccount.Parse(_options.ConnectionString);
            var client = account.CreateCloudTableClient();
            return client.GetTableReference(tableName);
        }
    }
}
