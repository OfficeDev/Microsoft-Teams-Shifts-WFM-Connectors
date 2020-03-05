// <copyright file="ShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the ShiftMappingEntity.
    /// </summary>
    public class ShiftMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the ShiftMappingEntityId.
        /// </summary>
        [Key]
        public string ShiftMappingEntityId { get; set; }

        /// <summary>
        /// Gets or sets the TenantId.
        /// </summary>
        public string TenantId { get; set; }

        /// <summary>
        /// Gets or sets the Shift ID from Shifts - "SHFT_guid".
        /// </summary>
        public string GraphShiftId { get; set; }

        /// <summary>
        /// Gets or sets the unique ID that is generated from the UniqueIdUtility.CreateUniqueId() method.
        /// </summary>
        public string KronosUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the AadObjectId of the user.
        /// </summary>
        public string AadUserId { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber.
        /// </summary>
        public string KronosPersonNumber { get; set; }
    }
}