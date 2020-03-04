// <copyright file="ShiftUser.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using Newtonsoft.Json;

    /// <summary>
    /// The class models the shift user details.
    /// </summary>
    public class ShiftUser
    {
        /// <summary>
        /// Gets or sets the AAD id of the User in Shifts.
        /// </summary>
        [JsonProperty("Id")]
        public string ShiftAADObjectId { get; set; }

        /// <summary>
        /// Gets or sets Name of the User in Shifts.
        /// </summary>
        [JsonProperty("DisplayName")]
        public string ShiftUserDisplayName { get; set; }

        /// <summary>
        /// Gets or sets UPN of the User in Shifts.
        /// </summary>
        [JsonProperty("UserPrincipalName")]
        public string ShiftUserPrincipalName { get; set; }
    }
}