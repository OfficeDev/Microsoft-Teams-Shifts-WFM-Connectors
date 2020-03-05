// <copyright file="OpenShiftRequestApproval.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.RequestModels
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class defines the OpenShiftRequestApproval object.
    /// </summary>
    public class OpenShiftRequestApproval
    {
        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        [JsonProperty("message")]
        public string Message { get; set; }
    }
}