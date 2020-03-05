// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Employee requests.
    /// </summary>
    public class EmployeeRequestMgmt
    {
        /// <summary>
        /// Gets or sets person query date span of a request.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets employee details.
        /// </summary>
        [XmlElement("Employee")]
        public Employee Employees { get; set; }

        /// <summary>
        /// Gets or sets request Ids.
        /// </summary>
        [XmlElement("RequestIds")]
        public RequestIds RequestIds { get; set; }
    }
}
