// ---------------------------------------------------------------------------
// <copyright file="BusinessUnitModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    /// <summary>
    /// Defines the model of a business unit being the heirarchical entity in the WFM provider that
    /// maps to a team in Microsoft Teams.
    /// </summary>
    public class BusinessUnitModel
    {
        /// <summary>
        /// The ID of the business unit in the WFM system
        /// </summary>
        public string WfmBuId { get; set; }

        /// <summary>
        /// The name of the business unit in the WFM system
        /// </summary>
        public string WfmBuName { get; set; }

        /// <summary>
        /// The .NET time zone identifier for the business unit in the WFM system
        /// </summary>
        public string TimeZoneInfoId { get; set; }
    }
}
