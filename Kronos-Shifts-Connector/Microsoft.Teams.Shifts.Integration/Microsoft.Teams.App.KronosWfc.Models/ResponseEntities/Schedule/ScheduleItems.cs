// <copyright file="ScheduleItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.Schedule
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ScheduleItems.
    /// </summary>
    public class ScheduleItems
    {
        /// <summary>
        /// Gets or sets the necessary items.
        /// </summary>
        [XmlElement("SchedulePayCodeEdit", typeof(SchedulePayCodeEdit))]
        [XmlElement("ScheduleShift", typeof(ScheduleShift))]
#pragma warning disable CA1819 // Properties should not return arrays
        public object[] Items { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}