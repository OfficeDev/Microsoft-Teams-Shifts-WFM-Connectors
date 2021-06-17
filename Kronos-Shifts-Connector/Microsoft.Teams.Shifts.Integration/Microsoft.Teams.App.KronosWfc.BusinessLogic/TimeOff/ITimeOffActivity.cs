// <copyright file="ITimeOffActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.TimeOff
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests;
    using TimeOffApproveDeclineResponse = Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests.TimeOffApproveDecline;

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

        /// <summary>
        /// Approves or Denies the given time off request.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <param name="approved">Whether the request is being accepted or denied.</param>
        /// <param name="kronosId">The id of the TimeOffRequest in Kronos.</param>
        /// <returns>A response.</returns>
        Task<TimeOffApproveDeclineResponse.Response> ApproveOrDenyTimeOffRequestAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber,
            bool approved,
            string kronosId);
    }
}