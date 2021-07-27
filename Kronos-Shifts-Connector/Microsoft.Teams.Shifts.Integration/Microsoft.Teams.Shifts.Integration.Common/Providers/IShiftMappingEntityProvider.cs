// <copyright file="IShiftMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface defines the necessary methods with regards to the ShiftMappingEntity.
    /// </summary>
    public interface IShiftMappingEntityProvider
    {
        /// <summary>
        /// Method definition to save or update the ShiftMappingEntity data in Azure table storage.
        /// </summary>
        /// <param name="entity">The ShiftMappingEntity to save or update.</param>
        /// <param name="shiftId">The ShiftID to save or update.</param>
        /// <param name="monthPartitionKey">Month Partition Value.</param>
        /// <returns>A unit of execution.</returns>
        Task SaveOrUpdateShiftMappingEntityAsync(TeamsShiftMappingEntity entity, string shiftId, string monthPartitionKey);

        /// <summary>
        /// Method to delete the orphan data from shifts mapping entity.
        /// </summary>
        /// <param name="entity">The mapping entity to be deleted.</param>
        /// <returns>A unit of execution.</returns>
        Task DeleteOrphanDataFromShiftMappingAsync(TeamsShiftMappingEntity entity);

        /// <summary>
        /// Get all shift data from lookup table for the current batch.
        /// </summary>
        /// <param name="processKronosUsersInBatchList">Model to store list of user information.</param>
        /// <param name="monthPartitionKey">Month Partition Value.</param>
        /// <param name="queryStartDate">Query start date.</param>
        /// <param name="queryEndDate">Query end date.</param>
        /// <returns>ShiftMappingEntity.</returns>
        Task<List<TeamsShiftMappingEntity>> GetAllShiftMappingEntitiesInBatchAsync(
            IEnumerable<UserDetailsModel> processKronosUsersInBatchList,
            string monthPartitionKey,
            string queryStartDate,
            string queryEndDate);

        /// <summary>
        /// Method that would be able to return based on the PartitionKey, hash, and Shift "ID".
        /// </summary>
        /// <param name="monthPartition">The month partition.</param>
        /// <param name="aadUserId">The Row Key for the shift entity mapping.</param>
        /// <returns>List of TeamsShiftMapping.</returns>
        Task<List<TeamsShiftMappingEntity>> GetAllUsersShiftsByPartitionKeyAsync(string monthPartition, string aadUserId);

        /// <summary>
        /// Method that retrieves a temporary shift.
        /// </summary>
        /// <param name="tempShiftRowKey">The temporary shift row key.</param>
        /// <returns>A unit of execution that contains the TeamsShiftMappingEntity - represents the temp entry.</returns>
        Task<TeamsShiftMappingEntity> GetShiftMappingEntityByRowKeyAsync(
            string tempShiftRowKey);
    }
}