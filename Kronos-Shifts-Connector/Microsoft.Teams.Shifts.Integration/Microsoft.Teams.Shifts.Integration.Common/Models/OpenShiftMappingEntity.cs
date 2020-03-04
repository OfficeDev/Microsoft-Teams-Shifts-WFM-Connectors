// <copyright file="OpenShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.ComponentModel.DataAnnotations;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the Open Shift Entity being mapped betweeen Kronos and Shifts.
    /// </summary>
    public class OpenShiftMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the OpenShiftMappingEntityId - this serves as the Primary Key.
        /// </summary>
        [Key]
        public string OpenShiftMappingEntityId { get; set; }

        /// <summary>
        /// Gets or sets the Open Shift ID from Shifts - "OPNSHFT_guid".
        /// </summary>
        public string GraphOpenShiftId { get; set; }

        /// <summary>
        /// Gets or sets the unique ID that is generated from the UniqueIdUtility.CreateUniqueId(OpenShift openShift) method.
        /// </summary>
        public string KronosUniqueId { get; set; }

        /// <summary>
        /// Gets or sets the KronosSlots.
        /// </summary>
        public string KronosSlots { get; set; }

        /// <summary>
        /// Gets or sets the MonthWisePartition.
        /// </summary>
        public string MonthWisePartition { get; set; }
    }
}