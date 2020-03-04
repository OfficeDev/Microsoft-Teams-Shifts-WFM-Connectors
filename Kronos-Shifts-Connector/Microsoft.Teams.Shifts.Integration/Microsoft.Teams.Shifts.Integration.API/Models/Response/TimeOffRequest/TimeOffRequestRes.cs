// <copyright file="TimeOffRequestRes.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response.TimeOffRequest
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Time Off Shift.
    /// </summary>
    public class TimeOffRequestRes
    {
        /// <summary>
        /// Gets or sets the NextLink in Shifts for getting more timeoff requests.
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public Uri NextLink { get; set; }

        /// <summary>
        /// Gets or sets the Time off Request item.
        /// </summary>
        [JsonProperty("value")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<TimeOffRequestItem> TORItem { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}