// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.TimeOffRequests;

    /// <summary>
    /// Employee request management for swap shift request.
    /// </summary>
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets QueryDateSpan for request.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets employee for request.
        /// </summary>
        [XmlElement(ElementName = "Employees")]
        public Employees Employee { get; set; }

        /// <summary>
        /// Gets or sets requestitems for swap shift.
        /// </summary>
        [XmlElement(ElementName ="RequestItems")]
        public RequestItems RequestItems { get; set; }
    }
}