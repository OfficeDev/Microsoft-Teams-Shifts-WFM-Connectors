// <copyright file="ITimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;

    /// <summary>
    /// TimeOff Activity Interface.
    /// </summary>
    public interface ITimeOffActivity
    {
        /// <summary>
        /// Fecth time off request details for displaying history.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">JSession.</param>
        /// <param name="queryDateSpan">QueryDateSpan string.</param>
        /// <param name="employees">Employees who created request.</param>
        /// <returns>Request details response object.</returns>
        Task<Response> GetTimeOffRequestDetailsByBatchAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            List<Models.ResponseEntities.HyperFind.ResponseHyperFindResult> employees);
    }
}