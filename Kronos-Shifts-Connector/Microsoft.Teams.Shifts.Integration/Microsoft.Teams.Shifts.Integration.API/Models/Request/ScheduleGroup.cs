// <copyright file="ScheduleGroup.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using System.Collections.Generic;

    /// <summary>
    /// Schedule Group Entity.
    /// </summary>
    public class ScheduleGroup
    {
        /// <summary>
        /// Gets or sets the Display name of scheduling group.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the Active indicator of group.
        /// </summary>
        public string IsActive { get; set; }

        /// <summary>
        /// Gets or sets the User Id list of group.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<string> UserIds { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}