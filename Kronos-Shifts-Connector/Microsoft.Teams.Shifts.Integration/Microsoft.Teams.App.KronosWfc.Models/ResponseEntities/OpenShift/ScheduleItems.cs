// <copyright file="ScheduleItems.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.OpenShift
{
    using System.Collections.Generic;
    using System.Xml.Serialization;

    /// <summary>
    /// This class is used to parse the response from the ScheduleItems from Kronos.
    /// </summary>
    public class ScheduleItems
    {
        /// <summary>
        /// Gets or sets the ScheduleShift.
        /// </summary>
        [XmlElement(ElementName = "ScheduleShift")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ScheduleShift> ScheduleShift { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}
