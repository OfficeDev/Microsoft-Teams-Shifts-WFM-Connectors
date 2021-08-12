// <copyright file="SharedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.IntegrationAPI
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// This class models the SharedShift.
    /// </summary>
    public class SharedShift
    {
        /// <summary>
        /// Gets or sets the displayName.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the notes.
        /// </summary>
        public string Notes { get; set; }

        /// <summary>
        /// Gets or sets the activities.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<Activity> Activities { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets a value indicating whether or not the sharedShift is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the startDateTime.
        /// </summary>
        public DateTime StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the endDateTime.
        /// </summary>
        public DateTime EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets the Theme.
        /// </summary>
        public string Theme { get; set; }
    }
}