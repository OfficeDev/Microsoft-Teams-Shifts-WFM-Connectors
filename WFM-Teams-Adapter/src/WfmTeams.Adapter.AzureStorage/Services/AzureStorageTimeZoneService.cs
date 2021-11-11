// ---------------------------------------------------------------------------
// <copyright file="AzureStorageTimeZoneService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure.Cosmos.Table;
    using WfmTeams.Adapter.AzureStorage.Entities;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Services;

    public class AzureStorageTimeZoneService : ITimeZoneService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageTimeZoneService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<string> GetTimeZoneInfoIdAsync(string wfmTimeZoneName)
        {
            var table = GetTableReference();
            var operation = TableOperation.Retrieve<TimeZoneEntity>(TimeZoneEntity.DefaultPartitionKey, wfmTimeZoneName);
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
