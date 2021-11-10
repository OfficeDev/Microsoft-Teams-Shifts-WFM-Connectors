// <copyright file="SwapRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This request models the SwapShiftRequest that is coming in.
    /// </summary>
    public class SwapRequest
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
        /// Gets or sets the recipientUserId.
        /// </summary>
        [JsonProperty("recipientUserId")]
        public string RecipientUserId { get; set; }

        /// <summary>
        /// Gets or sets the recipientActionMessage.
        /// </summary>
        [JsonProperty("recipientActionMessage")]
        public string RecipientActionMessage { get; set; }

        /// <summary>
        /// Gets or sets the recipientActionDateTime.
        /// </summary>
        [JsonProperty("recipientActionDateTime")]
        public DateTime RecipientActionDateTime { get; set; }

        /// <summary>
        /// Gets or sets the managerActionMessage.
        /// </summary>
        [JsonProperty("managerActionMessage")]
        public string ManagerActionMessage { get; set; }

        /// <summary>
        /// Gets or sets the eTag.
        /// </summary>
        [JsonProperty("eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the senderShiftId.
        /// </summary>
        [JsonProperty("senderShiftId")]
        public string SenderShiftId { get; set; }

        /// <summary>
        /// Gets or sets the recipientShiftId.
        /// </summary>
        [JsonProperty("recipientShiftId")]
        public string RecipientShiftId { get; set; }

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
        /// Gets or sets the id (requestId).
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}