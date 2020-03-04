// <copyright file="ClaimsPrincipalExtensions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Extensions
{
    using System;
    using System.Security.Claims;

    /// <summary>
    /// The Extension method to extract object identifier.
    /// </summary>
    public static class ClaimsPrincipalExtensions
    {
        private const string ObjectIdentifierType = "http://schemas.microsoft.com/identity/claims/objectidentifier";
        private const string TenantId = "http://schemas.microsoft.com/identity/claims/tenantid";

        /// <summary>
        /// Gets the user's Azure AD object id.
        /// </summary>
        /// <returns>Returns object identifier of the user.</returns>
        /// <param name="principal">Claims principal for signed in user.</param>
        public static string GetObjectId(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            return principal.FindFirstValue(ObjectIdentifierType);
        }

        /// <summary>
        /// Gets the user's Azure AD tenant id.
        /// </summary>
        /// <returns>Returns tenant id of the user.</returns>
        /// <param name="principal">Claims principal for signed in user.</param>
        public static string GetTenantId(this ClaimsPrincipal principal)
        {
            if (principal == null)
            {
                throw new ArgumentNullException(nameof(principal));
            }

            return principal.FindFirstValue(TenantId);
        }
    }
}
