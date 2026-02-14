using System.Collections.Generic;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Represents a series of Dictionary extension methods.
    /// </summary>
    public static class DictionaryExtensions
    {
#if !NETSTANDARD
        public static TValue GetValueOrDefault<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            if (dictionary.TryGetValue(key, out TValue value))
            {
                return value;
            }
            return defaultValue;
        }

        public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
            {
                return false;
            }
            dictionary.Add(key, value);
            return true;
        }
#endif

        /// <summary>
        /// Tries to get a value in the given Dictionary without throwing Exceptions.
        /// </summary>
        /// <typeparam name="TKey">The Dictionary Key type.</typeparam>
        /// <typeparam name="TValue">The Dictionary Value type.</typeparam>
        /// <param name="dictionary">The Dictionary.</param>
        /// <param name="key">The Dictionary key.</param>
        /// <param name="value">The Dictionary value.</param>
        /// <returns><c>true</c> if the given Key is found. Otherwise, <c>false</c>.</returns>
        public static bool TryGetValueSafe<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, out TValue value)
        {
            if (key == null)
            {
                value = default;
                return false;
            }
            return dictionary.TryGetValue(key, out value);
        }
    }
}
