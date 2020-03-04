// <copyright file="ApprovalTimeOffPeriods.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the ApprovalTimeOffPeriods.
    /// </summary>
    [XmlRoot(ElementName = "ApprovalTimeOffPeriods")]
    public class ApprovalTimeOffPeriods
    {
        /// <summary>
        /// Gets or sets the TimeOffPeriod.
        /// </summary>
        [XmlElement(ElementName = "TimeOffPeriod")]
        public TimeOffPeriod TimeOffPeriod { get; set; }
    }
}