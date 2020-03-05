// <copyright file="SchedulingGroupDetails.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System.Collections.Generic;

    /// <summary>
    /// This class models the SchedulingGroupDetails.
    /// </summary>
    public class SchedulingGroupDetails
    {
        /// <summary>
        /// Gets or sets the value which is a list of the ShiftSchedulingGroups.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftSchedulingGroups> Value { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}