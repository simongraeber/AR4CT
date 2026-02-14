using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Provides a comparer for <see cref="BoneWeight1"/> instances that sorts
    /// them by weight in descending order.
    /// </summary>
    /// <remarks>
    /// This comparer is typically used to prioritize the most influential
    /// bone weights when processing or normalizing skinning data.
    /// </remarks>
    public struct BoneWeightComparer : IComparer<BoneWeight1>
    {
        /// <summary>
        /// Compares two <see cref="BoneWeight1"/> instances based on their weight.
        /// </summary>
        /// <param name="a">The first bone weight to compare.</param>
        /// <param name="b">The second bone weight to compare.</param>
        /// <returns>
        /// A value less than zero if <paramref name="b"/> has a greater weight than
        /// <paramref name="a"/>, zero if both weights are equal, or a value greater
        /// than zero otherwise.
        /// </returns>
        public int Compare(BoneWeight1 a, BoneWeight1 b)
        {
            return b.weight.CompareTo(a.weight);
        }
    }
}
