// <copyright file="ITimeOffRequestProvider.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Providers
{
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.Shifts.Integration.BusinessLogic.Models;

    /// <summary>
    /// This interface defines methods to be implemented with regards to the TimeOff Request Entity.
    /// </summary>
    public interface ITimeOffRequestProvider
    {
        /// <summary>
        /// Method definition to get all the TimeOffReqMappingEntities.
        /// </summary>
        /// <param name="monthPartitionKey">The Month partition key.</param>
        /// <param name="timeOffReqId">The TimeOffReqId.</param>
        /// <returns>A unit of execution containing a list of the time off requests.</returns>
        Task<List<TimeOffMappingEntity>> GetAllTimeOffReqMappingEntitiesAsync(string monthPartitionKey, string timeOffReqId);
    }
}