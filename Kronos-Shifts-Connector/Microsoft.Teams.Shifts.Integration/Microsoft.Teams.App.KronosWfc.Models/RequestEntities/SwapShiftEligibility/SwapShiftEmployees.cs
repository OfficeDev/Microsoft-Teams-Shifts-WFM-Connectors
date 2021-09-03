// <copyright file="SwapShiftEmployees.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShiftEligibility
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// Swap Shift Employees Tag.
    /// </summary>
    public class SwapShiftEmployees
    {
        /// <summary>
        /// Gets or Sets the start time for the requestor's shift.
        /// </summary>
        [XmlAttribute]
        public string StartTime { get; set; }

        /// <summary>
        /// Gets or Sets the end time for the requestor's shift.
        /// </summary>
        [XmlAttribute]
        public string EndTime { get; set; }

        /// <summary>
        /// Gets or Sets the query date for the requestor's shift.
        /// </summary>
        [XmlAttribute]
        public string QueryDate { get; set; }

        /// <summary>
        /// Gets or Sets the date for the potential requested shift.
        /// </summary>
        [XmlAttribute]
        public string ShiftSwapDate { get; set; }

        /// <summary>
        /// Gets or Sets the employee that is requesting the shift.
        /// </summary>
        [XmlElement]
        public Employee Employee { get; set; }
    }
}
