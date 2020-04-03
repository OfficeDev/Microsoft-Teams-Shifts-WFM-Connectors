// <copyright file="IPayCodeActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.PayCodes
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    /// <summary>
    /// This interface used to fetch paycodes from Kronos.
    /// </summary>
    public interface IPayCodeActivity
    {
        /// <summary>
        /// Fetch paycodes mapped.
        /// </summary>
        /// <param name="endPointUrl">Kronos endpoint url.</param>
        /// <param name="jSession">Kronos session.</param>
        /// <returns>List of Kronos paycodes.</returns>
        Task<List<string>> FetchPayCodesAsync(
            Uri endPointUrl,
            string jSession);
    }
}