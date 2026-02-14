using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeAnimation : IAnimation
    {
        public HashSet<DaeModel> AnimatedModels = new HashSet<DaeModel>();
        public float LocalStart = float.MaxValue;
        public float LocalEnd = float.MinValue;
        public Dictionary<DaeModel, IList<DaeAnimationCurve>> AnimationCurveBindingsDictionary = new Dictionary<DaeModel, IList<DaeAnimationCurve>>();

        public string Name { get; set; }
        public bool Used { get; set; }
        public List<IAnimationCurveBinding> AnimationCurveBindings { get; set; }

        public Dictionary<IModel, Dictionary<string, IAnimationCurve>> AnimationCurvesByModel
        {
            get ;
            set ;
        }

        public float FrameRate { get; set; }

        public HashSet<float> TranslationKeyTimes
        {
            get;
            set;
        }
    }
}