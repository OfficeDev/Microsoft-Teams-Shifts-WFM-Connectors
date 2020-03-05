// <copyright file="IJobAssignmentActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.JobAssignment
{
    using System;
    using System.Threading.Tasks;

    /// <summary>
    /// Job assignment activity interface.
    /// </summary>
    public interface IJobAssignmentActivity
    {
        /// <summary>
        /// Get Job Assignments.
        /// </summary>
        /// <param name="endPointUrl">End Point Url.</param>
        /// <param name="personNumber">Person Number.</param>
        /// <param name="tenantId">Tenant ID.</param>
        /// <param name="jSession">J Session.</param>
        /// <returns>Job Assignment response.</returns>
        Task<Models.ResponseEntities.JobAssignment.Response> GetJobAssignmentAsync(
            Uri endPointUrl,
            string personNumber,
            string tenantId,
            string jSession);
    }
}