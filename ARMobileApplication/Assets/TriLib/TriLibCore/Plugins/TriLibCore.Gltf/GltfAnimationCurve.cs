using System;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfAnimationCurve : IAnimationCurve
    {
        public string Property { get; set; }
        public Type AnimatedType { get; set; } = typeof(Transform);
        public AnimationCurve AnimationCurve { get; set; }
        
        public TangentMode TangentMode { get; set; }
    }
}