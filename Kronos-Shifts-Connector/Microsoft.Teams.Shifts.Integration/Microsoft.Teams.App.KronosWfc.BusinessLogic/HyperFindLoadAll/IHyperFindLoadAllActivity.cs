// <copyright file="IHyperFindLoadAllActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.HyperFindLoadAll
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.HyperFindLoadAll;

    /// <summary>
    /// Hyper Find Load All Activity interface.
    /// </summary>
    public interface IHyperFindLoadAllActivity
    {
        /// <summary>
        /// Returns all the home employees.
        /// </summary>
        /// <param name="endPointUrl">The Kronos WFC endpoint URL.</param>
        /// <param name="jSession">The jSession string.</param>
        /// <returns>A unit of execution that contains the type <see cref="Response"/>.</returns>
        Task<Response> GetHyperFindQueryValuesAsync(Uri endPointUrl, string jSession);
    }
}