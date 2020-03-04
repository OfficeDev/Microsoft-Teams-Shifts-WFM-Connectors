// <copyright file="IKeyVaultHelper.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Common
{
    /// <summary>
    /// This interface defines methods for accessing the Azure KeyVault for application related configurations.
    /// </summary>
    public interface IKeyVaultHelper
    {
        /// <summary>
        /// Get secret from azure key vault by URI.
        /// </summary>
        /// <param name="resourceUri">URI of secret to be fetched.</param>
        /// <returns>returns the secret string value.</returns>
#pragma warning disable CA1054 // Uri parameters should not be strings
#pragma warning disable CA1055 // Uri return values should not be strings
        string GetSecretByUri(string resourceUri);
#pragma warning restore CA1055 // Uri return values should not be strings
#pragma warning restore CA1054 // Uri parameters should not be strings

        /// <summary>
        /// Method to set the necessary properties in the Azure KeyVault.
        /// </summary>
        /// <param name="keyVaultKey">The key for the KeyVault.</param>
        /// <param name="secretName">The name of the secret.</param>
        /// <param name="secretValue">The actual value of the secret.</param>
        /// <returns>A unit of execution which has a string value boxed in.</returns>
        string SetKeyVaultSecret(string keyVaultKey, string secretName, string secretValue);
    }
}