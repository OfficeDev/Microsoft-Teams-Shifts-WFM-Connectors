using JdaTeams.Connector.AzureStorage.Options;
using JdaTeams.Connector.Models;
using JdaTeams.Connector.Services;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JdaTeams.Connector.AzureStorage.Services
{
    public class AzureStorageScheduleCacheService : IScheduleCacheService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageScheduleCacheService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<CacheModel> LoadScheduleAsync(string teamId, DateTime weekStartDate)
        {
            var teamShiftsBlob = GetTeamShiftsBlob(teamId, weekStartDate);
            if (await teamShiftsBlob.ExistsAsync())
            {
                using (var blobStream = await teamShiftsBlob.OpenReadAsync())
                using (var streamReader = new StreamReader(blobStream))
                using (var jsonReader = new JsonTextReader(streamReader))
                {
                    var serializer = new JsonSerializer();
                    return serializer.Deserialize<CacheModel>(jsonReader);
                }
            }

            return new CacheModel();
        }

        public async Task SaveScheduleAsync(string teamId, DateTime weekStartDate, CacheModel cacheModel)
        {
            var teamShiftsBlob = GetTeamShiftsBlob(teamId, weekStartDate);
            using (var blobStream = await teamShiftsBlob.OpenWriteAsync())
            using (var streamWriter = new StreamWriter(blobStream))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = new JsonSerializer();
                serializer.Serialize(jsonWriter, cacheModel);
            }
        }

        public async Task DeleteScheduleAsync(string teamId, DateTime weekStartDate)
        {
            var teamShiftsBlob = GetTeamShiftsBlob(teamId, weekStartDate);

            await teamShiftsBlob.DeleteIfExistsAsync();
        }

        private CloudBlockBlob GetTeamShiftsBlob(string teamId, DateTime weekStartDate)
        {
            var storageAccount = CloudStorageAccount.Parse(_options.ConnectionString);
            var cloudBlobClient = storageAccount.CreateCloudBlobClient();
            var shiftsContainer = cloudBlobClient.GetContainerReference(_options.ShiftsContainerName);
            var teamShiftsName = $"team_{teamId}_{weekStartDate.ToString("yyyyMMdd")}";
            return shiftsContainer.GetBlockBlobReference(teamShiftsName);
        }
    }
}
