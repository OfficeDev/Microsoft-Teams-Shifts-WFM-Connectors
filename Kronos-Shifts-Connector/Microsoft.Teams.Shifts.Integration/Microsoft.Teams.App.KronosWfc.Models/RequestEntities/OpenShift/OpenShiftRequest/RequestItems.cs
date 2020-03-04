// <copyright file="RequestItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the RequestItems.
    /// </summary>
    public class RequestItems
    {
        /// <summary>
        /// Gets or sets the GlobalOpenShiftRequestItem.
        /// </summary>
        [XmlElement]
        public GlobalOpenShiftRequestItem GlobalOpenShiftRequestItem { get; set; }
    }
}