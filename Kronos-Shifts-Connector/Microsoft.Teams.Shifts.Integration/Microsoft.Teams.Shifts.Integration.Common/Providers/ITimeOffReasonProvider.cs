// <copyright file="ITimeOffReasonProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface defines methods to be implemented with regards to the TimeOff Reason Entity.
    /// </summary>
    public interface ITimeOffReasonProvider
    {
        /// <summary>
        /// Function that will return all of the configurations that are registered in Azure Table storage.
        /// </summary>
        /// <returns>A list of the configurations established.</returns>
        Task<List<PayCodeToTimeOffReasonsMappingEntity>> GetTimeOffReasonsAsync();

        /// <summary>
        /// Function that will return all the reasons of a team.
        /// </summary>
        /// <param name="teamsId">Shift teams id.</param>
        /// <param name="tenantId">tenant id.</param>
        /// <returns>time off reasons.</returns>
        Task<Dictionary<string, string>> FetchReasonsForTeamsAsync(string teamsId, string tenantId);

        /// <summary>
        /// Function that will return the mapping for a given reason name.
        /// </summary>
        /// <param name="reason">The reason you are looking for.</param>
        /// <returns>The mapping for the reason.</returns>
        Task<PayCodeToTimeOffReasonsMappingEntity> FetchReasonAsync(string reason);

        /// <summary>
        /// Delete all mappings except for ones with a given name.
        /// </summary>
        /// <param name="reasonsToKeep">The reasons name for the mappings you want to keep.</param>
        Task DeleteSpecificReasons(params string[] reasonsToKeep);
    }
}