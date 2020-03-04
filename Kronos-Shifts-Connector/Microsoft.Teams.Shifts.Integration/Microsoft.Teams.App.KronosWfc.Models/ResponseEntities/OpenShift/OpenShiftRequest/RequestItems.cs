// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestItems.
    /// </summary>
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets the SwapShiftRequestItem.
        /// </summary>
        [XmlElement]
        public GlobalOpenShiftRequestItem SwapShiftRequestItem { get; set; }
    }
}