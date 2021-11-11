// <copyright file="IUserMappingProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// The interface for defining the necessary methods to get the necessary mappings
    /// between a User in Shifts to a User in Kronos.
    /// </summary>
    public interface IUserMappingProvider
    {
        /// <summary>
        /// Function that will return all of the users that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped Users.</returns>
        Task<List<AllUserMappingEntity>> GetAllMappedUserDetailsAsync();

        /// <summary>
        /// Function that will return all of the active Users that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped Users.</returns>
        Task<List<AllUserMappingEntity>> GetAllActiveMappedUserDetailsAsync();

        /// <summary>
        /// Gets a single user entity mapping record.
        /// </summary>
        /// <param name="userAadObjectId">The partition key - UserAadObjectId.</param>
        /// <param name="teamId">The row key - the TeamId.</param>
        /// <returns>A unit of execution that contains the UserMappingEntity.</returns>
        /// To Do
        Task<AllUserMappingEntity> GetUserMappingEntityAsyncNew(
            string userAadObjectId,
            string teamId);

        /// <summary>
        /// Method definition for saving or updating the AllUserMappingEntity.
        /// </summary>
        /// <param name="entity">An object of type <see cref="AllUserMappingEntity"/> which is to be saved or updated.</param>
        /// <returns>A unit of execution.</returns>
        Task SaveOrUpdateUserMappingEntityAsync(AllUserMappingEntity entity);

        /// <summary>
        /// Method to get distinct Kronos org job path.
        /// </summary>
        /// <returns>List of distinct OrgJobPath.</returns>
        Task<List<string>> GetDistinctOrgJobPatAsync();

        /// <summary>
        /// Method to get delete Mapped user details.
        /// </summary>
        /// <param name="partitionKey">Kronos OrgJobPath.</param>
        /// <param name="rowKey">Kronos person number.</param>
        /// <returns>Success if successfully deleted.</returns>
        Task<bool> DeleteMappedUserDetailsAsync(string partitionKey, string rowKey);
    }
}