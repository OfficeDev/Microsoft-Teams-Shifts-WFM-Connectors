// ---------------------------------------------------------------------------
// <copyright file="DictionaryExtensions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation. All rights reserved.
// </copyright>
// ---------------------------------------------------------------------------

namespace WfmTeams.Adapter.Extensions
{
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Defines IDictionary extension methods.
    /// </summary>
    public static class DictionaryExtensions
    {
        public static TValue ReplGetValueOrDefault<TValue>(this IDictionary self, string key)
        {
            return self.Contains(key)
                ? (TValue)self[key]
                : default;
        }

        public static TValue ReplGetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key)
        {
            return self.TryGetValue(key, out var value)
                ? value
                : default;
        }
    }
}
