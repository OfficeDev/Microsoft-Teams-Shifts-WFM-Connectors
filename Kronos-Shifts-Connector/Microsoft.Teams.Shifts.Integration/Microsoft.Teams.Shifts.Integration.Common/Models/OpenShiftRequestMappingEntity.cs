// <copyright file="OpenShiftRequestMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class defines the Azure table schema for the OpenShiftRequestMapping.
    /// </summary>
    public class OpenShiftRequestMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the OpenShiftRequestMappingId -
        /// the PRIMARY KEY.
        /// </summary>
        [JsonProperty("OpenShiftRequestMappingId")]
        public string OpenShiftRequestMappingId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        [JsonProperty("TeamsTenantId")]
        public string TeamsTenantId { get; set; }

        /// <summary>
        /// Gets or sets the OpenShiftId from Teams.
        /// </summary>
        [JsonProperty("TeamsOpenShiftId")]
        public string TeamsOpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftRequestId -
        /// this is coming from the outbound request.
        /// </summary>
        [JsonProperty("TeamsShiftRequestId")]
        public string TeamsShiftRequestId { get; set; }

        /// <summary>
        /// Gets or sets the AadObjectId of the Teams user -
        /// this is coming from the outbound request.
        /// </summary>
        [JsonProperty("TeamsUserId")]
        public string TeamsUserId { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber -
        /// this is coming from the UserToUserMapping.
        /// </summary>
        [JsonProperty("KronosPersonNumber")]
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the KronosRequestId -
        /// this is coming from when the Open Shift Request is posted to Kronos.
        /// </summary>
        [JsonProperty("KronosRequestId")]
        public string KronosRequestId { get; set; }

        /// <summary>
        /// Gets or sets the KronosRequestStatus -
        /// this is coming from when the Open Shift Request in Kronos is updated
        /// to SUBMITTED from DRAFT.
        /// </summary>
        [JsonProperty("KronosRequestStatus")]
        public string KronosRequestStatus { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath -
        /// this is from the request being made to push the Open Shift Request into Kronos
        /// (going in as DRAFT).
        /// </summary>
        [JsonProperty("KronosOrgJobPath")]
        public string KronosOrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the MonthWisePartition.
        /// </summary>
        [JsonProperty("MonthWisePartition")]
        public string MonthWisePartition { get; set; }

        /// <summary>
        /// Gets or sets the Hash of attributes of OpenShift, need to be obtained by using OpenShiftMapping
        /// for a given Teams Open Shift Id.
        /// </summary>
        [JsonProperty("KronosOpenShiftUniqueId")]
        public string KronosOpenShiftUniqueId { get; set; }
    }
}