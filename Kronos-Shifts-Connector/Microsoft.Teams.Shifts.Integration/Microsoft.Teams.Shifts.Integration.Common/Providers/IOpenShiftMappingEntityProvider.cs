// <copyright file="IOpenShiftMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface defines all of the necessary methods that are required for mapping open shifts between Kronos and Shifts.
    /// </summary>
    public interface IOpenShiftMappingEntityProvider
    {
        /// <summary>
        /// Method definition that saves or updates the OpenShiftMappingEntity in Azure table storage.
        /// </summary>
        /// <param name="entity">The OpenShiftMappingEntity to save or update.</param>
        /// <returns>A unit of execution.</returns>
        Task SaveOrUpdateOpenShiftMappingEntityAsync(AllOpenShiftMappingEntity entity);

        /// <summary>
        /// Method to delete the orphan data from open shifts mapping entity.
        /// </summary>
        /// <param name="entity">The mapping entity to be deleted.</param>
        /// <returns>A unit of execution.</returns>
        Task DeleteOrphanDataFromOpenShiftMappingAsync(AllOpenShiftMappingEntity entity);

        /// <summary>
        /// Method implementation to be able to return matched entry for the OpenShiftEntityMapping table.
        /// </summary>
        /// <param name="openShiftId">The open shift Id.</param>
        /// <returns>Entity associated with the open shift Id.</returns>
        Task<List<AllOpenShiftMappingEntity>> GetOpenShiftMappingEntitiesAsync(string openShiftId);

        /// <summary>
        /// The method definition to be getting entities in a batch manner.
        /// </summary>
        /// <param name="monthPartitionKey">The partition key to search.</param>
        /// <param name="schedulingGroupId">The scheduling group ID for the open shifts.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>A unit of execution that contains a list of <see cref="AllOpenShiftMappingEntity"/>.</returns>
        Task<List<AllOpenShiftMappingEntity>> GetAllOpenShiftMappingEntitiesInBatch(
            string monthPartitionKey,
            string schedulingGroupId,
            string queryStartDate,
            string queryEndDate);

        /// <summary>
        /// Method definition to delete an open shift mapping entity by the Open Shift ID.
        /// </summary>
        /// <param name="openShiftId">The open shift ID of the record to delete.</param>
        /// <returns>A unit of execution.</returns>
        Task DeleteOrphanDataFromOpenShiftMappingByOpenShiftIdAsync(string openShiftId);
    }
}