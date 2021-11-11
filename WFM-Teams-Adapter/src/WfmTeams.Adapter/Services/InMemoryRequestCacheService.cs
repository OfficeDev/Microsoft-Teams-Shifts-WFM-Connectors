// ---------------------------------------------------------------------------
// <copyright file="InMemoryRequestCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Collections.Concurrent;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Extensions;

    public class InMemoryRequestCacheService : IRequestCacheService
    {
        private readonly ConcurrentDictionary<string, object> _requests = new ConcurrentDictionary<string, object>();

        public Task DeleteRequestAsync(string teamId, string requestId)
        {
            _requests.TryRemove(requestId, out object value);

            return Task.CompletedTask;
        }

        public Task<T> LoadRequestAsync<T>(string teamId, string requestId)
        {
            var requestModel = _requests.ReplGetValueOrDefault(requestId);

            return Task.FromResult((T)requestModel);
        }

        public Task SaveRequestAsync<T>(string teamId, string requestId, T requestModel)
        {
            _requests[requestId] = requestModel;

            return Task.CompletedTask;
        }
    }
}
