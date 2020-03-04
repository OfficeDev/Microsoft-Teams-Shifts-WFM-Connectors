// <copyright file="TokenCacheFactory.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Services
{
    using System;
    using System.Security.Claims;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;
    using Microsoft.Teams.Shifts.Integration.Configuration.Extensions;

    /// <summary>
    /// The Class for Cache Token.
    /// </summary>
    public class TokenCacheFactory : ITokenCacheFactory
    {
        private readonly IDistributedCache distributedCache;
        private readonly IDataProtectionProvider dataProtectionProvider;

        // Token cache is cached in-memory in this instance to avoid loading data multiple times during the request
        // For this reason this factory should always be registered as Scoped
        private TokenCache cachedTokenCache;
        private string cachedTokenCacheUserId;

        /// <summary>
        /// Initializes a new instance of the <see cref="TokenCacheFactory"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed Cache.</param>
        /// <param name="dataProtectionProvider">Data protection provider.</param>
        public TokenCacheFactory(IDistributedCache distributedCache, IDataProtectionProvider dataProtectionProvider)
        {
            this.distributedCache = distributedCache;
            this.dataProtectionProvider = dataProtectionProvider;
        }

        /// <summary>
        /// Create cache token for user.
        /// </summary>
        /// <returns>The cache token for user.</returns>
        /// <param name="user">Claims principal for signed in user.</param>
        public TokenCache CreateForUser(ClaimsPrincipal user)
        {
            string userId = user.GetObjectId();

            if (this.cachedTokenCache != null)
            {
                // Guard for accidental re-use across requests
                if (userId != this.cachedTokenCacheUserId)
                {
                    throw new Exception(Resources.TokenCacheReuseExceptionMessage);
                }

                return this.cachedTokenCache;
            }

            this.cachedTokenCache = new AdalDistributedTokenCache(
                this.distributedCache, this.dataProtectionProvider, userId);
            this.cachedTokenCacheUserId = userId;
            return this.cachedTokenCache;
        }
    }
}