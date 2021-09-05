// ---------------------------------------------------------------------------
// <copyright file="AzureStorageScheduleConnectorService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using WfmTeams.Adapter.AzureStorage.Entities;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class AzureStorageScheduleConnectorService : IScheduleConnectorService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageScheduleConnectorService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task DeleteConnectionAsync(string teamId)
        {
            var table = GetTableReference();
            var entity = ConnectionEntity.FromId(teamId);
            var operation = TableOperation.Delete(entity);
            await table.ExecuteAsync(operation).ConfigureAwait(false);
        }

        public async Task<ConnectionModel> GetConnectionAsync(string teamId)
        {
            var table = GetTableReference();
            var operation = TableOperation.Retrieve<ConnectionEntity>(ConnectionEntity.DefaultPartitionKey, teamId);
            var tableResult = await table.ExecuteAsync(operation).ConfigureAwait(false);
            var entity = tableResult.Result as ConnectionEntity;

            if (entity == null)
            {
                throw new KeyNotFoundException();
            }

            return entity.AsModel();
        }

        public async Task<IEnumerable<ConnectionModel>> ListConnectionsAsync()
        {
            var table = GetTableReference();
            var filter = TableQuery.GenerateFilterCondition(nameof(ConnectionEntity.PartitionKey), QueryComparisons.Equal, ConnectionEntity.DefaultPartitionKey);
            var query = new TableQuery<ConnectionEntity>()
                .Where(filter)
                .Take(_options.TakeCount);
            var tableResult = await table.ExecuteQuerySegmentedAsync(query, null).ConfigureAwait(false);

            return tableResult.Results
                .Select(t => t.AsModel());
        }

        public async Task SaveConnectionAsync(ConnectionModel model)
        {
            var table = GetTableReference();
            var entity = ConnectionEntity.FromModel(model);
            var operation = TableOperation.InsertOrReplace(entity);
            await table.ExecuteAsync(operation).ConfigureAwait(false);
        }

        public async Task UpdateEnabledAsync(string teamId, bool enabled)
        {
            var table = GetTableReference();
            var entity = new DynamicTableEntity(table.Name, teamId)
            {
                ETag = "*"
            };
            entity.Properties.Add("Enabled", new EntityProperty(enabled));

            var mergeOperation = TableOperation.Merge(entity);
            await table.ExecuteAsync(mergeOperation).ConfigureAwait(false);
        }

        public async Task UpdateLastExecutionDatesAsync(ConnectionModel model)
        {
            var table = GetTableReference();
            var entity = new DynamicTableEntity(table.Name, model.TeamId)
            {
                ETag = "*"
            };
            entity.Properties.Add(nameof(model.LastAOExecution), new EntityProperty(model.LastAOExecution));
            entity.Properties.Add(nameof(model.LastECOExecution), new EntityProperty(model.LastECOExecution));
            entity.Properties.Add(nameof(model.LastETROExecution), new EntityProperty(model.LastETROExecution));
            entity.Properties.Add(nameof(model.LastOSOExecution), new EntityProperty(model.LastOSOExecution));
            entity.Properties.Add(nameof(model.LastSOExecution), new EntityProperty(model.LastSOExecution));
            entity.Properties.Add(nameof(model.LastTOOExecution), new EntityProperty(model.LastTOOExecution));

            var mergeOperation = TableOperation.Merge(entity);
            await table.ExecuteAsync(mergeOperation).ConfigureAwait(false);
        }

        private CloudTable GetTableReference()
        {
            var account = CloudStorageAccount.Parse(_options.ConnectionString);
            var client = account.CreateCloudTableClient();
            return client.GetTableReference(_options.TeamTableName);
        }
    }
}
