// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;

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
        [XmlElement]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the RequestItems.
        /// </summary>
        [XmlElement]
        public RequestItems RequestItems { get; set; }
    }
}