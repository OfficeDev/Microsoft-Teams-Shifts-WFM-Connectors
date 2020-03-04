// <copyright file="ShiftsTeamsDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.Common.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models details fetched from Shifts team via Graph api calls.
    /// </summary>
    public class ShiftsTeamsDetails
    {
        /// <summary>
        /// Gets or sets the Shifts team ID.
        /// </summary>
        [JsonProperty("id")]
        public string TeamId { get; set; }

        /// <summary>
        /// Gets or sets the Shifts team display name.
        /// </summary>
        [JsonProperty("displayName")]
        public string TeamDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Shifts team description.
        /// </summary>
        [JsonProperty("description")]
        public string TeamDescription { get; set; }

        /// <summary>
        /// Gets or sets the Shifts team internal Id.
        /// </summary>
        [JsonProperty("internalId")]
        public string TeamInternalId { get; set; }

        /// <summary>
        /// Gets or sets the Shifts team web url.
        /// </summary>
        [JsonProperty("webUrl")]
        public string TeamWebUrl { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the status of Shifts team is archived.
        /// </summary>
        [JsonProperty("isArchived")]
        public bool IsArchived { get; set; }
    }
}
