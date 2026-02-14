using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Camera.
    /// </summary>
    public interface ICamera : IModel
    {
        /// <summary>
        /// The camera aspect ratio.
        /// </summary>
        float AspectRatio { get; set; }
        /// <summary>
        /// Indicates whether the camera ortographic?
        /// </summary>
        bool Ortographic { get; set; }
        /// <summary>
        /// The camera ortographic size.
        /// </summary>
        float OrtographicSize { get; set; }
        /// <summary>
        /// The camera field of view in degrees.
        /// </summary>
        float FieldOfView { get; set; }
        /// <summary>
        /// The camera near clip plane distance.
        /// </summary>
        float NearClipPlane { get; set; }
        /// <summary>
        /// The camera far clip distance.
        /// </summary>
        float FarClipPlane { get; set; }
        /// <summary>
        /// The physical camera focal length.
        /// </summary>
        float FocalLength { get; set; }
        /// <summary>
        /// The physical camera sensor size.
        /// </summary>
        Vector2 SensorSize { get; set; }
        /// <summary>
        /// The physical camera lens shift.
        /// </summary>
        Vector2 LensShift { get; set; }
        /// <summary>
        /// The physical camera gate fit mode.
        /// </summary>
        Camera.GateFitMode GateFitMode { get; set; }

        /// <summary>
        /// Defines whether this is a camera with physical properties.
        /// </summary>
        bool PhysicalCamera { get; set; }

        /// <summary>
        /// Defines whether this camera has a target.
        /// </summary>
        bool HasTarget { get; set; }
    }
}
