// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the EmployeeRequestMgmt.
    /// </summary>
    [XmlRoot(ElementName = "EmployeeRequestMgmt")]
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets the RequestItems.
        /// </summary>
        [XmlElement(ElementName = "RequestItems")]
        public RequestItems RequestItems { get; set; }

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement(ElementName = "Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }
    }
}