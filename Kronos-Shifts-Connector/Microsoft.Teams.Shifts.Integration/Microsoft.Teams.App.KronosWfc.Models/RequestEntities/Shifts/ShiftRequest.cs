// <copyright file="ShiftRequest.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// A model for the Shift CRUD requests.
    /// </summary>
    [XmlRoot("Request")]
    public class ShiftRequest
    {
        /// <summary>
        /// Gets or sets the Action.
        /// </summary>
        [XmlAttribute]
        public string Action { get; set; }

        /// <summary>
        /// Gets or sets the Schedule object.
        /// </summary>
        [XmlElement]
        public Schedule Schedule { get; set; }
    }
}
