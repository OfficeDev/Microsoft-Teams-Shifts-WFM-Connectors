// <copyright file="EmployeeGlobalTimeOffRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the time off request Item.
    /// </summary>
    public class EmployeeGlobalTimeOffRequestItem
    {
        /// <summary>
        /// Gets or sets the time off request periods.
        /// </summary>
        [XmlElement("TimeOffPeriods")]
        public TimeOffPeriods TimeOffPeriodsList { get; set; }

        /// <summary>
        /// Gets or sets the time off request creation time.
        /// </summary>
        [XmlAttribute]
        public string CreationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the time off request status name.
        /// </summary>
        [XmlAttribute]
        public string StatusName { get; set; }

        /// <summary>
        /// Gets or sets the time off request id.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets for which the time off is requested for.
        /// </summary>
        [XmlAttribute]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the time off request holiday settings.
        /// </summary>
        [XmlElement("HolidayRequestSettings")]
        public HolidayRequestSettings HolidayRequestSettingList { get; set; }
    }
}
