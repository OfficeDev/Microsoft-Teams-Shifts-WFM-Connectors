// <copyright file="RedisTokenCache.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Cache
{
    using System;
    using Microsoft.Extensions.Caching.Distributed;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Token cache implementing Redis.
    /// </summary>
    public class RedisTokenCache : TokenCache
    {
        private readonly IDistributedCache cache;
        private readonly string clientId;

        /// <summary>
        /// Initializes a new instance of the <see cref="RedisTokenCache"/> class.
        /// </summary>
        /// <param name="cache">Distributed cache.</param>
        /// <param name="clientId">ClientId Id.</param>
        public RedisTokenCache(IDistributedCache cache, string clientId)
        {
            this.cache = cache;
            this.clientId = clientId;
            this.BeforeAccess = this.BeforeAccessNotification;
            this.AfterAccess = this.AfterAccessNotification;
        }

        /// <summary>
        /// Get the key of the cache.
        /// </summary>
        /// <returns>Cache key.</returns>
        private string GetCacheKey()
        {
            return $"{this.clientId}_TokenCache";
        }

        /// <summary>
        /// Notification raised before ADAL accesses the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the ADAL call accessing the cache.</param>
        private void BeforeAccessNotification(TokenCacheNotificationArgs args)
        {
            this.DeserializeAdalV3(this.cache.Get(this.GetCacheKey()));
        }

        /// <summary>
        /// Notification raised after ADAL accessed the cache.
        /// </summary>
        /// <param name="args">Contains parameters used by the ADAL call accessing the cache.</param>
        private void AfterAccessNotification(TokenCacheNotificationArgs args)
        {
            if (this.HasStateChanged)
            {
                this.cache.Set(this.GetCacheKey(), this.SerializeAdalV3(), new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(90),
                    SlidingExpiration = TimeSpan.FromDays(7),
                });
                this.HasStateChanged = false;
            }
        }
    }
}