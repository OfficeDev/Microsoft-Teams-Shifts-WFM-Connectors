// ---------------------------------------------------------------------------
// <copyright file="InMemoryTimeOffCacheService .cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Services
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using WfmTeams.Adapter.Models;

    public class InMemoryTimeOffCacheService : ITimeOffCacheService
    {
        private readonly ConcurrentDictionary<(string, DateTime), CacheModel<TimeOffModel>> _timeOff = new ConcurrentDictionary<(string, DateTime), CacheModel<TimeOffModel>>();

        private readonly ConcurrentDictionary<(string, DateTime), List<TimeOffModel>> _timeOffSource = new ConcurrentDictionary<(string, DateTime), List<TimeOffModel>>();

        public Task DeleteTimeOffAsync(string teamId, DateTime weekStartDate)
        {
            _timeOff.TryRemove((teamId, weekStartDate), out _);

            return Task.CompletedTask;
        }

        public Task<CacheModel<TimeOffModel>> LoadTimeOffAsync(string teamId, DateTime weekStartDate)
        {
            if (_timeOff.TryGetValue((teamId ?? string.Empty, weekStartDate), out var timeOffCache))
            {
                return Task.FromResult(timeOffCache);
            }
            else
            {
                return Task.FromResult(new CacheModel<TimeOffModel>());
            }
        }

        public Task SaveTimeOffAsync(string teamId, DateTime weekStartDate, CacheModel<TimeOffModel> timeOffModel)
        {
            _timeOff[(teamId, weekStartDate)] = timeOffModel;

            return Task.CompletedTask;
        }

        public Task SaveTimeOffAsync(string teamId, DateTime weekStartDate, List<TimeOffModel> timeOffSource)
        {
            _timeOffSource[(teamId, weekStartDate)] = timeOffSource;

            return Task.CompletedTask;
        }
    }
}
