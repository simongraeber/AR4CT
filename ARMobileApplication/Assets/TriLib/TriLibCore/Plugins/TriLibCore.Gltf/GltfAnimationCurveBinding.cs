using System.Collections.Generic;
using TriLibCore.General;
using TriLibCore.Interfaces;

namespace TriLibCore.Gltf
{
    public class GltfAnimationCurveBinding : IAnimationCurveBinding
    {
        public IModel Model { get; set; }

        public List<IAnimationCurve> AnimationCurves { get; set; } = new List<IAnimationCurve>(Constants.CommonAnimationCurveCount);
    }
}