// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.SwapShift.SubmitRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// Request Items for swap shift.
    /// </summary>
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets Swap Shift Request Item.
        /// </summary>
        [XmlElement]
        public SwapShiftRequestItem SwapShiftRequestItem { get; set; }
    }
}
