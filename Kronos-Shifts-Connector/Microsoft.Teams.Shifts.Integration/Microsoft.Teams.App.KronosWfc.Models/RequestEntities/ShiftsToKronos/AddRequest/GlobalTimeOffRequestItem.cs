// <copyright file="GlobalTimeOffRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Time off request details.
    /// </summary>
    public class GlobalTimeOffRequestItem
    {
        /// <summary>
        /// Gets or sets the Time Off perirods.
        /// </summary>
        [XmlElement("TimeOffPeriods")]
        public TimeOffPeriods TimeOffPeriods { get; set; }

        /// <summary>
        /// Gets or sets the type of Time Off.
        /// </summary>
        [XmlElement("RequestFor")]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the Employee details.
        /// </summary>
        [XmlElement("Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the Comments associated with time off.
        /// </summary>
        [XmlElement("Comments")]
        public Comments Comments { get; set; }
    }
}
