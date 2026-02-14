using System.Collections.Generic;
using TriLibCore.Interfaces;

namespace TriLibCore.Dae
{
    public class DaeAnimationCurveBinding : IAnimationCurveBinding
    {
        public IModel Model { get; set; }
        public List<IAnimationCurve> AnimationCurves { get; set; } = new List<IAnimationCurve>();
    }
}