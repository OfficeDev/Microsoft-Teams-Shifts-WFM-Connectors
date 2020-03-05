// <copyright file="Encryption.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// Have the necessary encryption "navigation" properties.
    /// </summary>
    public class Encryption
    {
        /// <summary>
        /// Gets or sets the protocol of sharing credentials.
        /// </summary>
        [JsonProperty("protocol")]
        public string Protocol { get; set; } = "sharedSecret";

        /// <summary>
        /// Gets or sets the symmetric key used to encrypt the payload that will be sent over to the integration from Shifts.
        /// </summary>
        [JsonProperty("secret")]
        public string Secret { get; set; }
    }
}