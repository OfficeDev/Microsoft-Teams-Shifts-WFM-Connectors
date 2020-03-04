// <copyright file="HolidayRequestSetting.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the holiday request setting.
    /// </summary>
    public class HolidayRequestSetting
    {
        /// <summary>
        /// Gets or sets the paycode with schedule.
        /// </summary>
        [XmlAttribute]
        public string GeneratePayCodeWithSchedule { get; set; }

        /// <summary>
        /// Gets or sets the paycode without schedule.
        /// </summary>
        [XmlAttribute]
        public string GeneratePayCodeWithOutSchedule { get; set; }
    }
}
