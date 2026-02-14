using System;
using System.Collections.Generic;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a class used to handle TriLib CustomData parameter as a Type/Object Dictionary.
    /// </summary>
    public static class CustomDataHelper
    {
        /// <summary>
        /// Creates a new Type/Object Dictionary.
        /// </summary>
        /// <returns>The created Dictionary</returns>
        public static Dictionary<Type, object> CreateCustomDataDictionary()
        {
            return new Dictionary<Type, object>();
        }

        /// <summary>
        /// Creates a new Type/Object Dictionary and adds an existing data to it.
        /// </summary>
        /// <param name="data">The data to add to the Dictionary.</param>
        /// <returns>The created Dictionary</returns>
        public static Dictionary<Type, object> CreateCustomDataDictionaryWithData<T>(T data) where T : class
        {
            var dictionary = new Dictionary<Type, object>();
            dictionary.Add(typeof(T), data);
            return dictionary;
        }

        /// <summary>
        /// Tries to get a value with the type 'T' when the 'customData' is a Type/Object Dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the value to look for.</typeparam>
        /// <param name="customData">The customData object.</param>
        /// <returns>The value, if found. Otherwise <c>null</c>.</returns>
        public static T GetCustomData<T>(object customData) where T : class
        {
            if (customData is Dictionary<Type, object> dictionary && dictionary.TryGetValue(typeof(T), out var typedCustomData))
            {
                return typedCustomData as T;
            }
            return customData as T;
        }

        /// <summary>
        /// Tries to set a value with the type 'T' and value 'value' by setting the 'customData' as a Type/Object Dictionary.
        /// </summary>
        /// <typeparam name="T">The type of the value to store.</typeparam>
        /// <param name="customData">The customData object.</param>
        /// <param name="value">The value to store.</param>
        public static void SetCustomData<T>(ref object customData, T value) where T : class
        {
            if ((customData is Dictionary<Type, object> dictionary))
            {
                dictionary[typeof(T)] = value;
                return;
            }
            customData = value;
        }
    }
}