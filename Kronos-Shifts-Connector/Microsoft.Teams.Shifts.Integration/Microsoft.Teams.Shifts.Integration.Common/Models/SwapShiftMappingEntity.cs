// <copyright file="SwapShiftMappingEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Microsoft.WindowsAzure.Storage.Table;

    /// <summary>
    /// This class models the ShiftMappingEntity.
    /// </summary>
    public class SwapShiftMappingEntity : TableEntity
    {
        /// <summary>
        /// Gets or sets the Shift ID from Shifts - "SHFT_guid".
        /// </summary>
        public string TeamsOfferedShiftId { get; set; }

        /// <summary>
        /// Gets or sets TeamsRequestedShiftId.
        /// </summary>
        public string TeamsRequestedShiftId { get; set; }

        /// <summary>
        /// Gets or sets the unique ID that is generated from the UniqueIdUtility.CreateUniqueId() method.
        /// </summary>
        public string KronosUniqueIdForOfferedShift { get; set; }

        /// <summary>
        /// Gets or sets the unique ID that is generated from the UniqueIdUtility.CreateUniqueId() method.
        /// </summary>
        public string KronosUniqueIdForRequestedShift { get; set; }

        /// <summary>
        /// Gets or sets the AadObjectId of the user.
        /// </summary>
        public string AadUserId { get; set; }

        /// <summary>
        /// Gets or sets the KronosPersonNumber.
        /// </summary>
        public string RequestorKronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the RequestedKronosPersonNumber.
        /// </summary>
        public string RequestedKronosPersonNumber { get; set; }

        /// <summary>
        /// Gets or sets the Kronos StatusName.
        /// </summary>
        public string KronosStatus { get; set; }

        /// <summary>
        /// Gets or sets the Shifts StatusName.
        /// </summary>
        public string ShiftsStatus { get; set; }

        /// <summary>
        /// Gets or sets the KronosReqId.
        /// </summary>
        public string KronosReqId { get; set; }

        /// <summary>
        /// Gets or sets shifts teams id.
        /// </summary>
        public string ShiftsTeamId { get; set; }
    }
}