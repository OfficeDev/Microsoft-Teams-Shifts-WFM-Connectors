// <copyright file="GlobalOpenShiftRequestItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift.OpenShiftRequest
{
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
        /// Gets or sets the ShiftSegments.
        /// </summary>
        [XmlElement]
        public ShiftSegments ShiftSegments { get; set; }
    }
}