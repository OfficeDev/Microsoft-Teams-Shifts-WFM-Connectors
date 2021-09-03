// <copyright file="ITeamDepartmentMappingProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// The interface for defining the necessary methods to get the necessary mappings
    /// between a Team in Shifts and a Department in Kronos.
    /// </summary>
    public interface ITeamDepartmentMappingProvider
    {
        /// <summary>
        /// Retrieves a single TeamDepartmentMapping from Azure Table storage.
        /// </summary>
        /// <param name="workForceIntegrationId">WorkForceIntegration Id.</param>
        /// <param name="orgJobPath">Kronos Org Job Path.</param>
        /// <returns>A unit of execution that returns a <see cref="TeamToDepartmentJobMappingEntity"/>.</returns>
        Task<TeamToDepartmentJobMappingEntity> GetTeamMappingForOrgJobPathAsync(string workForceIntegrationId, string orgJobPath);

        /// <summary>
        /// Function that will return all of the teams that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped teams.</returns>
        Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamDetailsAsync();

        /// <summary>
        /// Function that will return all the mappings for a single team that are mapped in Azure Table storage.
        /// </summary>
        /// <param name="teamId">The ID of the team to get the mappings for.</param>
        /// <returns>The mappings for the team.</returns>
        Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamDetailsAsync(string teamId);

        /// <summary>
        /// This method definition will be getting all of the team to department mappings
        /// that have OrgJobPaths.
        /// </summary>
        /// <returns>A list of TeamToDepartmentJobMappingEntity.</returns>
        Task<List<TeamToDepartmentJobMappingEntity>> GetMappedTeamToDeptsWithJobPathsAsync();

        /// <summary>
        /// This method will get all of the OrgJobPaths at one shot.
        /// </summary>
        /// <returns>A list of string records contained in a unit of execution.</returns>
        Task<List<string>> GetAllOrgJobPathsAsync();

        /// <summary>
        /// Method to save or update Teams to Department mapping.
        /// </summary>
        /// <param name="entity">The entity to delete.</param>
        /// <returns>http status code representing the asynchronous operation.</returns>
        Task<bool> SaveOrUpdateTeamsToDepartmentMappingAsync(TeamToDepartmentJobMappingEntity entity);

        /// <summary>
        /// Method to delete teams and Department mapping.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>boolean value that indicates delete success.</returns>
        Task<bool> DeleteMappedTeamDeptDetailsAsync(string partitionKey, string rowKey);
    }
}