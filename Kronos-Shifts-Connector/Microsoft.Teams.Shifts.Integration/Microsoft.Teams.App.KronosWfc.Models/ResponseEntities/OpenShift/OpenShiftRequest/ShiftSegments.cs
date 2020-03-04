// <copyright file="ShiftSegments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.OpenShiftRequest
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftSegments.
    /// </summary>
    public class ShiftSegments
    {
        /// <summary>
        /// Gets or sets the ShiftSegment.
        /// </summary>
        [XmlElement]
        public ShiftSegment ShiftSegment { get; set; }
    }
}