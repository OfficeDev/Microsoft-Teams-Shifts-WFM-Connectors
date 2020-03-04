// <copyright file="ISwapShiftMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface defines all of the necessary methods that are required for mapping time off between Kronos and Shifts.
    /// </summary>
    public interface ISwapShiftMappingEntityProvider
    {
        /// <summary>
        /// Get all the time off entities for a user.
        /// </summary>
        /// <param name="kronosReqIds">Kronos req ids.</param>
        /// <returns>Mapped time off entities.</returns>
        Task<List<SwapShiftMappingEntity>> GetAllSwapShiftMappingEntitiesAsync(
            List<string> kronosReqIds);

        /// <summary>
        /// Adds an entity to SwapShiftMapping table.
        /// </summary>
        /// <param name="swapShiftMappingEntity">SwapShiftMappingEntity instance.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task AddOrUpdateSwapShiftMappingAsync(
            SwapShiftMappingEntity swapShiftMappingEntity);

        /// <summary>
        /// Method to get the list of Swap Shift Mapping entities based on an Id.
        /// </summary>
        /// <param name="swapShiftRedId">The swap shift request id.</param>
        /// <returns>A unit of execution that contains the List of SwapShiftMappingEntity.</returns>
        Task<SwapShiftMappingEntity> GetKronosReqAsync(string swapShiftRedId);

        /// <summary>
        /// Method to check the existence of a record in Azure table storage.
        /// </summary>
        /// <param name="shiftId">The shiftId to check.</param>
        /// <returns>A unit of execution that contains the boolean value.</returns>
        Task<bool> CheckShiftExistanceAsync(string shiftId);

        /// <summary>
        /// Method to check the existence of a record in Azure table storage.
        /// </summary>
        /// <param name="shiftId">The shiftId to check.</param>
        /// <returns>A unit of execution that contains the boolean value.</returns>
        Task<TeamsShiftMappingEntity> GetShiftDetailsAsync(
            string shiftId);

        /// <summary>
        /// Method to find request is in pending state.
        /// </summary>
        /// <returns>List of swap shift pending requests.</returns>
        Task<List<SwapShiftMappingEntity>> GetPendingRequest();
    }
}