// <copyright file="KeyVaultHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    using Microsoft.ApplicationInsights;
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.Services.AppAuthentication;

    /// <summary>
    /// KeyVaultHelper class to read key values.
    /// </summary>
    public class KeyVaultHelper : IKeyVaultHelper
    {
        private readonly TelemetryClient telemetryClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="KeyVaultHelper"/> class.
        /// </summary>
        /// <param name="telemetryClient">telemetry client.</param>
        public KeyVaultHelper(TelemetryClient telemetryClient)
        {
            this.telemetryClient = telemetryClient;
        }

        /// <summary>
        /// Get secret from azure key vault by URI.
        /// </summary>
        /// <param name="resourceUri">URI of secret to be fetched.</param>
        /// <returns>returns the secret string value.</returns>
#pragma warning disable CA1055 // Uri return values should not be strings
#pragma warning disable CA1054 // Uri parameters should not be strings
        public string GetSecretByUri(string resourceUri)
#pragma warning restore CA1054 // Uri parameters should not be strings
#pragma warning restore CA1055 // Uri return values should not be strings
        {
            string secret = string.Empty;

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            var authCallback = new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback);
            using (var keyvaultClient = new KeyVaultClient(authCallback))
            {
                secret = keyvaultClient.GetSecretAsync(resourceUri).Result.Value;
            }

            return secret;
        }

        /// <summary>
        /// Method to set the necessary properties in the Azure KeyVault.
        /// </summary>
        /// <param name="keyVaultKey">The key for the KeyVault.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The actual value of the secret.</param>
        /// <returns>A unit of execution which has a string value boxed in.</returns>
        public string SetKeyVaultSecret(string keyVaultKey, string secretName, string secretValue)
        {
            string secret = string.Empty;

            var azureServiceTokenProvider = new AzureServiceTokenProvider();
            using (var keyvaultClient = new KeyVaultClient(new KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback)))
            {
                var secretBundle = keyvaultClient.SetSecretAsync(keyVaultKey, secretName, secretValue);
                secret = secretBundle.Result.Value;
            }

            return secret;
        }
    }
}