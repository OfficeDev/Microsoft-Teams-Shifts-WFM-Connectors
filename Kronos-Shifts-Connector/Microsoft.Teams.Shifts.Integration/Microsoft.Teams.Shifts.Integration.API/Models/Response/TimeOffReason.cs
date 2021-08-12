// <copyright file="TimeOffReason.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response
{
    /// <summary>
    /// This class models the TimeOffReason.
    /// </summary>
    public class TimeOffReason
    {
        /// <summary>
        /// Gets or sets the Odata.
        /// </summary>
        public string Odata { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the CreatedDateTime.
        /// </summary>
        public object CreatedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the LastModifiedDateTime.
        /// </summary>
        public object LastModifiedDateTime { get; set; }

        /// <summary>
        /// Gets or sets the DisplayName.
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Gets or sets the IconType.
        /// </summary>
        public string IconType { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether or not a mapping is active.
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Gets or sets the LastModifiedBy.
        /// </summary>
        public object LastModifiedBy { get; set; }
    }
}