// <copyright file="TimeOffPeriods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.RequestEntities.ShiftsToKronos.AddRequest
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class models time off periods.
    /// </summary>
    public class TimeOffPeriods
    {
        /// <summary>
        /// Gets or sets list of time off periods.
        /// </summary>
        [XmlElement("TimeOffPeriod")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<TimeOffPeriod> TimeOffPeriod { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
