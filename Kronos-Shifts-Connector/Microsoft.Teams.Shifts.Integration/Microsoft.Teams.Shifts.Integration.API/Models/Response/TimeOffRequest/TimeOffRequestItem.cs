// <copyright file="TimeOffRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest
{
    using System;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the TimeOffRequestItem.
    /// </summary>
    public partial class TimeOffRequestItem
    {
        /// <summary>
        /// Gets or sets the OdataEtag.
        /// </summary>
        [JsonProperty("@odata.etag")]
        public string OdataEtag { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CreatedDateTime.
        /// </summary>
        [JsonProperty("createdDateTime")]
        public DateTimeOffset CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the LastModifiedDateTime.
        /// </summary>
        [JsonProperty("lastModifiedDateTime")]
        public DateTimeOffset LastModifiedDateTime { get; set; }

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
        public DateTimeOffset SenderDateTime { get; set; }

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
        /// Gets or sets the ManagerActionDateTime.
        /// </summary>
        [JsonProperty("managerActionDateTime")]
        public DateTimeOffset? ManagerActionDateTime { get; set; }

        /// <summary>
        /// Gets or sets the ManagerActionMessage.
        /// </summary>
        [JsonProperty("managerActionMessage")]
        public string ManagerActionMessage { get; set; }

        /// <summary>
        /// Gets or sets the ManagerUserId.
        /// </summary>
        [JsonProperty("managerUserId")]
        public string ManagerUserId { get; set; }

        /// <summary>
        /// Gets or sets the StartDateTime.
        /// </summary>
        [JsonProperty("startDateTime")]
        public DateTimeOffset StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDateTime.
        /// </summary>
        [JsonProperty("endDateTime")]
        public DateTimeOffset EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the TimeOffReasonId.
        /// </summary>
        [JsonProperty("timeOffReasonId")]
        public string TimeOffReasonId { get; set; }

        /// <summary>
        /// Gets or sets the LastModifiedBy.
        /// </summary>
        [JsonProperty("lastModifiedBy")]
        public LastModifiedBy LastModifiedBy { get; set; }
    }
}