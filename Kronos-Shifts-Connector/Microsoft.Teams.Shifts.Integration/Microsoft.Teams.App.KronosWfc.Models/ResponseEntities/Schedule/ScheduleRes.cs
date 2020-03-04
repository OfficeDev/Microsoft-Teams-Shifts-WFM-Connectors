// <copyright file="ScheduleRes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Schedule
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Schedule.
    /// </summary>
    public class ScheduleRes
    {
        // [XmlArray]

        /// <summary>
        /// Gets or sets the Employees.
        /// </summary>
#pragma warning disable CA2227 // Collection properties should be read only
        public List<PersonIdentity> Employees { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the ScheduleItems.
        /// </summary>
        [XmlElement]
        public ScheduleItems ScheduleItems { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }
    }
}