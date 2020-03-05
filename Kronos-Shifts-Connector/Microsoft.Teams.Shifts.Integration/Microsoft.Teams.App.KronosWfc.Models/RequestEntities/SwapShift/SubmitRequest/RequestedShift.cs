// <copyright file="RequestedShift.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// Requested shift for swap.
    /// </summary>
    public class RequestedShift
    {
        /// <summary>
        /// Gets or sets shift details for swap.
        /// </summary>
        [XmlElement]
        public ShiftRequestItem ShiftRequestItem { get; set; }
    }
}
