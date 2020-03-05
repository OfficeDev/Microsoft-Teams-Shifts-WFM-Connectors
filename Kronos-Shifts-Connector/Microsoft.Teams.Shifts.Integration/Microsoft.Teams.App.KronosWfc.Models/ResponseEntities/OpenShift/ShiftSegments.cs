// <copyright file="ShiftSegments.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the list of ShiftSegments from Kronos.
    /// </summary>
    public class ShiftSegments
    {
        /// <summary>
        /// Gets or sets the ShiftSegment.
        /// </summary>
        [XmlElement(ElementName = "ShiftSegment")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftSegment> ShiftSegment { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
