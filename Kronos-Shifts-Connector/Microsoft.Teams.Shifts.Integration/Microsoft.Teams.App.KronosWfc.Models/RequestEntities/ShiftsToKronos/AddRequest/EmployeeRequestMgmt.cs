// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the EmployeeRequestMgmt.
    /// </summary>
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement("Employee")]
        public Employee Employees { get; set; }

        /// <summary>
        /// Gets or sets the RequestItems.
        /// </summary>
        [XmlElement("RequestItems")]
        public RequestItems RequestItems { get; set; }
    }
}