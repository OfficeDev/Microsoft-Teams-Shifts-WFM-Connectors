// <copyright file="SwapShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.FetchApproval
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest.RequestManagement;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest;

    /// <summary>
    /// This class models the SwapShiftRequestItem for swap shift.
    /// </summary>
    public class SwapShiftRequestItem
    {
        /// <summary>
        /// Gets or sets employee with whome the request is made.
        /// </summary>
        [XmlElement(ElementName = "Employee")]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the CreationDateTime.
        /// </summary>
        [XmlAttribute(AttributeName = "CreationDateTime")]
        public string CreationDateTime { get; set; }

        /// <summary>
        /// Gets or sets the shift which is offered for swap.
        /// </summary>
        [XmlElement(ElementName = "OfferedShift")]
        public OfferedShift OfferedShift { get; set; }

        /// <summary>
        /// Gets or sets the shift which is requested for swap.
        /// </summary>
        [XmlElement(ElementName = "RequestedShift")]
        public RequestedShift RequestedShift { get; set; }

        /// <summary>
        /// Gets or sets requestfor.
        /// </summary>
        [XmlAttribute(AttributeName = "RequestFor")]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the StatusName.
        /// </summary>
        [XmlAttribute(AttributeName = "StatusName")]
        public string StatusName { get; set; }

        /// <summary>
        /// Gets or sets the Id.
        /// </summary>
        [XmlAttribute]
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the RequestStatusChanges.
        /// </summary>
        [XmlElement(ElementName = "RequestStatusChanges")]
        public RequestStatusChanges RequestStatusChanges { get; set; }

        /// <summary>
        /// Gets or sets the CreatedByUser.
        /// </summary>
        [XmlElement(ElementName = "CreatedByUser")]
        public Employee CreatedByUser { get; set; }
    }
}