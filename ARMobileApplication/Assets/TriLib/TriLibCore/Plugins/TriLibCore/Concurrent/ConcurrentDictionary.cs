using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TriLibCore.General
{
    //todo: find original license
    /// <summary>Represents a concurrent (thread-safe) Dictionary.</summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TValue">The type of the t value.</typeparam>
    public class ConcurrentDictionary<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// The padlock
        /// </summary>
        private readonly object _lock = new object(); 

        /// <summary>
        /// The dictionary
        /// </summary>
        private readonly Dictionary<TKey, TValue> _dict = new Dictionary<TKey, TValue>();

        /// <summary>
        /// Gets or sets the TValue with the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>TValue.</returns>
        public TValue this[TKey key]
        {
            get
            {
                lock (_lock)
                {
                    return _dict[key];
                }
            }

            set
            {
                lock (_lock)
                {
                    _dict[key] = value;
                }
            }
        }

        /// <summary>Tries to get the value from the given Key on the Dictionary.</summary>
        /// <param name="key">The Key to look for.</param>
        /// <param name="value">The value, if found.</param>
        /// <returns>
        /// <c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_lock)
                return _dict.TryGetValue(key, out value);

        }
        /// <summary>Tries to add a Value to the Dictionary using the given Key</summary>
        /// <param name="key">The Key to try to add.</param>
        /// <param name="value">The value to try to add.</param>
        /// <returns>
        /// <c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryAdd(TKey key, TValue value)
        {
            lock (_lock)
            {
                if (!_dict.ContainsKey(key))
                {
                    _dict.Add(key, value);
                    return true;
                }
                return false;
            }
        }

        /// <summary>Tries to remove the given Item from the Dictionary.</summary>
        /// <param name="key">The Key to try to remove.</param>
        /// <returns>
        /// <c>true</c> if XXXX, <c>false</c> otherwise.</returns>
        public bool TryRemove(TKey key)
        {
            lock (_lock)
            {
                return _dict.Remove(key);
            }
        }

        /// <summary>Adds the specified key to the Dictionary.</summary>
        /// <param name="key">The Key to add.</param>
        /// <param name="val">The value to add.</param>
        public void Add(TKey key, TValue val)
        {
            lock (_lock)
            {
                _dict.Add(key, val);
            }
        }

        /// <summary>Determines whether this Dictionary contains the given Key.</summary>
        /// <param name="id">The Key value.</param>
        /// <returns>
        /// <c>true</c> if the specified identifier contains key; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(TKey id)
        {
            lock (_lock)
                return _dict.ContainsKey(id);
        }

        /// <summary>Orders the Dictionary using the given callback.</summary>
        /// <param name="func">The OrderBy callback Method.</param>
        /// <returns>List&lt;KeyValuePair&lt;TKey, TValue&gt;&gt;.</returns>
        public List<KeyValuePair<TKey, TValue>> OrderBy(Func<KeyValuePair<TKey, TValue>, TKey> func)
        {
            lock (_lock)
            {
                return _dict.OrderBy(func).ToList(); //todo: remove linq
            }
        }

        /// <summary>Gets the Dictionary Values.</summary>
        public Dictionary<TKey, TValue>.ValueCollection Values
        {
            get
            {
                lock (_lock)
                    return _dict.Values;
            }
        }
        /// <summary>Gets the Dictionary Keys.</summary>
        public Dictionary<TKey, TValue>.KeyCollection Keys
        {
            get
            {
                lock (_lock)
                    return _dict.Keys;
            }
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>An enumerator that can be used to iterate through the collection.</returns>
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            lock (_lock)
            {
                return _dict.GetEnumerator(); 
            }
        }

        /// <summary>Returns an enumerator that iterates through the collection.</summary>
        /// <returns>An <see cref="T:System.Collections.IEnumerator" /> object that can be used to iterate through the collection.</returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            lock (_lock)
            {
                return _dict.GetEnumerator(); 
            }
        }

        /// <summary>Gets the Dicitionary item count.</summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _dict.Count;
                }
            }
        }
    }
    
}
