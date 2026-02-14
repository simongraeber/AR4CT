using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>Represents a structure to store a Transform position, rotation, and scale temporarily.</summary>
    public struct TransformBackup
    {
        /// <summary>
        /// Stored Local Position
        /// </summary>
        private readonly Vector3 _localPosition;
        /// <summary>
        /// Stored Local Rotation
        /// </summary>
        private readonly Quaternion _localRotation;
        /// <summary>
        /// Stored Local Scale
        /// </summary>
        private readonly Vector3 _localScale;

        /// <summary>Represents a structure to store a Transform position, rotation, and scale temporarily.</summary>
        /// <param name="transform">The Transform to backup the data from.</param>
        public TransformBackup(Transform transform)
        {
            _localPosition = transform.localPosition;
            _localRotation = transform.localRotation;
            _localScale = transform.localScale;
        }

        /// <summary>Restores the Transform position, rotation and scale to the original value.</summary>
        /// <param name="transform">The Transform to restore the data.</param>
        public void Restore(Transform transform)
        {
            transform.localPosition = _localPosition;
            transform.localRotation = _localRotation;
            transform.localScale = _localScale;
        }
    }
}