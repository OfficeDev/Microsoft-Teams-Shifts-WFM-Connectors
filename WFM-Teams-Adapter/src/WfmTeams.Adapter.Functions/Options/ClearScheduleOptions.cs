// ---------------------------------------------------------------------------
// <copyright file="ClearScheduleOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Functions.Options
{
    public class ClearScheduleOptions
    {
        public int ClearScheduleBatchSize { get; set; } = 200;
    }
}
