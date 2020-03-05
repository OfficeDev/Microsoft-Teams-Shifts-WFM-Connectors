// <copyright file="ShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline;

    /// <summary>
    /// Shift details for swap shift.
    /// </summary>
    public class ShiftRequestItem
    {
        /// <summary>
        /// Gets or sets StartDateTime of shift.
        /// </summary>
        [XmlAttribute]
        public string StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets EndDateTime of shift.
        /// </summary>
        [XmlAttribute]
        public string EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets OrgJobPath of shift.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets Employee for request swap shift.
        /// </summary>
        [XmlElement("Employee")]
        public Employee Employee { get; set; }
    }
}