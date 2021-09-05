// ---------------------------------------------------------------------------
// <copyright file="AzureStorageTimeOffCacheService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Logging;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Models;
    using WfmTeams.Adapter.Services;

    public class AzureStorageTimeOffCacheService : AzureStorageBlobService, ITimeOffCacheService
    {
        public AzureStorageTimeOffCacheService(AzureStorageOptions options, ILogger<AzureStorageBlobService> log)
            : base(options, log)
        {
        }

        public async Task DeleteTimeOffAsync(string teamId, DateTime weekStartDate)
        {
            var blobName = GetTimeOffBlobName(teamId, weekStartDate);
            await DeleteBlobAsync(_options.TimeOffContainerName, blobName);
        }

        public async Task<CacheModel<TimeOffModel>> LoadTimeOffAsync(string teamId, DateTime weekStartDate)
        {
            var blobName = GetTimeOffBlobName(teamId, weekStartDate);
            return await LoadCacheModelBlobAsync<TimeOffModel>(_options.TimeOffContainerName, blobName);
        }

        public async Task SaveTimeOffAsync(string teamId, DateTime weekStartDate, CacheModel<TimeOffModel> cacheModel)
        {
            var blobName = GetTimeOffBlobName(teamId, weekStartDate);
            await SaveBlobAsync(_options.TimeOffContainerName, blobName, cacheModel);
        }

        private string GetTimeOffBlobName(string teamId, DateTime weekStartDate)
        {
            return $"team_{teamId}_{weekStartDate:yyyyMMdd}";
        }
    }
}
