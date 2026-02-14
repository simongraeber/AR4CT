using System.Collections.Generic;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Animation.
    /// </summary>
    public interface IAnimation : IObject
    {
        /// <summary>
        /// Gets or sets the list of animation curve bindings associated with this animation.
        /// Each binding defines how an animation curve is mapped to a particular property.
        /// </summary>
        List<IAnimationCurveBinding> AnimationCurveBindings { get; set; }

        /// <summary>
        /// Gets or sets a dictionary of animation curves grouped by model. 
        /// The outer dictionary key is the <see cref="IModel"/>, and the value is another dictionary 
        /// where each entry's key is a property name or identifier, and the value is the associated 
        /// <see cref="IAnimationCurve"/>.
        /// </summary>
        Dictionary<IModel, Dictionary<string, IAnimationCurve>> AnimationCurvesByModel { get; set; }

        /// <summary>
        /// Gets or sets this animation's frame rate in frames per second.
        /// </summary>
        float FrameRate { get; set; }

        /// <summary>
        /// Gets or sets the set of keyframe times at which translation (position) updates occur 
        /// for the associated model(s).
        /// </summary>
        HashSet<float> TranslationKeyTimes { get; set; }
    }
}
