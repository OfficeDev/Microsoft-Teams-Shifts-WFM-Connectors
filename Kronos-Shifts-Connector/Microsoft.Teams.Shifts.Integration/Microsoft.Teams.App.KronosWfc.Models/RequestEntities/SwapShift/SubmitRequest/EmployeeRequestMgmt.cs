// <copyright file="EmployeeRequestMgmt.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline;

    /// <summary>
    /// Employee request management for swap shift request.
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
        public RequestItems RequestItems { get; set; }
    }
}