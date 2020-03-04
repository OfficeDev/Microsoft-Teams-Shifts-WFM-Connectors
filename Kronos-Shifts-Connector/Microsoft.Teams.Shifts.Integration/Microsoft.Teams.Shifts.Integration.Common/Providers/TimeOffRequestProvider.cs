// <copyright file="TimeOffRequestProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class implements the methods defined in <see cref="ITimeOffRequestProvider"/>.
    /// </summary>
    public class TimeOffRequestProvider : ITimeOffRequestProvider
    {
        private const string TimeOffTable = "TimeOffMapping";
        private readonly Lazy<Task> initializeTask;
        private readonly TelemetryClient telemetryClient;
        private CloudTable timeoffEntityMappingTable;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimeOffRequestProvider"/> class.
        /// </summary>
        /// <param name="telemetryClient">ApplicationInsights DI.</param>
        /// <param name="connectionString">The Azure table storage connection string.</param>
        public TimeOffRequestProvider(
            TelemetryClient telemetryClient,
            string connectionString)
        {
            this.telemetryClient = telemetryClient;
            this.initializeTask = new Lazy<Task>(() => this.InitializeAsync(connectionString));
        }

        /// <summary>
        /// Method to get the TimeOffReqMappingEntities.
        /// </summary>
        /// <param name="monthPartitionKey">The month partition key.</param>
        /// <param name="timeOffReqId">The TimeOffRequestId.</param>
        /// <returns>A unit of execution that contains a List of <see cref="TimeOffMappingEntity"/>.</returns>
        public async Task<List<TimeOffMappingEntity>> GetAllTimeOffReqMappingEntitiesAsync(string monthPartitionKey, string timeOffReqId)
        {
            await this.EnsureInitializedAsync().ConfigureAwait(false);

            var getEntitiesProps = new Dictionary<string, string>()
            {
                { "CurrentCallingAssembly", Assembly.GetCallingAssembly().GetName().Name },
            };

            this.telemetryClient.TrackTrace(MethodBase.GetCurrentMethod().Name, getEntitiesProps);

            // Table query
            TableQuery<TimeOffMappingEntity> query = new TableQuery<TimeOffMappingEntity>();
            query.Where(TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.Equal, monthPartitionKey), TableOperators.And, TableQuery.CombineFilters(TableQuery.GenerateFilterCondition("ShiftsRequestId", QueryComparisons.Equal, timeOffReqId), TableOperators.And, TableQuery.GenerateFilterConditionForBool("IsActive", QueryComparisons.Equal, true))));

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