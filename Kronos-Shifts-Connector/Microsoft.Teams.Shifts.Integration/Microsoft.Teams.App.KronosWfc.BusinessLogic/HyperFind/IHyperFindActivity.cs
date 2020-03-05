// <copyright file="IHyperFindActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFind
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFind;

    /// <summary>
    /// Hyper Find Activity Interface.
    /// </summary>
    public interface IHyperFindActivity
    {
        /// <summary>
        /// Returns all the home employees.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="tenantId">The TenantId.</param>
        /// <param name="jSession">The jSession string.</param>
        /// <param name="startDate">The startDate.</param>
        /// <param name="endDate">The endDate.</param>
        /// <param name="hyperFindQueryName">The name of the hyper find query.</param>
        /// <param name="visibilityCode">The visibility code.</param>
        /// <returns>A unit of execution that contains the type <see cref="Response"/>.</returns>
        Task<Response> GetHyperFindQueryValuesAsync(
            Uri endPointUrl,
            string tenantId,
            string jSession,
            string startDate,
            string endDate,
            string hyperFindQueryName,
            string visibilityCode);
    }
}