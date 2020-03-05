// <copyright file="IntegrationApiResponseModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.ResponseModels
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the api response to send back to Shifts.
    /// </summary>
    public class IntegrationApiResponseModel
    {
        /// <summary>
        /// Gets or sets the responses.
        /// </summary>
        [JsonProperty("responses")]
#pragma warning disable CA2227 // Collection properties should be read only
        public List<ShiftsIntegResponse> ShiftsIntegResponses { get; set; }
#pragma warning restore CA2227 // Collection properties should be read only
    }
}