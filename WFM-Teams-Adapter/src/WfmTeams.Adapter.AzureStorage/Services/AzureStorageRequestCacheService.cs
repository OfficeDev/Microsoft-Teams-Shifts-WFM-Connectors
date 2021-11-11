// ---------------------------------------------------------------------------
// <copyright file="AzureStorageRequestCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Services;

    public class AzureStorageRequestCacheService : AzureStorageBlobService, IRequestCacheService
    {
        public AzureStorageRequestCacheService(AzureStorageOptions options, ILogger<AzureStorageBlobService> log)
            : base(options, log)
        {
        }

        public async Task DeleteRequestAsync(string teamId, string requestId)
        {
            var blobName = GetRequestBlobName(teamId, requestId);
            await DeleteBlobAsync(_options.RequestsContainerName, blobName);
        }

        public async Task<T> LoadRequestAsync<T>(string teamId, string requestId)
        {
            var blobName = GetRequestBlobName(teamId, requestId);
            return await LoadBlobAsync<T>(_options.RequestsContainerName, blobName);
        }

        public async Task SaveRequestAsync<T>(string teamId, string requestId, T requestModel)
        {
            var blobName = GetRequestBlobName(teamId, requestId);
            await SaveBlobAsync(_options.RequestsContainerName, blobName, requestModel);
        }

        private string GetRequestBlobName(string teamId, string requestId)
        {
            return $"{teamId}_{requestId}";
        }
    }
}
