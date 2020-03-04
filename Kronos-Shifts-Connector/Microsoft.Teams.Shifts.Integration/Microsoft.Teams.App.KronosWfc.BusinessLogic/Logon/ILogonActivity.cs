// <copyright file="ILogonActivity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.BusinessLogic.Logon
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Logon;

    /// <summary>
    /// Logon Activity Interface.
    /// </summary>
    public interface ILogonActivity
    {
        /// <summary>
        /// Logon method.
        /// </summary>
        /// <param name="username">User details.</param>
        /// <param name="password">The user password.</param>
        /// <param name="endPointUrl">The Kronos Endpoint URL.</param>
        /// <returns>User Response.</returns>
        // Task<Response> Logon(User user, string endPointUrl);
        Task<Response> LogonAsync(string username, string password, Uri endPointUrl);
    }
}