using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Extensions
{
    
    /// <summary>Represents a series of Animation extension methods.</summary>
    public static class AnimationExtensions
    {
        /// <summary>Gets all Animation Clips  from the given Animation Component.</summary>
        /// <param name="animation">The Animation Component containing the Animation Clips.</param>
        /// <returns>The animation clips from the given animation component.</returns>
        public static List<AnimationClip> GetAllAnimationClips(this Animation animation)
        {
            var result = new List<AnimationClip>();
            foreach (AnimationState animationState in animation)
            {
                result.Add(animationState.clip);
            }
            return result;
        }
    }
    
}
