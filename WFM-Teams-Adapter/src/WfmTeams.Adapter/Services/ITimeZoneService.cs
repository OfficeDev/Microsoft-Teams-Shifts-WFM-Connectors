// ---------------------------------------------------------------------------
// <copyright file="ITimeZoneService.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System.Threading.Tasks;

    public interface ITimeZoneService
    {
        Task<string> GetTimeZoneInfoIdAsync(string wfmTimeZoneName);
    }
}
