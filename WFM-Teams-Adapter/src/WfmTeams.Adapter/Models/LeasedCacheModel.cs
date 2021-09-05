// ---------------------------------------------------------------------------
// <copyright file="LeasedCacheModel.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Models
{
    using System.Collections.Generic;
    using Newtonsoft.Json;

    /// <summary>
    /// Extends the cache model with support for leasing in blob storage which allows the adapter to
    /// lock the blob storage entity during processing.
    /// </summary>
    /// <typeparam name="T">The type of entity being stored.</typeparam>
    public class LeasedCacheModel<T> : CacheModel<T>
    {
        public LeasedCacheModel()
            : base()
        {
        }

        public LeasedCacheModel(IEnumerable<T> tracked)
            : base(tracked)
        {
        }

        [JsonIgnore]
        public string LeaseId { get; set; }
    }
}
