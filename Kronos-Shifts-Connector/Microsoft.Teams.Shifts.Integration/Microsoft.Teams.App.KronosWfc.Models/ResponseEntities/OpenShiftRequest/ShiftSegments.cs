// <copyright file="ShiftSegments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftSegments.
    /// </summary>
    [XmlRoot(ElementName = "ShiftSegments")]
    public class ShiftSegments
    {
        /// <summary>
        /// Gets or sets the ShiftSegment.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegment")]
        public ShiftSegment ShiftSegment { get; set; }
    }
}