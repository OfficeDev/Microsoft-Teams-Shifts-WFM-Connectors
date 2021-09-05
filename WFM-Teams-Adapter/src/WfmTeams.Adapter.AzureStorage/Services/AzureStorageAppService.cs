// ---------------------------------------------------------------------------
// <copyright file="AzureStorageAppService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.AzureStorage.Services
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Azure.Storage.Blobs;
    using WfmTeams.Adapter.AzureStorage.Options;
    using WfmTeams.Adapter.Services;

    public class AzureStorageAppService : IAppService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageAppService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public async Task<Stream> OpenAppStreamAsync()
        {
            var container = new BlobContainerClient(_options.ConnectionString, _options.AppContainerName);
            var blob = container.GetBlobClient(_options.AppBlobName);
            var response = await blob.DownloadAsync();
            return response.Value.Content;
        }
    }
}
