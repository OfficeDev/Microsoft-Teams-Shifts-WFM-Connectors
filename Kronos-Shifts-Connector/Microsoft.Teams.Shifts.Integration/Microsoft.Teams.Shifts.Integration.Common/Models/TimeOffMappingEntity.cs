// <copyright file="TimeOffMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the TimeOffMappingEntity.
    /// </summary>
    public class TimeOffMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the KronosRequestId.
        /// </summary>
        public string KronosRequestId { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsRequestId.
        /// </summary>
        public string ShiftsRequestId { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber.
        /// </summary>
        public string KronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the StartDate.
        /// </summary>
        public string StartDate { get; set; }

        /// <summary>
        /// Gets or sets the StartTime.
        /// </summary>
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDate.
        /// </summary>
        public string EndDate { get; set; }

        /// <summary>
        /// Gets or sets the PayCodeName.
        /// </summary>
        public string PayCodeName { get; set; }

        /// <summary>
        /// Gets or sets the Duration.
        /// </summary>
        public string Duration { get; set; }

        /// <summary>
        /// Gets or sets the KronosStatus.
        /// </summary>
        public string KronosStatus { get; set; }

        /// <summary>
        /// Gets or sets the ShiftsStatus.
        /// </summary>
        public string ShiftsStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not a time off is requested.
        /// </summary>
        public bool IsActive { get; set; }
    }
}