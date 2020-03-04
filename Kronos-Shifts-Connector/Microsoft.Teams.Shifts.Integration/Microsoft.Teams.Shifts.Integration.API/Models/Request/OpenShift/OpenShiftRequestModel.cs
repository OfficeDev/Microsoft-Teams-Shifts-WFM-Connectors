// <copyright file="OpenShiftRequestModel.cs" company="Microsoft">
// Copyright (c) Microsoft. All rights reserved.
// </copyright>

namespace Microsoft.Teams.Shifts.Integration.BusinessLogic.Models.RequestModels.OpenShift
{
    using Newtonsoft.Json;

    /// <summary>
    /// This class models the OpenShift.
    /// </summary>
    public class OpenShiftRequestModel
    {
        /// <summary>
        /// Gets or sets the SchedulingGroupId.
        /// </summary>
        [JsonProperty("schedulingGroupId")]
        public string SchedulingGroupId { get; set; }

        /// <summary>
        /// Gets or sets the SharedOpenShfit.
        /// </summary>
        [JsonProperty("sharedOpenShift")]
        public OpenShiftItem SharedOpenShift { get; set; }

        /// <summary>
        /// Gets or sets the DraftOpenShift.
        /// </summary>
        [JsonProperty("draftOpenShift")]
        public OpenShiftItem DraftOpenShift { get; set; }

        /// <summary>
        /// Gets or sets the hash of open shift.
        /// </summary>
        public string KronosUniqueId { get; set; }
    }
}