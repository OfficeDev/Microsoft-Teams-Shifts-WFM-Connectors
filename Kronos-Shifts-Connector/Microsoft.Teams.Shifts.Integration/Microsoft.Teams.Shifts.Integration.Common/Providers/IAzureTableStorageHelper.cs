// <copyright file="IAzureTableStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This interface defines the necessary methods for the Azure table storage.
    /// </summary>
    public interface IAzureTableStorageHelper
    {
        /// <summary>
        /// This will insert or merge table entities.
        /// </summary>
        /// <typeparam name="T">The entity.</typeparam>
        /// <param name="entity">Entity to merge/insert.</param>
        /// <param name="tableName">The table name.</param>
        /// <returns>A unit of execution.</returns>
        Task<T> InsertOrMergeTableEntityAsync<T>(T entity, string tableName)
            where T : TableEntity;

        /// <summary>
        /// This method will fetch records from an Azure table.
        /// </summary>
        /// <typeparam name="T">A generic type.</typeparam>
        /// <param name="tableName">The name of the Azure table.</param>
        /// <param name="partitionKey">The partition key of that table.</param>
        /// <returns>A list of type T that is boxed in a unit of execution.</returns>
        Task<List<T>> FetchTableRecordsAsync<T>(string tableName, string partitionKey)
            where T : ITableEntity, new();
    }
}