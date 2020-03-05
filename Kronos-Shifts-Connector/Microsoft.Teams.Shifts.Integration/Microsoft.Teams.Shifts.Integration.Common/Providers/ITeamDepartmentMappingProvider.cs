// <copyright file="ITeamDepartmentMappingProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Graph;
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
        /// <returns>A unit of execution that boxes a <see cref="ShiftsTeamDepartmentMappingEntity"/>.</returns>
        Task<TeamToDepartmentJobMappingEntity> GetTeamMappingForOrgJobPathAsync(string workForceIntegrationId, string orgJobPath);

        /// <summary>
        /// Saving or updating a mapping between a Team in Shifts and Department in Kronos.
        /// </summary>
        /// <param name="shiftsTeamsDetails">Shifts team details fetched via Graph api calls.</param>
        /// <param name="kronosDepartmentName">Department name fetched from Kronos.</param>
        /// <param name="workforceIntegrationId">The Workforce Integration Id.</param>
        /// <param name="tenantId">Tenant Id.</param>
        /// <returns><see cref="Task"/> that resolves successfully if the data was saved successfully.</returns>
        Task<bool> SaveOrUpdateShiftsTeamDepartmentMappingAsync(
            Team shiftsTeamsDetails,
            string kronosDepartmentName,
            string workforceIntegrationId,
            string tenantId);

        /// <summary>
        /// Function that will return all of the teams that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>A list of the mapped teams.</returns>
        Task<List<ShiftsTeamDepartmentMappingEntity>> GetMappedTeamDetailsAsync();

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
        Task<bool> TeamsToDepartmentMappingAsync(TeamsDepartmentMappingModel entity);

        /// <summary>
        /// Function that will return all of the teams and department that are mapped in Azure Table storage.
        /// </summary>
        /// <returns>List of Team and Department mapping model.</returns>
        Task<List<TeamsDepartmentMappingModel>> GetTeamDeptMappingDetailsAsync();

        /// <summary>
        /// Method to delete teams and Department mapping.
        /// </summary>
        /// <param name="partitionKey">The partition key.</param>
        /// <param name="rowKey">The row key.</param>
        /// <returns>boolean value that indicates delete success.</returns>
        Task<bool> DeleteMappedTeamDeptDetailsAsync(string partitionKey, string rowKey);
    }
}