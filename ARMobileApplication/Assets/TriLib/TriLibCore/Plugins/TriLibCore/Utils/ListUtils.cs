using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a series of Lists utility methods.
    /// </summary>
    public static class ListUtils
    {
        /// <summary>
        /// Fixes the given index, so it doesn't get outside the given List boundaries.
        /// </summary>
        /// <typeparam name="T">The List element type.</typeparam>
        /// <param name="index">The index.</param>
        /// <param name="list">The List.</param>
        /// <returns>The fixed element at the fixed index.</returns>
        public static int FixIndexNonGeneric<T>(int index, IList<T> list)
        {
            return Mathf.Clamp(index, 0, list.Count - 1);
        }
        /// <summary>
        /// Fixes the given index, so it doesn't get outside the given List boundaries and returns the element at the fixed index.
        /// </summary>
        /// <typeparam name="T">The List element type.</typeparam>
        /// <param name="index">The index.</param>
        /// <param name="list">The List.</param>
        /// <returns>The fixed element at the fixed index.</returns>
        public static T FixIndex<T>(int index, IList<T> list)
        {
            return list == null || list.Count == 0 ? default : list[Mathf.Clamp(index, 0, list.Count - 1)];
        }
    }
}
