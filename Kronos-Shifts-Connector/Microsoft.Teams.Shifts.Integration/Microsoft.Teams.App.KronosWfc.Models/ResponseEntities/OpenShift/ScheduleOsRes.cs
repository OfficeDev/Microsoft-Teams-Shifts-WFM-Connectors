// <copyright file="ScheduleOsRes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the Schedule from Kronos.
    /// </summary>
    public class ScheduleOsRes
    {
        /// <summary>
        /// Gets or sets the ScheduleItems.
        /// </summary>
        [XmlElement(ElementName = "ScheduleItems")]
        public ScheduleItems ScheduleItems { get; set; }

        /// <summary>
        /// Gets or sets the QueryDateSpan.
        /// </summary>
        [XmlAttribute(AttributeName = "QueryDateSpan")]
        public string QueryDateSpan { get; set; }

        /// <summary>
        /// Gets or sets the OrgJobPath.
        /// </summary>
        [XmlAttribute(AttributeName = "OrgJobPath")]
        public string OrgJobPath { get; set; }
    }
}
