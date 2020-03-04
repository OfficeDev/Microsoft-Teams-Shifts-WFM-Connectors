// <copyright file="AuthOptionsModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Models
{
    /// <summary>
    /// The Authentication options definitions.
    /// </summary>
    public class AuthOptionsModel
    {
        /// <summary>
        /// Gets or sets the Authority.
        /// </summary>
        public string Authority { get; set; }

        /// <summary>
        /// Gets or sets the ClientId.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets the ClientSecret.
        /// </summary>
        public string ClientSecret { get; set; }
    }
}