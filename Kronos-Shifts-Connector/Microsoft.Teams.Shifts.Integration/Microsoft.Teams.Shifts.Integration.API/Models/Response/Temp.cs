// <copyright file="Temp.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Response
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the Temp model.
    /// </summary>
    public class Temp
    {
        /// <summary>
        /// Gets or sets the Odata.
        /// </summary>
        public string Odata { get; set; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "value", Required = Required.Default)]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<TimeOffReason> Value { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}