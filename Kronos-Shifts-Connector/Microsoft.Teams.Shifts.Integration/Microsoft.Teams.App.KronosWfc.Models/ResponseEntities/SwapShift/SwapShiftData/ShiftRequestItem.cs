// <copyright file="ShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftRequestItem.
    /// </summary>
    [XmlRoot(ElementName = "ShiftRequestItem")]
    public class ShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement(ElementName = "Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute(AttributeName = "OrgJobPath")]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the StartDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "StartDateTime")]
        public string StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets the EndDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "EndDateTime")]
        public string EndDateTime { get; set; }
    }
}