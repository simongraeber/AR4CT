using TriLibCore.Gltf.Reader;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfLight : GltfModel, ILight
    {
        public LightType LightType { get; set; } = LightType.Point;
        public Color Color { get; set; } = Color.white;
        public float Intensity { get; set; } = 1f;
        public float Range { get; set; } = GltfReader.SpotLightDistance;
        public float InnerSpotAngle { get; set; } = 90f;
        public float OuterSpotAngle { get; set; } = 90f;
        public float Width { get; set; }
        public float Height { get; set; }
        public bool CastShadows { get; set; }
    }
}