// <copyright file="GlobalOpenShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift.OpenShiftRequest
{
    using System.Collections.Generic;
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest;

    /// <summary>
    /// This class models the GlobalOpenShiftRequestItem.
    /// </summary>
    public class GlobalOpenShiftRequestItem
    {
        /// <summary>
        /// Gets or sets the Employee.
        /// </summary>
        [XmlElement]
        public Employee Employee { get; set; }

        /// <summary>
        /// Gets or sets the RequestFor.
        /// </summary>
        [XmlAttribute]
        public string RequestFor { get; set; }

        /// <summary>
        /// Gets or sets the ShiftDate.
        /// </summary>
        [XmlAttribute]
        public string ShiftDate { get; set; }

        /// <summary>
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement]
#pragma warning disable CA2227 // Collection properties should be read only
        public ShiftSegments ShiftSegments { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}