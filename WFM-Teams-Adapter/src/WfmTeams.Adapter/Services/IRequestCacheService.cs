// ---------------------------------------------------------------------------
// <copyright file="IRequestCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;

    public interface IRequestCacheService
    {
        Task DeleteRequestAsync(string teamId, string requestId);

        Task<T> LoadRequestAsync<T>(string teamId, string requestId);

        Task SaveRequestAsync<T>(string teamId, string requestId, T requestModel);
    }
}
