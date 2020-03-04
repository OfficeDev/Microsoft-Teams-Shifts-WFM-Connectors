// <copyright file="AdalDistributedTokenCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Services
{
    using System;
    using Microsoft.AspNetCore.DataProtection;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Caches access and refresh tokens for Azure AD.
    /// </summary>
    public class AdalDistributedTokenCache : TokenCache
    {
        private readonly IDistributedCache distributedCache;
        private readonly IDataProtector dataProtector;
        private readonly string userId;

        /// <summary>
        /// Initializes a new instance of the <see cref="AdalDistributedTokenCache"/> class.
        /// </summary>
        /// <param name="distributedCache">Distributed cache used for storing tokens.</param>
        /// <param name="dataProtectionProvider">The protector provider for encrypting/decrypting the cached data.</param>
        /// <param name="userId">The user's unique identifier.</param>
        public AdalDistributedTokenCache(
            IDistributedCache distributedCache,
            IDataProtectionProvider dataProtectionProvider,
            string userId)
        {
            if (dataProtectionProvider is null)
            {
                throw new ArgumentNullException(nameof(dataProtectionProvider));
            }

            this.distributedCache = distributedCache;
            this.dataProtector = dataProtectionProvider.CreateProtector("AadTokens");
            this.userId = userId;
            this.BeforeAccess = this.BeforeAccessNotification;
            this.AfterAccess = this.AfterAccessNotification;
        }

        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            // Called before ADAL tries to access the cache,
            // so this is where we should read from the distibruted cache
            // A blocking call must be made since the ADAL API calls are synchronous
            byte[] cachedData = this.distributedCache.Get(this.GetCacheKey());

            if (cachedData != null)
            {
                // Decrypt and deserialize the cached data
                this.DeserializeAdalV3(this.dataProtector.Unprotect(cachedData));
            }
            else
            {
                // Ensures the cache is cleared in TokenCache
                this.DeserializeAdalV3(null);
            }
        }

        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            // Called after ADAL is done accessing the token cache
            if (this.HasStateChanged)
            {
                // In this case the cache state has changed, maybe a new token was written
                // So we encrypt and write the data to the distributed cache
                var data = this.dataProtector.Protect(this.SerializeAdalV3());

                this.distributedCache.Set(this.GetCacheKey(), data, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
                });

                this.HasStateChanged = false;
            }
        }

        private string GetCacheKey() => $"{this.userId}_TokenCache";
    }
}