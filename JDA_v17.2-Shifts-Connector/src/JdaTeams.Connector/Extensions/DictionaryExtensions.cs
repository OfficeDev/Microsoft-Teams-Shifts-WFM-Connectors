using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace JdaTeams.Connector.Extensions
{
    public static class DictionaryExtensions
    {
        public static TValue GetValueOrDefault<TValue>(this IDictionary self, string key)
        {
            return self.Contains(key) 
                ? (TValue)self[key]
                : default;
        }

        public static TValue GetValueOrDefault<TKey, TValue>(this IDictionary<TKey, TValue> self, TKey key)
        {
            return self.TryGetValue(key, out var value) 
                ? value
                : default;
        }

        public static TValue GetValueOrCreate<TKey, TValue>(this ConcurrentDictionary<TKey, TValue> self, TKey key) where TValue : new()
        {
            return self.GetOrAdd(key, _ => new TValue());
        }

        public static void AddRange<TKey, TValue>(this IDictionary<TKey, TValue> self, IEnumerable<TValue> values, Func<TValue, TKey> keyAccessor)
        {
            foreach (var value in values)
            {
                var key = keyAccessor(value);

                self[key] = value;
            }
        }
    }
}
