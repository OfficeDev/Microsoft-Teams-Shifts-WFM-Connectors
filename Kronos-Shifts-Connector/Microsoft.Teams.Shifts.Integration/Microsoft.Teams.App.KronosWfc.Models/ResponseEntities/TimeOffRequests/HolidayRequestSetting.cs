// <copyright file="HolidayRequestSetting.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the HolidayRequestString.
    /// </summary>
    [XmlRoot(ElementName = "HolidayRequestSetting")]
    public class HolidayRequestSetting
    {
        /// <summary>
        /// Gets or sets the string for GeneratePayCodeWithSchedule.
        /// </summary>
        [XmlAttribute(AttributeName = "GeneratePayCodeWithSchedule")]
        public string GeneratePayCodeWithSchedule { get; set; }

        /// <summary>
        /// Gets or sets the string for GeneratePayCodeWithOutSchedule.
        /// </summary>
        [XmlAttribute(AttributeName = "GeneratePayCodeWithOutSchedule")]
        public string GeneratePayCodeWithOutSchedule { get; set; }
    }
}