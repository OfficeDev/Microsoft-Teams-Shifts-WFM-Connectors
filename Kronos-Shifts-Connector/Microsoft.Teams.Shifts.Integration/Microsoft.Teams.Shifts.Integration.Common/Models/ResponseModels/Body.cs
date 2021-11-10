// <copyright file="Body.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the body of the Integration Service API response.
    /// </summary>
    public class Body
    {
        /// <summary>
        /// Gets or sets the ETag.
        /// </summary>
        [JsonProperty("eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the error.
        /// </summary>
        [JsonProperty("error")]
        public ResponseError Error { get; set; }

        /// <summary>
        /// Gets or sets the list of eligible shifts.
        /// </summary>
        [JsonProperty("data")]
        public IEnumerable<string> Data { get; set; }
    }
}