// <copyright file="EmployeeGlobalOpenShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the EmployeeGlobalOpenShiftRequestItem.
    /// </summary>
    [XmlRoot(ElementName = "EmployeeGlobalOpenShiftRequestItem")]
    public class EmployeeGlobalOpenShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegments")]
        public ShiftSegments ShiftSegments { get; set; }

        /// <summary>
        /// Gets or sets the CreationDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "CreationDateTime")]
        public string CreationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the ShiftDate.
        /// </summary>
        [XmlAttribute(AttributeName = "ShiftDate")]
        public string ShiftDate { get; set; }

        /// <summary>
        /// Gets or sets the StatusName.
        /// </summary>
        [XmlAttribute(AttributeName = "StatusName")]
        public string StatusName { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlAttribute(AttributeName = "Id")]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlAttribute(AttributeName = "RequestFor")]
        public string RequestFor { get; set; }
    }
}