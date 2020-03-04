// <copyright file="SwapShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.ApproveDecline.RequestManagementApproveDecline;

    /// <summary>
    /// SwapShiftReeuqestItem for swap shift.
    /// </summary>
    public class SwapShiftRequestItem
    {
        /// <summary>
        /// Gets or sets employee with whome the request is made.
        /// </summary>
        [XmlElement]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the shift which is offered for swap.
        /// </summary>
        [XmlElement]
        public OfferedShift OfferedShift { get; set; }

        /// <summary>
        /// Gets or sets the shift which is requested for swap.
        /// </summary>
        [XmlElement]
        public RequestedShift RequestedShift { get; set; }

        /// <summary>
        /// Gets or sets requestfor.
        /// </summary>
        [XmlAttribute]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the StatusName.
        /// </summary>
        [XmlAttribute]
        public string StatusName { get; set; }
    }
}