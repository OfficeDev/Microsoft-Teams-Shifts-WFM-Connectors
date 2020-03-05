// <copyright file="TimeOffPeriods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeOffPeriods.
    /// </summary>
    public class TimeOffPeriods
    {
        /// <summary>
        /// Gets or sets the time off period.
        /// </summary>
        [XmlElement("TimeOffPeriod")]
#pragma warning disable CA1819 // Properties should not return arrays
        public TimeOffPeriod[] TimeOffPerd { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
