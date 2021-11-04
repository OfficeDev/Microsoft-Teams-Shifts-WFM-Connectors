// <copyright file="OpenShiftRequestIS.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This model defines the incoming open shift request.
    /// </summary>
    public class OpenShiftRequestIS
    {
        /// <summary>
        /// Gets or sets the assignedTo.
        /// </summary>
        [JsonProperty("assignedTo")]
        public string AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the senderDateTime.
        /// </summary>
        [JsonProperty("senderDateTime")]
        public DateTime SenderDateTime { get; set; }

        /// <summary>
        /// Gets or sets the senderMessage.
        /// </summary>
        [JsonProperty("senderMessage")]
        public string SenderMessage { get; set; }

        /// <summary>
        /// Gets or sets the senderUserId.
        /// </summary>
        [JsonProperty("senderUserId")]
        public string SenderUserId { get; set; }

        /// <summary>
        /// Gets or sets the managerUserId.
        /// </summary>
        [JsonProperty("managerUserId")]
        public string ManagerUserId { get; set; }

        /// <summary>
        /// Gets or sets the managerActionMessage.
        /// </summary>
        [JsonProperty("managerActionMessage")]
        public string ManagerActionMessage { get; set; }

        /// <summary>
        /// Gets or sets the openShiftId.
        /// </summary>
        [JsonProperty("openShiftId")]
        public string OpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the createdDateTime.
        /// </summary>
        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the lastModifiedDateTime.
        /// </summary>
        [JsonProperty("lastModifiedDateTime")]
        public DateTime LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}