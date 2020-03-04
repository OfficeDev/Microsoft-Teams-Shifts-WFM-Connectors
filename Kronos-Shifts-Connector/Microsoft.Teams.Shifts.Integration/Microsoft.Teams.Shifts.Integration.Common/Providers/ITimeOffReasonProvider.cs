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
    }
}