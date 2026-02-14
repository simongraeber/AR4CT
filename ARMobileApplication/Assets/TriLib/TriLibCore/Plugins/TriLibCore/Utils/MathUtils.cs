using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents a series of math utility methods.
    /// </summary>
    public static class MathUtils
    {
        /// <summary>
        /// Represents the 3D axis as an array.
        /// </summary>
        public static readonly Vector3[] Axis = { Vector3.right, Vector3.up, Vector3.forward };

        /// <summary>
        /// Calculates the given tangent value sign. (w component)
        /// </summary>
        /// <param name="normal">The normal vector.</param>
        /// <param name="tangent">The tangent vector.</param>
        /// <returns>The tangent sign.</returns>
        public static float CalculateTangentSign(Vector3 normal, Vector3 tangent)
        {
            var binormal = Vector3.Cross(tangent, normal);
            var dp = Vector3.Dot(Vector3.Cross(normal, tangent), binormal);
            return dp > 0.0f ? 1f : -1f;
        }
    }
}