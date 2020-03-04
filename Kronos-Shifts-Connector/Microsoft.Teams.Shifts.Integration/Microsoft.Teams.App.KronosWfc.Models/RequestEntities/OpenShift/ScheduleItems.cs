// <copyright file="ScheduleItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Schedule
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ScheduleItems.
    /// </summary>
    public class ScheduleItems
    {
        /// <summary>
        /// Gets or sets the necessary items.
        /// </summary>
        [XmlElement("ScheduleShift", typeof(ScheduleShift))]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ScheduleShift> ScheduleShifts { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}