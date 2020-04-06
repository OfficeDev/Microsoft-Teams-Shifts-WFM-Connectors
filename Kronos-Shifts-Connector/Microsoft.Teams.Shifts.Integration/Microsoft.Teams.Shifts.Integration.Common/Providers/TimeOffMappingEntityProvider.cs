// <copyright file="TimeOffMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements methods that are defined in <see cref="ITimeOffMappingEntityProvider"/>.
    /// </summary>
    public class TimeOffMappingEntityProvider : ITimeOffMappingEntityProvider
    {
        private const string TimeOffTable = "TimeOffMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable timeoffEntityMappingTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffMappingEntityProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        public TimeOffMappingEntityProvider(
            TelemetryClient telemetryClient,
            string connectionString)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
        }

        /// <summary>
        /// Method to get all the time off mapping entities.
        /// </summary>
        /// <param name="processKronosUsersInBatchList">The batch of Kronos users.</param>
        /// <param name="monthPartitionKey">The month partition key.</param>
        /// <returns>A list of time off mapping entities that are contained in a unit of execution.</returns>
        public async Task<List<TimeOffMappingEntity>> GetAllTimeOffMappingEntitiesAsync(
          IEnumerable<UserDetailsModel> processKronosUsersInBatchList,
          string monthPartitionKey)
        {
            if (processKronosUsersInBatchList is null)
            {
                throw new ArgumentNullException(nameof(processKronosUsersInBatchList));
            }

            var allTimeOffMappingEntitiesInBatch = new List<TimeOffMappingEntity>();
            var task = processKronosUsersInBatchList.Select(async item =>
            {
                var response = await this.GetAllTimeOffMappingEntitiesAsync(item, monthPartitionKey).ConfigureAwait(false);
                allTimeOffMappingEntitiesInBatch.AddRange(response);
            });

            await Task.WhenAll(task).ConfigureAwait(false);
            var count = allTimeOffMappingEntitiesInBatch.Count;

            return allTimeOffMappingEntitiesInBatch;
        }

        /// <summary>
        /// Get all the time off mapping entities.
        /// </summary>
        /// <param name="userModel">User in batch.</param>
        /// <param name="monthPartitionKey">Month Partition.</param>
        /// <returns>List of time Off mapping entities.</returns>
        private async Task<List<TimeOffMappingEntity>> GetAllTimeOffMappingEntitiesAsync(
            UserDetailsModel userModel,
            string monthPartitionKey)
        {
            if (userModel is null)
            {
                throw new ArgumentNullException(nameof(userModel));
            }

            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            string userFilter = TableQuery.GenerateFilterCondition("KronosPersonNumber", QueryComparisons.Equal, userModel?.KronosPersonNumber);
            string monthPartitionFilter = TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, monthPartitionKey);
            string isActive = TableQuery.GenerateFilterConditionForBool("IsActive", QueryComparisons.Equal, true);

            // Table query
            TableQuery<TimeOffMappingEntity> query = new TableQuery<TimeOffMappingEntity>();
            query.Where(TableQuery.CombineFilters(monthPartitionFilter, TableOperators.And, TableQuery.CombineFilters(userFilter, TableOperators.And, isActive)));

            // Results list
            List<TimeOffMappingEntity> results = new List<TimeOffMappingEntity>();
            TableContinuationToken continuationToken = null;
            if (await this.timeoffEntityMappingTable.ExistsAsync().ConfigureAwait(false))
            {
                do
                {
                    TableQuerySegment<TimeOffMappingEntity> queryResults = await this.timeoffEntityMappingTable.ExecuteQuerySegmentedAsync(query, continuationToken).ConfigureAwait(false);
                    continuationToken = queryResults.ContinuationToken;
                    results.AddRange(queryResults.Results);
                }
                while (continuationToken != null);
            }

            return results;
        }

        private async Task EnsureInitializedAsync()
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            await this.initializeTask.Value.ConfigureAwait(false);
        }

        private async Task InitializeAsync(string connectionString)
        {
            this.telemetryClient.TrackTrace($"{MethodBase.GetCurrentMethod().Name}");
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            this.timeoffEntityMappingTable = cloudTableClient.GetTableReference(TimeOffTable);
            await this.timeoffEntityMappingTable.CreateIfNotExistsAsync().ConfigureAwait(false);
        }
    }
}