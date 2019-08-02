using JdaTeams.Connector.AzureStorage.Options;
using JdaTeams.Connector.Services;
using Microsoft.WindowsAzure.Storage;
using System;
using System.IO;
using System.Threading.Tasks;

namespace JdaTeams.Connector.AzureStorage.Services
{
    public class AzureStorageAppService : IAppService
    {
        private readonly AzureStorageOptions _options;

        public AzureStorageAppService(AzureStorageOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public Task<Stream> OpenAppStreamAsync()
        {
            var account = CloudStorageAccount.Parse(_options.ConnectionString);
            var client = account.CreateCloudBlobClient();
            var container = client.GetContainerReference(_options.AppContainerName);
            var blob = container.GetBlockBlobReference(_options.AppBlobName);
            return blob.OpenReadAsync();
        }
    }
}
