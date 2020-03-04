// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the employee request management.
    /// </summary>
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets the query Date span.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
        [XmlElement("Employee")]
        public Employee Employees { get; set; }

        /// <summary>
        /// Gets or sets the request Item.
        /// </summary>
        [XmlElement("RequestItems")]
        public RequestItems RequestItem { get; set; }
    }
}
