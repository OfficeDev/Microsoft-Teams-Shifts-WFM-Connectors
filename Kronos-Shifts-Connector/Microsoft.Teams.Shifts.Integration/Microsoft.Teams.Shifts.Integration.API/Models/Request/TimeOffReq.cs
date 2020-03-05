// <copyright file="TimeOffReq.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using Newtonsoft.Json;

    /// <summary>
    /// Time off entity details.
    /// </summary>
    public class TimeOffReq
    {
        /// <summary>
         /// Gets or sets shared time off.
         /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "sharedTimeOff", Required = Required.Default)]
        public TimeOffItem SharedTimeOff { get; set; }

        /// <summary>
        /// Gets or sets draft time off.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "draftTimeOff", Required = Required.Default)]
        public TimeOffItem DraftTimeOff { get; set; }

        /// <summary>
        /// Gets or sets user id.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "userId", Required = Required.Default)]
        public string UserId { get; set; }
    }
}