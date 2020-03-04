// <copyright file="ScheduleEntity.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.API.Models.Request
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// The type ScheduleEntity.
    /// </summary>
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    [JsonConverter(typeof(DerivedTypeConverter))]
    public partial class ScheduleEntity
    {
        /// <summary>
        /// Gets or sets startDateTime.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "startDateTime", Required = Required.Default)]
        public DateTimeOffset? StartDateTime { get; set; }

        /// <summary>
        /// Gets or sets endDateTime.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "endDateTime", Required = Required.Default)]
        public DateTimeOffset? EndDateTime { get; set; }

        /// <summary>
        /// Gets or sets additional data.
        /// </summary>
        [JsonExtensionData(ReadData = true)]
#pragma warning disable CA2227 // Collection properties should be read only
        public IDictionary<string, object> AdditionalData { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only

        /// <summary>
        /// Gets or sets @odata.type.
        /// </summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore, PropertyName = "@odata.type", Required = Required.Default)]
        public string ODataType { get; set; }
    }
}