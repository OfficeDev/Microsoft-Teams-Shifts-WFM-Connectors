// <copyright file="Body.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.OpenShift
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class is used to create request for Open shift in Kronos.
    /// </summary>
    public class Body
    {
        /// <summary>
        /// Gets or sets the AssignedTo.
        /// </summary>
        [JsonProperty("assignedTo")]
        public string AssignedTo { get; set; }

        /// <summary>
        /// Gets or sets the State.
        /// </summary>
        [JsonProperty("state")]
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the SenderDateTime.
        /// </summary>
        [JsonProperty("senderDateTime")]
        public DateTime SenderDateTime { get; set; }

        /// <summary>
        /// Gets or sets the SenderMessage.
        /// </summary>
        [JsonProperty("senderMessage")]
        public string SenderMessage { get; set; }

        /// <summary>
        /// Gets or sets the SenderUserId.
        /// </summary>
        [JsonProperty("senderUserId")]
        public string SenderUserId { get; set; }

        /// <summary>
        /// Gets or sets the ManagerUserId.
        /// </summary>
        [JsonProperty("managerUserId")]
        public string ManagerUserId { get; set; }

        /// <summary>
        /// Gets or sets the OpenShiftId.
        /// </summary>
        [JsonProperty("openShiftId")]
        public string OpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the CreatedDateTime.
        /// </summary>
        [JsonProperty("createdDateTime")]
        public DateTime CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the LastModifiedDateTime.
        /// </summary>
        [JsonProperty("lastModifiedDateTime")]
        public DateTime LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}
