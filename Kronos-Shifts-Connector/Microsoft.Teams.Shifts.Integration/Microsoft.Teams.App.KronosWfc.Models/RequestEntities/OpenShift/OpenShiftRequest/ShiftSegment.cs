// <copyright file="ShiftSegment.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ShiftSegment.
    /// </summary>
    [XmlRoot]
    public class ShiftSegment
    {
        /// <summary>
        /// Gets or sets the necessary shift segments.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegment")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ResponseEntities.OpenShift.ShiftSegment> ShiftSegments { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}