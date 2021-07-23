// <copyright file="ApproveMsg.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models an ApproveMsg that would be part of the approval for SwapShift request.
    /// </summary>
    public class ApproveMsg
    {
        /// <summary>
        /// Gets or sets the message to send as part of the approval.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}