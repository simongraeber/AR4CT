using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>Represents a series of Matrix extension methods.</summary>
    public static class MatrixExtensions
    {
        /// <summary>
        /// Decomposes the given Matrix4x4 into TRS values.
        /// </summary>
        /// <param name="matrix">The Matrix4x4 to be decomposed.</param>
        /// <param name="position">The decomposed position.</param>
        /// <param name="rotation">The decomposed rotation.</param>
        /// <param name="scale">The decomposed scale.</param>
        public static void Decompose(this Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            // Extract translation directly
            position = new Vector3(matrix.m03, matrix.m13, matrix.m23);

            // Extract 3x3 rotation/scale portion
            var m = matrix;
            m.m03 = m.m13 = m.m23 = 0f; // Remove translation
            m.m30 = m.m31 = m.m32 = 0f;
            m.m33 = 1f;

            // Get basis vectors
            var c0 = m.GetColumn(0);
            var c1 = m.GetColumn(1);
            var c2 = m.GetColumn(2);

            // Compute scale magnitudes
            var lenC0 = c0.magnitude;
            var lenC1 = c1.magnitude;
            var lenC2 = c2.magnitude;

            // Avoid division by zero
            if (lenC0 < 0.000001f) lenC0 = 0.000001f;
            if (lenC1 < 0.000001f) lenC1 = 0.000001f;
            if (lenC2 < 0.000001f) lenC2 = 0.000001f;

            // Initial rotation matrix
            var rotationMatrix = Matrix4x4.identity;
            var r0 = c0 / lenC0;
            var r1 = c1 / lenC1;
            var r2 = c2 / lenC2;
            rotationMatrix.SetColumn(0, r0);
            rotationMatrix.SetColumn(1, r1);
            rotationMatrix.SetColumn(2, r2);

            // Set initial scale
            scale = new Vector3(lenC0, lenC1, lenC2);

            // Check determinant and flip if negative (UniGLTF style)
            var cross = Vector3.Cross(r0, r1);
            if (Vector3.Dot(cross, r2) < 0f) // Negative determinant
            {
                // Flip entire rotation matrix
                rotationMatrix.SetColumn(0, -rotationMatrix.GetColumn(0));
                rotationMatrix.SetColumn(1, -rotationMatrix.GetColumn(1));
                rotationMatrix.SetColumn(2, -rotationMatrix.GetColumn(2));
                scale *= -1f;
            }

            // Normalize rotation matrix columns (optional, for precision)
            rotationMatrix.SetColumn(0, rotationMatrix.GetColumn(0).normalized);
            rotationMatrix.SetColumn(1, rotationMatrix.GetColumn(1).normalized);
            rotationMatrix.SetColumn(2, rotationMatrix.GetColumn(2).normalized);

            // Extract rotation
            rotation = rotationMatrix.rotation;
        }

        /// <summary>
        /// Decomposes the given Matrix4x4 into TRS values using simpler methods.
        /// </summary>
        /// <param name="matrix">The Matrix4x4 to be decomposed.</param>
        /// <param name="position">The decomposed position.</param>
        /// <param name="rotation">The decomposed rotation.</param>
        /// <param name="scale">The decomposed scale.</param>
        public static void DecomposeSimple(this Matrix4x4 matrix, out Vector3 position, out Quaternion rotation, out Vector3 scale)
        {
            position = matrix.GetMatrixPositionSimple();
            rotation = matrix.rotation;
            scale = matrix.lossyScale;
        }

        /// <summary>Creates a Matrix to frame the given Bounds.</summary>
        /// <param name="fov">The view field-of-view.</param>
        /// <param name="bounds">The Bounds to frame.</param>
        /// <param name="distance">The distance to keep from Game Object center.</param>
        /// <returns>The framed Matrix.</returns>
        public static Matrix4x4 FitToBounds(float fov, Bounds bounds, float distance)
        {
            var boundRadius = bounds.extents.magnitude;
            var finalDistance = boundRadius / (2.0f * Mathf.Tan(0.5f * fov * Mathf.Deg2Rad)) * distance;
            if (float.IsNaN(finalDistance))
            {
                return Matrix4x4.identity;
            }
            var position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z + finalDistance);
            return Matrix4x4.TRS(position, Quaternion.LookRotation((bounds.center - position).normalized), Vector3.one);
        }

        /// <summary>Extracts the position from this Matrix.</summary>
        /// <param name="m">The source Matrix.</param>
        /// <returns>The extracted position.</returns>
        public static Vector3 GetMatrixPosition(this Matrix4x4 m)
        {
            m.DecomposeSimple(out var t, out _, out _);
            return t;
        }

        /// <summary>
        /// Extracts the position from this Matrix.
        /// </summary>
        /// <param name="m">The source Matrix.</param>
        /// <returns>The extracted position.</returns>
        public static Vector3 GetMatrixPositionSimple(this Matrix4x4 m)
        {
            return m.GetColumn(3);
        }

        /// <summary>
        /// Indicates whether the given Matrix4x4 scale is negative.
        /// </summary>
        /// <param name="m">The Matrix4x4 to check the scale from.</param>
        /// <returns><c>true</c> if any matrix scale component is negative.</returns>
        public static bool IsNegative(this Matrix4x4 m)
        {
            var cross = Vector3.Cross(m.GetColumn(0), m.GetColumn(1));
            return Vector3.Dot(cross, m.GetColumn(2)) < 0f;
        }
    }
}
