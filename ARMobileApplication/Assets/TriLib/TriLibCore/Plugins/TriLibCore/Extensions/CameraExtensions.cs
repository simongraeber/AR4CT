using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>Represents a series of Camera extension methods.</summary>
    public static class CameraExtensions
    {
        /// <summary>Frames the given Camera on the given Game Object Bounds.</summary>
        /// <param name="camera">The Camera to adjust to the Bounds.</param>
        /// <param name="gameObject">The Game Object to frame.</param>
        /// <param name="distance">The distance to keep from Game Object center.</param>
        public static void FitToBounds(this Camera camera, GameObject gameObject, float distance)
        {
            var bounds = gameObject.CalculateBounds();
            FitToBounds(camera, bounds, distance);
        }

        /// <summary>Frames the given Camera on the given Bounds.</summary>
        /// <param name="camera">The Camera to adjust to the Bounds.</param>
        /// <param name="bounds">The Bounds to frame.</param>
        /// <param name="distance">The distance to keep from Bounds center.</param>
        public static void FitToBounds(this Camera camera, Bounds bounds, float distance)
        {
            var boundRadius = bounds.extents.magnitude;
            var finalDistance = boundRadius / (2.0f * Mathf.Tan(0.5f * camera.fieldOfView * Mathf.Deg2Rad)) * distance;
            if (float.IsNaN(finalDistance))
            {
                return;
            }
            camera.transform.position = new Vector3(bounds.center.x, bounds.center.y, bounds.center.z + finalDistance);
            camera.transform.LookAt(bounds.center);
        }

        /// <summary>Frames the given Camera on the given Bounds.</summary>
        /// <param name="camera">The Camera to adjust to the Bounds.</param>
        /// <param name="bounds">The Bounds to frame.</param>
        /// <param name="rotation">The rotation to arc-rotate the the Camera relative to the Bounds center.</param>
        /// <param name="distance">The distance to keep from Bounds center.</param>
        public static void FitToBounds(this Camera camera, Bounds bounds, Quaternion rotation, float distance)
        {
            var boundRadius = bounds.extents.magnitude;
            var finalDistance = boundRadius / (2.0f * Mathf.Tan(0.5f * camera.fieldOfView * Mathf.Deg2Rad)) * distance;
            if (float.IsNaN(finalDistance))
            {
                return;
            }
            camera.transform.position = bounds.center - (rotation * Vector3.forward * finalDistance);
            camera.transform.LookAt(bounds.center);
        }
    }
}
