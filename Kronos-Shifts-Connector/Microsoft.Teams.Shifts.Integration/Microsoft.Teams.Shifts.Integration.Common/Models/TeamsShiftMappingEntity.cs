// <copyright file="TeamsShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System;
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the Teams ShiftMappingEntity.
    /// </summary>
    public class TeamsShiftMappingEntity : TableEntity
    {
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

        /// <summary>
        /// Gets or Sets the shift start date.
        /// </summary>
        public DateTime ShiftStartDate { get; set; }
    }
}