// <copyright file="SwapShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the SwapShiftRequestItem.
    /// </summary>
    [XmlRoot(ElementName = "SwapShiftRequestItem")]
    public class SwapShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the OfferedShift.
        /// </summary>
        [XmlElement(ElementName = "OfferedShift")]
        public OfferedShift OfferedShift { get; set; }

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement(ElementName = "Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the CreatedByUser.
        /// </summary>
        [XmlElement(ElementName = "CreatedByUser")]
        public CreatedByUser CreatedByUser { get; set; }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        [XmlElement(ElementName = "Comments")]
        public Comments Comments { get; set; }

        /// <summary>
        /// Gets or sets the RequestedShift.
        /// </summary>
        [XmlElement(ElementName = "RequestedShift")]
        public RequestedShift RequestedShift { get; set; }

        /// <summary>
        /// Gets or sets the RequestStatusChange.
        /// </summary>
        [XmlElement(ElementName = "RequestStatusChanges")]
        public RequestStatusChanges RequestStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the CreationDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "CreationDateTime")]
        public string CreationDateTime { get; set; }

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