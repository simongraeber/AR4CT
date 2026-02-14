using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfAnimation : IAnimation
    {
        public string Name { get; set; }
        public bool Used { get; set; }

        public List<IAnimationCurveBinding> AnimationCurveBindings { get; set; }

        public Dictionary<IModel, Dictionary<string, IAnimationCurve>> AnimationCurvesByModel
        {
            get;
            set;
        }

        public float FrameRate { get; set; }

        public HashSet<float> TranslationKeyTimes
        {
            get;
            set;
        }
    }
}