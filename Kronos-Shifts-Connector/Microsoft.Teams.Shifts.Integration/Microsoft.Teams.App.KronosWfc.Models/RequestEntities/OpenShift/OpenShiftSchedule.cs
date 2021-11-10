// <copyright file="OpenShiftSchedule.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.OpenShift
{
    using System.Xml.Serialization;
    using Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule;

    /// <summary>
    /// This class models the OpenShiftSchedule.
    /// </summary>
    public class OpenShiftSchedule
    {
        /// <summary>
        /// Gets or sets the scheduleItems.
        /// </summary>
        public ScheduleItems ScheduleItems { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute(AttributeName = "OrgJobPath")]
        public string OrgJobPath { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the entity is an open shift.
        /// </summary>
        [XmlAttribute(AttributeName = "IsOpenShift")]
        public bool IsOpenShift { get; set; }
    }
}