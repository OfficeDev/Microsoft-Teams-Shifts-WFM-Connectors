// <copyright file="Schedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Shifts
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common;

    /// <summary>
    /// A model for the schedule element of a request.
    /// </summary>
    public class Schedule
    {
        /// <summary>
        /// The date span to query for the request.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// The job for the request.
        /// </summary>
        [XmlAttribute]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// The employees that the request concerns.
        /// </summary>
        [XmlElement]
        public Employees Employees { get; set; }

        /// <summary>
        /// The specific schedule items containing information about the shift.
        /// </summary>
        [XmlElement]
        public ScheduleItems ScheduleItems { get; set; }
    }
}
