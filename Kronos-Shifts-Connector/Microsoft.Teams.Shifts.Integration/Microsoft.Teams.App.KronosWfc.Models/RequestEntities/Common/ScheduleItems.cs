// <copyright file="ScheduleItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.Common
{
    using System;
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the Schedule Items.
    /// </summary>
    public class ScheduleItems
    {
        /// <summary>
        /// Gets or sets the ScheduleShifts.
        /// </summary>
        [XmlElement]
        public List<ScheduleShift> ScheduleShift { get; set; }
    }
}