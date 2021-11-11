// ---------------------------------------------------------------------------
// <copyright file="ISystemTimeService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;

    public interface ISystemTimeService
    {
        DateTime Today { get; }
        DateTime UtcNow { get; }
    }
}
