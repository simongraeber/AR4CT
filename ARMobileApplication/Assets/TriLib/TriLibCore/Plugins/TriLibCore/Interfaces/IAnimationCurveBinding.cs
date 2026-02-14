using System.Collections.Generic;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Animation Curve Binding, which connects a <see cref="IModel"/> 
    /// with one or more associated <see cref="IAnimationCurve"/> instances.
    /// </summary>
    public interface IAnimationCurveBinding
    {
        /// <summary>
        /// Gets or sets the model that this binding references.
        /// </summary>
        IModel Model { get; set; }

        /// <summary>
        /// Gets or sets the list of animation curves that affect the <see cref="Model"/>.
        /// </summary>
        List<IAnimationCurve> AnimationCurves { get; set; }
    }
}
