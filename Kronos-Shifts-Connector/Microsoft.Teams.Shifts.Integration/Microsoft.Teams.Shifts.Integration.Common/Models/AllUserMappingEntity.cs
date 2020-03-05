// <copyright file="AllUserMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the User to User mapping.
    /// </summary>
    public class AllUserMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the ShiftsUserAadObjectId.
        /// </summary>
        [JsonProperty("ShiftUserAadObjectId")]
        public string ShiftUserAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsUserName.
        /// </summary>
        [JsonProperty("ShiftUserDisplayName")]
        public string ShiftUserDisplayName { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber.
        /// </summary>
        [JsonProperty("ShiftUserUpn")]
        public string ShiftUserUpn { get; set; }

        /// <summary>
        /// Gets or sets the KronosUserName.
        /// </summary>
        [JsonProperty("KronosUserName")]
        public string KronosUserName { get; set; }
    }
}