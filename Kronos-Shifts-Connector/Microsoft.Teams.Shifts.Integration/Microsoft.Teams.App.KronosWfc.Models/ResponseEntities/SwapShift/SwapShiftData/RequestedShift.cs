// <copyright file="RequestedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestedShift.
    /// </summary>
    [XmlRoot(ElementName = "RequestedShift")]
    public class RequestedShift
    {
        /// <summary>
        /// Gets or sets the ShiftRequestItem.
        /// </summary>
        [XmlElement(ElementName = "ShiftRequestItem")]
        public ShiftRequestItem ShiftRequestItem { get; set; }
    }
}