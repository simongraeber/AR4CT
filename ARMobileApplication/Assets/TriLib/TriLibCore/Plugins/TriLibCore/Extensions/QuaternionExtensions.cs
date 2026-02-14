using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Represents a series of Quaternion utility methods.
    /// </summary>
    public static class QuaternionExtensions
    {
        /// <summary>
        /// Checks if the given Quaternion is invalid.
        /// </summary>
        /// <param name="quaternion">Quaternion to be checked.</param>
        /// <returns><c>true</c> if Quaternion is invalid. Otherwise, <c>false</c>.</returns>
        public static bool IsInvalid(this Quaternion quaternion)
        {
            var invalid = quaternion.x == 0f;
            invalid &= quaternion.y == 0f;
            invalid &= quaternion.z == 0f;
            invalid &= quaternion.w == 0f;
            return invalid;
        }
    }
}