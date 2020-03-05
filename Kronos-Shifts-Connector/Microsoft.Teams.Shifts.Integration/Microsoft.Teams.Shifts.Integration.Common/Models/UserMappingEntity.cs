// <copyright file="UserMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the User to User mapping.
    /// </summary>
    public class UserMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the UserToUserMappingId.
        /// </summary>
        [Key]
        [JsonProperty("UserToUserMappingId")]
        public string UserToUserMappingId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        [JsonProperty("TenantId")]
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsTeamId.
        /// </summary>
        [JsonProperty("ShiftsTeamId")]
        public string ShiftsTeamId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsUserAadObjectId.
        /// </summary>
        [JsonProperty("ShiftsUserAadObjectId")]
        public string ShiftsUserAadObjectId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsUserName.
        /// </summary>
        [JsonProperty("ShiftsUserName")]
        public string ShiftsUserName { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber.
        /// </summary>
        [JsonProperty("KronosPersonNumber")]
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the KronosUserName.
        /// </summary>
        [JsonProperty("KronosUserName")]
        public string KronosUserName { get; set; }
    }
}