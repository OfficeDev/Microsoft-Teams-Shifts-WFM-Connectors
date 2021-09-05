// ---------------------------------------------------------------------------
// <copyright file="YearModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Models
{
    using System.Collections.Generic;

    public class YearModel
    {
        public string TeamId { get; set; }
        public int Year { get; set; }
        public string TimeZoneInfoId { get; set; }

        public IDictionary<string, object> AsDimensions()
        {
            return new Dictionary<string, object>
            {
                { nameof(TeamId), TeamId },
                { nameof(Year), Year }
            };
        }
    }
}
