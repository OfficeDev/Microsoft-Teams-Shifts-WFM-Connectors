// <copyright file="Period.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeFramePeriod.
    /// </summary>
    public class Period
    {
        /// <summary>
        /// Gets or sets the TimeFramePeriod.
        /// </summary>
        [XmlElement("TimeFramePeriod")]
        public TimeFramePeriod TimeFramePerd { get; set; }
    }
}