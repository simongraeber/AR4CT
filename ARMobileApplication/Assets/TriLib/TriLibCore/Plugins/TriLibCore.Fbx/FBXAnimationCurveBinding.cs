using System.Collections.Generic;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public class FBXAnimationCurveBinding : FBXObject, IAnimationCurveBinding
    {
        public IModel Model { get; set; }

        public List<IAnimationCurve> AnimationCurves { get; set; }
    }
}