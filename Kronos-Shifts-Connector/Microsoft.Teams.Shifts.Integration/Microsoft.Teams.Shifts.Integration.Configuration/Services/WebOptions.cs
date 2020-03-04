// <copyright file="WebOptions.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Configuration.Services
{
    /// <summary>
    /// Specifies necessary options.
    /// </summary>
    public class WebOptions
    {
        /// <summary>
        /// Gets or sets the GraphApiUrl.
        /// </summary>
#pragma warning disable CA1056 // Uri properties should not be strings
        public string GraphApiUrl { get; set; }
#pragma warning restore CA1056 // Uri properties should not be strings
    }
}