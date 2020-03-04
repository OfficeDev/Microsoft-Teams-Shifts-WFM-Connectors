// <copyright file="OfferedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// Offered shift for swap.
    /// </summary>
    public class OfferedShift
    {
        /// <summary>
        /// Gets or sets shift request item.
        /// </summary>
        [XmlElement("ShiftRequestItem")]
        public ShiftRequestItem ShiftRequestItem { get; set; }
    }
}