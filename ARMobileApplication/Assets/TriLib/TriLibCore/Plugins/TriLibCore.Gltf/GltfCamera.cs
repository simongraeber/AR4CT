using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfCamera : GltfModel, ICamera
    {
        public float XMag = 1f;
        public float YMag = 1f;
        public bool HasTarget { get; set; }
        public float AspectRatio { get; set; } = 1.333333f;
        public bool Ortographic { get; set; } = false;
        public float OrtographicSize { get; set; } = 1f;
        public float FieldOfView { get; set; } = 60f;
        public float NearClipPlane { get; set; } = 0.1f;
        public float FarClipPlane { get; set; } = 4f;
        public float FocalLength { get; set; }
        public Vector2 SensorSize { get; set; }
        public Vector2 LensShift { get; set; }
        public Camera.GateFitMode GateFitMode { get; set; } = Camera.GateFitMode.None;
        public bool PhysicalCamera { get; set; }
    }
}
