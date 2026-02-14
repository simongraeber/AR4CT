using TriLibCore.General;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>Represents a series of Transform extension methods.</summary>
    public static class TransformExtensions
    {
        /// <summary>Recursively counts this Transform children.</summary>
        /// <param name="transform">The Transform containing the children.</param>
        /// <returns>The children count</returns>
        public static int CountChild(this Transform transform)
        {
            var childCount = transform.childCount;
            for (var i = 0; i < transform.childCount; i++)
            {
                childCount += CountChild(transform.GetChild(i));
            }
            return childCount;
        }

        /// <summary>Tries to recursively find a Transform on another Transform hierarchy by its name.</summary>
        /// <param name="transform">The Transform containing the children.</param>
        /// <param name="right">The Transform name to search for</param>
        /// <param name="stringComparisonMode">The type of comparison to use.</param>
        /// <param name="caseInsensitive">Pass <c>true</c> to do a case-insensitive search.</param>
        /// <returns>The found transform, or <c>null</c></returns>
        public static Transform FindDeepChild(this Transform transform, string right, StringComparisonMode stringComparisonMode, bool caseInsensitive)
        {
            if (StringComparer.Matches(stringComparisonMode, caseInsensitive, transform.name, right))
            {
                return transform;
            }
            for (var i = 0; i < transform.childCount; i++)
            {
                var child = transform.GetChild(i);
                var result = child.FindDeepChild(right, stringComparisonMode, caseInsensitive);
                if (result != null)
                {
                    return result;
                }
            }
            return null;
        }

        /// <summary>Builds a path to the given Transform hierarchy.</summary>
        /// <param name="transform">The Transform to build the path from.</param>
        /// <param name="rootTransform">The Transform where the hierarchy ends.</param>
        /// <returns>The built path</returns>
        public static string BuildPath(this Transform transform, Transform rootTransform)
        {
            var path = string.Empty;
            while (transform != rootTransform && transform != null)
            {
                if (!string.IsNullOrEmpty(transform.name))
                {
                    path = path != string.Empty ? $"{transform.name}/{path}" : transform.name;
                }
                transform = transform.parent;
            }
            return path;
        }
    }
}