// <copyright file="ITokenCacheFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Services
{
    using System.Security.Claims;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// The Interface for TokenCacheFactory.
    /// </summary>
    public interface ITokenCacheFactory
    {
        /// <summary>
        /// Interface method to generate Cache Token.
        /// </summary>
        /// <returns>Returns cache token for the user.</returns>
        /// <param name="user">Claims principal for signed in user.</param>
        TokenCache CreateForUser(ClaimsPrincipal user);
    }
}