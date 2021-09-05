// ---------------------------------------------------------------------------
// <copyright file="ResultModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using Microsoft.Extensions.Logging;

    public class ResultModel
    {
        public int CreatedCount { get; set; }
        public int DeletedCount { get; set; }
        public int FailedCount { get; set; }
        public bool HasChanges => (CreatedCount + UpdatedCount + DeletedCount) > 0;
        public int IterationCount { get; set; }
        public LogLevel LogLevel => FailedCount > 0 ? LogLevel.Warning : LogLevel.Information;
        public int SkippedCount { get; set; }

        public int TotalCount
        {
            get
            {
                return CreatedCount + UpdatedCount + DeletedCount + FailedCount + SkippedCount;
            }
        }

        public int UpdatedCount { get; set; }
    }
}
