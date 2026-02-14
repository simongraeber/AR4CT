using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>Represents a TriLib Model, which will be converted to a Game Object in Unity.</summary>
    public interface IModel : IObject
    {
        /// <summary>
        /// Gets/Sets this Model pivot in world space.
        /// </summary>
        Vector3 Pivot { get; set; }

        /// <summary>Gets/Sets this Model local position.</summary>

        Vector3 LocalPosition { get; set; }

        /// <summary>Gets/Sets this Model local rotation.</summary>

        Quaternion LocalRotation { get; set; }

        /// <summary>Gets/Sets this Model local scale.</summary>

        Vector3 LocalScale { get; set; }

        /// <summary>Gets/Sets this Model visibility (visible or invisible).</summary>
        bool Visibility { get; set; }

        /// <summary>Gets/Sets this Model parent.</summary>

        IModel Parent { get; set; }

        /// <summary>Gets/Sets this Model children.</summary>

        List<IModel> Children { get; set; }

        /// <summary>Gets/Sets this Model bones.</summary>

        List<IModel> Bones { get; set; }

        /// <summary>
        /// Indicates wheter this model is a bone.
        /// </summary>
        bool IsBone { get; set; }

        /// <summary>Gets/Sets this Model Geometry Group.</summary>

        IGeometryGroup GeometryGroup { get; set; }

        ///// <summary>Gets/Sets this Model Materials.</summary>

        //IList<IMaterial> Materials { get; set; }

        /// <summary>Gets/Sets this Model bind poses.</summary>

        Matrix4x4[] BindPoses { get; set; }

        /// <summary>Gets/Sets this Model Material indices.</summary>

        int[] MaterialIndices { get; set; }

        /// <summary>
        /// Represents a series of model user defined properties.
        /// </summary>
        Dictionary<string, object> UserProperties { get; set; }

        /// <summary>
        /// Defines whether the given model uses a custom pivot.
        /// </summary>
        bool HasCustomPivot { get; set; }
        
        /// <summary>
        /// Original (not pivoted) model local to world matrix.
        /// </summary>
        Matrix4x4 OriginalGlobalMatrix { get; set; }
    }
}