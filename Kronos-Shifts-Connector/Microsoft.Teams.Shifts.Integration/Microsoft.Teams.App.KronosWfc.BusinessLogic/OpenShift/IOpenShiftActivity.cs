// <copyright file="IOpenShiftActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.OpenShift
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest;

    /// <summary>
    /// OpenShift Activity interface.
    /// </summary>
    public interface IOpenShiftActivity
    {
        /// <summary>
        /// Fetch open shifts from Kronos in a batch manner.
        /// </summary>
        /// <param name="endpointUrl">The Kronos WFC API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="orgJobPathsBatchList">The list of Org Job Paths.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <returns>A response object.</returns>
        Task<Models.ResponseEntities.OpenShift.Batch.Response> GetOpenShiftDetailsInBatchAsync(
            Uri endpointUrl,
            string jSession,
            List<string> orgJobPathsBatchList,
            string queryDateSpan);

        /// <summary>
        /// Method to create the DraftOpenShift.
        /// </summary>
        /// <param name="tenantId">The TenantId.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="openShiftObject">The OpenShiftObj.</param>
        /// <param name="endPointUrl">The Kronos WFC API.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.Response> PostDraftOpenShiftRequestAsync(
            string tenantId,
            string jSession,
            OpenShiftObj openShiftObject,
            Uri endPointUrl);

        /// <summary>
        /// Method that will return the response post updating the status for an open shift request.
        /// </summary>
        /// <param name="personNumber">The Kronos Person Number.</param>
        /// <param name="requestId">The request ID.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="comment">The comment.</param>
        /// <param name="endpointUrl">The Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <returns>A unit of execution that contains the response.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.Response> PostOpenShiftRequestStatusUpdateAsync(
            string personNumber,
            string requestId,
            string queryDateSpan,
            string comment,
            Uri endpointUrl,
            string jSession);

        /// <summary>
        /// Method definition to get approved or denied open shift requests per user.
        /// </summary>
        /// <param name="endPointUrl">Kronos API Endpoint.</param>
        /// <param name="jSession">The jSession.</param>
        /// <param name="queryDateSpan">The query date span.</param>
        /// <param name="kronosPersonNumber">The Kronos person number.</param>
        /// <returns>A unit of execution.</returns>
        Task<Models.ResponseEntities.OpenShiftRequest.ApproveDecline.Response> GetApprovedOrDeclinedOpenShiftRequestsForUserAsync(
            Uri endPointUrl,
            string jSession,
            string queryDateSpan,
            string kronosPersonNumber);
    }
}