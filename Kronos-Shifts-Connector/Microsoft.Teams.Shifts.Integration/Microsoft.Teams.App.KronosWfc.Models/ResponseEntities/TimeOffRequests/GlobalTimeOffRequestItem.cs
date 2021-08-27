// <copyright file="GlobalTimeOffRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.CommonEntities;

    /// <summary>
    /// This class models the GlobalTimeOffRequestItem.
    /// </summary>
    [XmlRoot(ElementName = "GlobalTimeOffRequestItem")]
    public class GlobalTimeOffRequestItem
    {
        /// <summary>
        /// Gets or sets the HolidayRequestSettings.
        /// </summary>
        [XmlElement(ElementName = "HolidayRequestSettings")]
        public HolidayRequestSettings HolidayRequestSettings { get; set; }

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
        /// Gets or sets the Comments.
        /// </summary>
        [XmlElement(ElementName = "Comments")]
        public Comments Comments { get; set; }

        /// <summary>
        /// Gets or sets the TimeOffPeriods.
        /// </summary>
        [XmlElement(ElementName = "TimeOffPeriods")]
        public TimeOffPeriods TimeOffPeriods { get; set; }

        /// <summary>
        /// Gets or sets the RequestStatusChanges.
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

        /// <summary>
        /// Gets or sets the ApprovalTimeOffPeriods.
        /// </summary>
        [XmlElement(ElementName = "ApprovalTimeOffPeriods")]
        public ApprovalTimeOffPeriods ApprovalTimeOffPeriods { get; set; }
    }
}