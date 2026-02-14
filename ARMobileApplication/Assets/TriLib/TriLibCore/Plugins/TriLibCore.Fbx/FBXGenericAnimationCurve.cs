using System;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXGenericAnimationCurve : IAnimationCurve
    {
        public string Name;
        public string Property { get; set; }
        public Type AnimatedType { get; set; }
        public AnimationCurve AnimationCurve { get; set; }

        public TangentMode TangentMode { get; set; }

        public FBXGenericAnimationCurve(string property, Type animatedType)
        {
            Property = property;
            AnimatedType = animatedType;
            AnimationCurve = new AnimationCurve();
        }
    }
}