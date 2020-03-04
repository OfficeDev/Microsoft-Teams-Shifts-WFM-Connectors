// <copyright file="HolidayRequestSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the HolidayRequestSettings.
    /// </summary>
    [XmlRoot(ElementName = "HolidayRequestSettings")]
    public class HolidayRequestSettings
    {
        /// <summary>
        /// Gets or sets the HolidayRequestSetting.
        /// </summary>
        [XmlElement(ElementName = "HolidayRequestSetting")]
        public HolidayRequestSetting HolidayRequestSetting { get; set; }
    }
}