// <copyright file="HolidayRequestSettings.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.App.KronosWfc.Models.ResponseEntities.ShiftsToKronos.TimeOffRequests
{
    using System.Xml.Serialization;

    /// <summary>
    /// This class models the holiday request settings.
    /// </summary>
    public class HolidayRequestSettings
    {
        /// <summary>
        /// Gets or sets the holiday request setting list.
        /// </summary>
        [XmlElement("HolidayRequestSetting")]
#pragma warning disable CA1819 // Properties should not return arrays
        public HolidayRequestSetting[] HolidayRequestSettingsList { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
    }
}
