// <copyright file="IOpenShiftRequestMappingEntityProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This file contains the necessary definitions of the methods to be implemented.
    /// </summary>
    public interface IOpenShiftRequestMappingEntityProvider
    {
        /// <summary>
        /// Method definition for saving or updating the OpenShiftRequestMappingEntity.
        /// </summary>
        /// <param name="entity">An object of type <see cref="OpenShiftRequestMappingEntity"/> which is to be saved or updated.</param>
        /// <returns>A unit of execution.</returns>
        Task SaveOrUpdateOpenShiftRequestMappingEntityAsync(AllOpenShiftRequestMappingEntity entity);

        /// <summary>
        /// Obtaining the necessary data by the Kronos Request Id.
        /// </summary>
        /// <param name="kronosReqId">The Kronos Request Id.</param>
        /// <returns>A unit of execution that contains the new open shift entity.</returns>
        Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByKronosReqIdAsync(
            string kronosReqId);

        /// <summary>
        /// Obtaining the open shift request mapping entity by the rowkey.
        /// </summary>
        /// <param name="shiftsOpenShiftRequestId">The Open Shift Request Id.</param>
        /// <returns>A unit of execution that contains the Open Shift Request Mapping entity.</returns>
        Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByRowKeyAsync(
            string shiftsOpenShiftRequestId);

        /// <summary>
        /// Method definition to check the existence of an open shift in the Open Shift Request table.
        /// </summary>
        /// <param name="teamsOpenShiftId">The Teams Open Shift ID.</param>
        /// <returns>A unit of execution that contains a boolean value.</returns>
        Task<bool> CheckOpenShiftRequestExistance(string teamsOpenShiftId);

        /// <summary>
        /// Method to get the open shift request by the open shift ID.
        /// </summary>
        /// <param name="openShiftId">The open shift ID.</param>
        /// <param name="openShiftReqId">The open shift request id.</param>
        /// <returns>A unit of execution that contains the Open Shift Request mapping entity.</returns>
        Task<AllOpenShiftRequestMappingEntity> GetOpenShiftRequestMappingEntityByOpenShiftRequestIdAsync(string openShiftId, string openShiftReqId);
    }
}