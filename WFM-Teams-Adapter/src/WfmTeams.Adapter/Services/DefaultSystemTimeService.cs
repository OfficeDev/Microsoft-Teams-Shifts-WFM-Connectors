// ---------------------------------------------------------------------------
// <copyright file="DefaultSystemTimeService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;

    public class DefaultSystemTimeService : ISystemTimeService
    {
        public DateTime Today => DateTime.Today;
        public DateTime UtcNow => DateTime.UtcNow;
    }
}
