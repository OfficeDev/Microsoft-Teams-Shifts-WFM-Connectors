// <copyright file="ShiftUserModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models
{
    using System;
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class will model the necessary Shift's user information.
    /// </summary>
    public class ShiftUserModel
    {
#pragma warning disable CA2227 // Collection properties should be read only
                              /// <summary>
                              /// Gets or sets the value in Shifts.
                              /// </summary>
        public List<ShiftUser> Value { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets the NextLink in Shifts for getting more users.
        /// </summary>
        [JsonProperty("@odata.nextLink")]
        public Uri NextLink { get; set; }
    }
}