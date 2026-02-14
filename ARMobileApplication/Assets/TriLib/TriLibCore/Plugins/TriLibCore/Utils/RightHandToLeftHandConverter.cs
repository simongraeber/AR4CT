using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Represents methods to convert from a right-hand to a left-hand coordinate system.
    /// </summary>
    public static class RightHandToLeftHandConverter
    {
        /// <summary>Converts a Matrix4x4 to the left-hand coordinate system.</summary>
        /// <param name="value">The Matrix4x4 to be converted.</param>
        /// <returns>The converted matrix.</returns>
        public static Matrix4x4 ConvertMatrix(Matrix4x4 value)
        {
            value.m00 = -value.m00;
            value.m01 = -value.m01;
            value.m02 = -value.m02;
            value.m03 = -value.m03;
            
            value.m00 = -value.m00;
            value.m10 = -value.m10;
            value.m20 = -value.m20;
            value.m30 = -value.m30;

            return value;
        }

        /// <summary>Converts a Matrix4x4 to the left-hand coordinate system.</summary>
        /// <param name="value">The Matrix4x4 to be converted.</param>
        /// <returns>The converted matrix.</returns>
        public static Matrix4x4 ConvertMatrix2(Matrix4x4 value)
        {
            value.m02 = -value.m02;
            value.m12 = -value.m12;
            value.m20 = -value.m20;
            value.m21 = -value.m21;
            value.m23 = -value.m23;
            return value;
        }
        /// <summary>Converts a Quaternion to the left-hand coordinate system.</summary>
        /// <param name="value">The Quaternion to be converted.</param>
        /// <returns>The converted Quaternion.</returns>
        public static Quaternion ConvertRotation(Quaternion value)
        {
            value.y = -value.y;
            value.z = -value.z;
            return value;
        }

        /// <summary>Converts an Angle representation to the left-hand coordinate system.</summary>
        /// <param name="value">The Angle representation to be converted.</param>
        /// <returns>The converted vector.</returns>
        public static Vector3 ConvertRotation(Vector3 value)
        {
            value.y = -value.y;
            value.z = -value.z;
            return value;
        }

        /// <summary>Converts a Quaternion to the left-hand coordinate system.</summary>
        /// <param name="value">The Quaternion to be converted.</param>
        /// <returns>The converted vector.</returns>
        public static Vector4 ConvertRotation(Vector4 value)
        {
            value.y = -value.y;
            value.z = -value.z;
            return value;
        }

        /// <summary>Converts a Vector3 to the left-hand coordinate system.</summary>
        /// <param name="value">The Vector3 to be converted.</param>
        /// <returns>The converted vector.</returns>
        public static Vector3 ConvertVector(Vector3 value)
        {
            value.x = -value.x;
            return value;
        }
    }
}