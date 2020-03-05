// <copyright file="AzureStorageHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Helper
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.ApplicationInsights;
    using Microsoft.WindowsAzure.Storage;
    using Microsoft.WindowsAzure.Storage.Blob;

    /// <summary>
    /// Helper class to download files from the blob storage.
    /// </summary>
    public static class AzureStorageHelper
    {
        /// <summary>
        /// Download File from Azure blob stoareage.
        /// </summary>
        /// <param name="storageAccountConnectionString">Coonection string for Storage Account.</param>
        /// <param name="storageContainerName">Name of Storage Container.</param>
        /// <param name="fileName">Actual blob name.</param>
        /// <param name="telemetryClient">The telemetry mechanism.</param>
        /// <returns>MemoryStream.</returns>
        public static async Task<MemoryStream> DownloadFileFromBlobAsync(
            string storageAccountConnectionString,
            string storageContainerName,
            string fileName,
            TelemetryClient telemetryClient)
        {
            if (telemetryClient is null)
            {
                throw new ArgumentNullException(nameof(telemetryClient));
            }

            try
            {
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(storageAccountConnectionString);
                CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
                CloudBlobContainer cloudBlobContainer = blobClient.GetContainerReference(storageContainerName);
                CloudBlockBlob blockBlob = cloudBlobContainer.GetBlockBlobReference(fileName);
                MemoryStream memStream = new MemoryStream();
                await blockBlob.DownloadToStreamAsync(memStream).ConfigureAwait(false);
                return memStream;
            }
            catch (Exception exception)
            {
                // Log Exception Here
                telemetryClient.TrackException(exception);
                throw;
            }
        }
    }
}