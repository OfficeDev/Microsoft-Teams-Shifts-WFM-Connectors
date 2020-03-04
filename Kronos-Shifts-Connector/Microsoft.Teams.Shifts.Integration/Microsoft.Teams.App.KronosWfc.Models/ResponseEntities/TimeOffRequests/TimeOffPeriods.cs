// <copyright file="TimeOffPeriods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeOffPeriods.
    /// </summary>
    [XmlRoot(ElementName = "TimeOffPeriods")]
    public class TimeOffPeriods
    {
        /// <summary>
        /// Gets or sets a single TimeOffPeriod.
        /// </summary>
        [XmlElement(ElementName = "TimeOffPeriod")]
        public TimeOffPeriod TimeOffPeriod { get; set; }
    }
}