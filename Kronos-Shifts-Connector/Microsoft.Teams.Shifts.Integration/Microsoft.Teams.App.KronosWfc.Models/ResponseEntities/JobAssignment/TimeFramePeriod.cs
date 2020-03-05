// <copyright file="TimeFramePeriod.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.JobAssignment
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the TimeFramePeriod.
    /// </summary>
    public class TimeFramePeriod
    {
        /// <summary>
        /// Gets or sets the PeriodDateSpan.
        /// </summary>
        [XmlAttribute]
        public string PeriodDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the TimeFrameName.
        /// </summary>
        [XmlAttribute]
        public string TimeFrameName { get; set; }
    }
}