// <copyright file="TimeOffItem.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using Newtonsoft.Json;

    /// <summary>
    /// TimeOffItem entity for time off details.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public partial class TimeOffItem : ScheduleEntity
    {
        /// <summary>
        /// Gets or sets timeOffReasonId.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "timeOffReasonId", Required = Required.Default)]
        public string TimeOffReasonId { get; set; }
    }
}