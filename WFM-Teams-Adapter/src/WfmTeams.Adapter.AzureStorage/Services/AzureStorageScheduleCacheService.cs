// ---------------------------------------------------------------------------
// <copyright file="AzureStorageScheduleCacheService.cs" company="Microsoft">
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

    public class AzureStorageScheduleCacheService : AzureStorageBlobService, IScheduleCacheService
    {
        public AzureStorageScheduleCacheService(AzureStorageOptions options, ILogger<AzureStorageBlobService> log)
            : base(options, log)
        {
        }

        public async Task DeleteScheduleAsync(string teamId, DateTime weekStartDate)
        {
            var blobName = GetTeamShiftsBlobName(teamId, weekStartDate);
            await DeleteBlobAsync(_options.ShiftsContainerName, blobName);
        }

        public async Task<CacheModel<ShiftModel>> LoadScheduleAsync(string teamId, DateTime weekStartDate)
        {
            var blobName = GetTeamShiftsBlobName(teamId, weekStartDate);
            return await LoadCacheModelBlobAsync<ShiftModel>(_options.ShiftsContainerName, blobName);
        }

        public async Task<LeasedCacheModel<ShiftModel>> LoadScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, TimeSpan leaseTime)
        {
            var blobName = GetTeamShiftsBlobName(teamId, weekStartDate);
            return await LoadBlobWithLeaseAsync<ShiftModel>(_options.ShiftsContainerName, blobName, leaseTime);
        }

        public async Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel<ShiftModel> cacheModel)
        {
            var blobName = GetTeamShiftsBlobName(teamId, weekStartDate);
            await SaveBlobAsync(_options.ShiftsContainerName, blobName, cacheModel);
        }

        public async Task SaveScheduleWithLeaseAsync(string teamId, DateTime weekStartDate, LeasedCacheModel<ShiftModel> cacheModel)
        {
            var blobName = GetTeamShiftsBlobName(teamId, weekStartDate);
            await SaveBlobWithLeaseAsync(_options.ShiftsContainerName, blobName, cacheModel);
        }

        private string GetTeamShiftsBlobName(string teamId, DateTime weekStartDate)
        {
            return $"team_{teamId}_{weekStartDate:yyyyMMdd}";
        }
    }
}
