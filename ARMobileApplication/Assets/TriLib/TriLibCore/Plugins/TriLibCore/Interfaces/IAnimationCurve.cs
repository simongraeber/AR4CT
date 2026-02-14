using System;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Animation Curve.
    /// </summary>
    public interface IAnimationCurve
    {
        /// <summary>
        /// Gets/Sets the animated Property name.
        /// </summary>
        string Property { get; set; }

        /// <summary>
        /// Gets/Sets the animated Property Type.
        /// </summary>
        Type AnimatedType { get; set; }

        /// <summary>
        /// Gets/Sets the Animation Curve Keyframes.
        /// </summary>
        AnimationCurve AnimationCurve { get; set; }
    }
}
