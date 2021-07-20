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
        /// The Query Date Span for the request.
        /// </summary>
        [XmlAttribute]
        public string QueryDateSpan { get; set; }

        [XmlAttribute]
        public string OrgJobPath { get; set; }

        [XmlElement]
        public Employees Employees { get; set; }

        [XmlElement]
        public ScheduleItems ScheduleItems { get; set; }
    }
}
