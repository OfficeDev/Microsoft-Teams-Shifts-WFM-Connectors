// ---------------------------------------------------------------------------
// <copyright file="CacheModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// The model that is used to store the cached data in blob storage.
    /// </summary>
    /// <typeparam name="T">The type of data being stored, e.g. shifts, open shifts etc.</typeparam>
    public class CacheModel<T>
    {
        public CacheModel()
        {
        }

        public CacheModel(IEnumerable<T> tracked)
        {
            Tracked = tracked.ToList();
        }

        public List<string> Skipped { get; set; } = new List<string>();
        public List<T> Tracked { get; set; } = new List<T>();
    }
}
