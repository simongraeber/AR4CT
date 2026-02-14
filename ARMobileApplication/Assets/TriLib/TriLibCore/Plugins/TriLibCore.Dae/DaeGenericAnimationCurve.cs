using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeGenericAnimationCurve : IAnimationCurve
    {
        public string Name;
        public string Property { get; set; }
        public Type AnimatedType { get; set; }
        public AnimationCurve AnimationCurve { get; set; }
        
        public TangentMode TangentMode { get; set; }

        public DaeGenericAnimationCurve(string property, Type animatedType)
        {
            Property = property;
            AnimatedType = animatedType;
            AnimationCurve = new AnimationCurve();
        }
    }
}
