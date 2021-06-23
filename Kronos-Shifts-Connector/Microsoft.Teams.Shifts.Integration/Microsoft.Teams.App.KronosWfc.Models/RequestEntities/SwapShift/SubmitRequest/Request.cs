// <copyright file="Request.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// Swap shift request object.
    /// </summary>
    [XmlRoot]
    public class Request
    {
        /// <summary>
        /// Gets or sets the Employee request management.
        /// </summary>
        [XmlElement]
        public EmployeeRequestMgmt EmployeeRequestMgmt { get; set; }

        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }
    }
}
