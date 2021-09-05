// ---------------------------------------------------------------------------
// <copyright file="AzureRedisOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureRedis.Options
{
    public class AzureRedisOptions
    {
        public int CacheItemExpiryMinutes { get; set; } = 150;
        public int CacheItemShortExpiryMinutes { get; set; } = 10;
        public string RedisCacheConnectionString { get; set; }
    }
}