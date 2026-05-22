using System;
using System.Linq;
using System.Reflection;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace RozgrywkaKoncowa.Utils
{
    // Generic helper for "smart enum" classes implemented as public static fields
    public static class SmartEnum<T> where T : class
    {
        // Cached array of all static public fields of type T
        private static readonly T[] _all = typeof(T)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(f => f.FieldType == typeof(T))
            .Select(f => (T)f.GetValue(null)!)
            .ToArray();

        private static readonly ConcurrentDictionary<string, T?> _cache = new();

        public static IReadOnlyList<T> All => _all;

        // Find by arbitrary predicate
        public static T? FromPredicate(Func<T, bool> predicate) => Array.Find(_all, new Predicate<T>(predicate));

        // Find by selector + value (uses EqualityComparer<TValue>.Default)
        public static T? FromValue<TValue>(Func<T, TValue> selector, TValue value)
            where TValue : notnull
        {
            return _all.FirstOrDefault(x => EqualityComparer<TValue>.Default.Equals(selector(x), value));
        }

        // Fast lookup by string key (caches results). The keySelector should return a stable string identifier.
        public static T? FromKey(Func<T, string> keySelector, string key)
        {
            if (key == null) return null;
            return _cache.GetOrAdd(key, k => _all.FirstOrDefault(x => string.Equals(keySelector(x), k, StringComparison.Ordinal)));
        }
    }
}
