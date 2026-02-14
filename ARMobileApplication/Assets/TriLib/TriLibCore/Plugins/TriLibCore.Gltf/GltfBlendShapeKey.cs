using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfBlendShapeKey : IBlendShapeKey {
        public Dictionary<int, int> IndexMap { get; set; }
        public List<Vector3> Vertices { get; set; }
        public List<Vector3> Normals { get; set; }
        public List<Vector3> Tangents { get; set; }
        public float FrameWeight { get; set; }
        public bool FullGeometryShape { get; set; }
        public string Name { get; set; }
        public bool Used { get; set; }
    }
}