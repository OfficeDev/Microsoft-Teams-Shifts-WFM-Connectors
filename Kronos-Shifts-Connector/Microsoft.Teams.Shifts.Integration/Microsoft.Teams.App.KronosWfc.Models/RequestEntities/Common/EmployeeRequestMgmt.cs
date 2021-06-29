// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System.Xml.Serialization;

    /// <summary>
    /// Employee request management tag.
    /// </summary>
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets QueryDateSpan for request.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets employee for request.
        /// </summary>
        [XmlElement]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets requestitems for swap shift.
        /// </summary>
        [XmlElement]
        public RequestIds RequestIds { get; set; }
    }
}