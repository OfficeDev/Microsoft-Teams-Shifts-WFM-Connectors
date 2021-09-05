// ---------------------------------------------------------------------------
// <copyright file="AzureRedisCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureRedis.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using StackExchange.Redis;
    using WfmTeams.Adapter.AzureRedis.Options;
    using WfmTeams.Adapter.Services;

    public class AzureRedisCacheService : ICacheService, IDisposable
    {
        private readonly Lazy<ConnectionMultiplexer> _lazyConnection;

        private readonly ILogger<AzureRedisCacheService> _log;

        private readonly AzureRedisOptions _options;

        private bool disposedValue = false;

        public AzureRedisCacheService(AzureRedisOptions options, ILogger<AzureRedisCacheService> log)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            _lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
            {
                var cacheConnectionString = _options.RedisCacheConnectionString;
                return ConnectionMultiplexer.Connect(cacheConnectionString);
            });
        }

        public async Task DeleteKeyAsync(string tableName, string id)
        {
            var cacheId = GetCacheId(tableName, id);
            _log.LogTrace("Deleting item {cacheId}", cacheId);

            try
            {
                var cacheDb = _lazyConnection.Value.GetDatabase();
                await cacheDb.KeyDeleteAsync(cacheId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error deleting key: {cacheId}");
            }
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
        }

        public async Task<T> GetKeyAsync<T>(string tableName, string id)
        {
            var cacheId = GetCacheId(tableName, id);

            try
            {
                var cacheDb = _lazyConnection.Value.GetDatabase();
                Type type = typeof(T);
                var cacheValue = await cacheDb.StringGetAsync(cacheId).ConfigureAwait(false);

                _log.LogTrace("Retrieving item {cacheId} with value {cacheValue}", cacheId, cacheValue);

                if (!cacheValue.IsNull)
                {
                    if (type.IsValueType)
                    {
                        return (T)Convert.ChangeType(cacheValue, type);
                    }
                    else
                    {
                        return JsonConvert.DeserializeObject<T>(cacheValue);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error getting key: {cacheId}");
            }

            return default;
        }

        public async Task SetKeyAsync<T>(string tableName, string id, T value, bool shortExpiry = false)
        {
            var cacheId = GetCacheId(tableName, id);

            try
            {
                var cacheDb = _lazyConnection.Value.GetDatabase();
                string cacheValue;
                Type type = typeof(T);

                if (value == null)
                {
                    cacheValue = null;
                }
                else if (type.IsValueType)
                {
                    cacheValue = value.ToString();
                }
                else
                {
                    cacheValue = JsonConvert.SerializeObject(value);
                }

                _log.LogTrace("Caching item {cacheId} with value {cacheValue} and short expiry {shortExpiry}", cacheId, cacheValue, shortExpiry);

                TimeSpan lifetime = new TimeSpan(0, shortExpiry ? _options.CacheItemShortExpiryMinutes : _options.CacheItemExpiryMinutes, 0);
                await cacheDb.StringSetAsync(cacheId, cacheValue, lifetime).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error setting key: {cacheId}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _lazyConnection.Value.Dispose();
                }

                disposedValue = true;
            }
        }

        private string GetCacheId(string tableName, string id)
        {
            return $"{tableName}_{id}";
        }

        // To detect redundant calls
    }
}
