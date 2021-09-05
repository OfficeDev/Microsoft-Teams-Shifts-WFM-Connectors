// ---------------------------------------------------------------------------
// <copyright file="AzureStorageBlobService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Storage;
    using Azure.Storage.Blobs;
    using Azure.Storage.Blobs.Models;
    using Azure.Storage.Blobs.Specialized;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Models;

    public abstract class AzureStorageBlobService
    {
        protected readonly ILogger<AzureStorageBlobService> _log;

        protected readonly AzureStorageOptions _options;

        public AzureStorageBlobService(AzureStorageOptions options, ILogger<AzureStorageBlobService> log)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _log = log ?? throw new ArgumentNullException(nameof(log));
        }

        protected async Task DeleteBlobAsync(string containerName, string blobName)
        {
            var blob = GetBlobClient(containerName, blobName);
            await blob.DeleteIfExistsAsync();
        }

        protected BlobClient GetBlobClient(string containerName, string blobName)
        {
            var container = new BlobContainerClient(_options.ConnectionString, containerName);
            return container.GetBlobClient(blobName);
        }

        protected async Task<T> LoadBlobAsync<T>(string containerName, string blobName)
        {
            var blob = GetBlobClient(containerName, blobName);

            if (await blob.ExistsAsync())
            {
                var response = await blob.DownloadAsync();
                using var streamReader = new StreamReader(response.Value.Content);
                using var jsonReader = new JsonTextReader(streamReader);
                var serializer = new JsonSerializer();

                return serializer.Deserialize<T>(jsonReader);
            }

            return default;
        }

        protected async Task<LeasedCacheModel<T>> LoadBlobWithLeaseAsync<T>(string containerName, string blobName, TimeSpan leaseTime)
        {
            var blob = GetBlobClient(containerName, blobName);

            if (await blob.ExistsAsync())
            {
                var response = await blob.DownloadAsync();
                using var streamReader = new StreamReader(response.Value.Content);
                using var jsonReader = new JsonTextReader(streamReader);

                var serializer = new JsonSerializer();
                var model = serializer.Deserialize<LeasedCacheModel<T>>(jsonReader);

                if (model != null)
                {
                    var leaseClient = blob.GetBlobLeaseClient();
                    var leaseResponse = await leaseClient.AcquireAsync(leaseTime);
                    _log.LogTrace($"{nameof(LoadBlobWithLeaseAsync)}: LeaseId={leaseResponse.Value.LeaseId}, LastModified={leaseResponse.Value.LastModified}, LeaseTime={leaseResponse.Value.LeaseTime}");
                    model.LeaseId = leaseResponse.Value.LeaseId;

                    return model;
                }
            }

            return new LeasedCacheModel<T>();
        }

        protected async Task<CacheModel<T>> LoadCacheModelBlobAsync<T>(string containerName, string blobName)
        {
            var blob = await LoadBlobAsync<CacheModel<T>>(containerName, blobName);
            return blob ?? new CacheModel<T>();
        }

        protected async Task SaveBlobAsync<T>(string containerName, string blobName, T model)
        {
            var blob = GetBlobClient(containerName, blobName);

            using var ms = new MemoryStream();
            LoadStreamWithJson(ms, model);
            await blob.UploadAsync(ms, overwrite: true);
        }

        protected async Task SaveBlobWithLeaseAsync<T>(string containerName, string blobName, LeasedCacheModel<T> model)
        {
            var blob = GetBlobClient(containerName, blobName);

            if (string.IsNullOrEmpty(model.LeaseId))
            {
                // this model doesn't have a lease so use the standard save instead
                await SaveBlobAsync(containerName, blobName, model);
            }
            else
            {
                using var ms = new MemoryStream();
                LoadStreamWithJson(ms, model);

                _log.LogTrace($"{nameof(SaveBlobWithLeaseAsync)}: BlobName={blob.Name}");
                _log.LogTrace($"{nameof(SaveBlobWithLeaseAsync)}: ModelLeaseId={model.LeaseId}");
                var props = await blob.GetPropertiesAsync();

                var leaseClient = blob.GetBlobLeaseClient(model.LeaseId);
                _log.LogTrace($"{nameof(SaveBlobWithLeaseAsync)}: LeaseClientLeaseId={leaseClient.LeaseId}");

                var requestConditions = new BlobRequestConditions
                {
                    LeaseId = model.LeaseId
                };

                _log.LogTrace($"{nameof(SaveBlobWithLeaseAsync)}: LeaseId={requestConditions.LeaseId}");

                try
                {
                    await blob.UploadAsync(ms, conditions: requestConditions);
                }
                finally
                {
                    await leaseClient.ReleaseAsync(requestConditions);
                }
            }
        }

        private void LoadStreamWithJson(Stream ms, object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            StreamWriter writer = new StreamWriter(ms);
            writer.Write(json);
            writer.Flush();
            ms.Position = 0;
        }
    }
}
