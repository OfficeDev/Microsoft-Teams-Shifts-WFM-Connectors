// ---------------------------------------------------------------------------
// <copyright file="InMemoryCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Extensions;

    public class InMemoryCacheService : ICacheService
    {
        private ConcurrentDictionary<string, object> _cache = new ConcurrentDictionary<string, object>();

        public Task DeleteKeyAsync(string tableName, string id)
        {
            string cacheId = GetCacheId(tableName, id);
            _cache.TryRemove(cacheId, out object value);

            return Task.CompletedTask;
        }

        public Task<T> GetKeyAsync<T>(string tableName, string id)
        {
            string cacheId = GetCacheId(tableName, id);

            return Task.FromResult((T)_cache.ReplGetValueOrDefault(cacheId));
        }

        public Task SetKeyAsync<T>(string tableName, string id, T value, bool shortExpiry = false)
        {
            string cacheId = GetCacheId(tableName, id);
            _cache[cacheId] = value;

            return Task.CompletedTask;
        }

        private string GetCacheId(string tableName, string id)
        {
            return $"{tableName}_{id}";
        }
    }
}
