// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.SwapShift.FetchApprovals.SwapShiftData
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestItems.
    /// </summary>
    [XmlRoot(ElementName = "RequestItems")]
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets the SwapShiftRequestItem.
        /// </summary>
        [XmlElement(ElementName = "SwapShiftRequestItem")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<SwapShiftRequestItem> SwapShiftRequestItem { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}