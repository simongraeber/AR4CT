using System;
using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Allows developers to implement a custom mechanism for blend shape playback
    /// instead of Unity’s built-in system. By overriding methods in this class, you 
    /// can perform specialized setup for blend shapes, remap animation curves, 
    /// or otherwise customize how blend shape data is applied at runtime.
    /// </summary>
    public class BlendShapeMapper : ScriptableObject
    {
        /// <summary>
        /// Performs initial configuration for the specified <paramref name="geometryGroup"/>, 
        /// applying the blend shape data to the provided <paramref name="meshGameObject"/>. 
        /// Override this method to implement a custom blend shape playback system 
        /// (e.g., a custom runtime script or different naming conventions).
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing references to loaded objects, 
        /// loading options, and callbacks.
        /// </param>
        /// <param name="geometryGroup">
        /// The geometry group (mesh data and related structures) containing the blend shape keys.
        /// </param>
        /// <param name="meshGameObject">
        /// The <see cref="GameObject"/> to which the blend shape mesh is bound. 
        /// Often hosts a <see cref="SkinnedMeshRenderer"/> if using Unity’s default system.
        /// </param>
        /// <param name="blendShapeKeys">
        /// A list of <see cref="IBlendShapeKey"/> objects referencing the blend shapes 
        /// (morph targets) available for this mesh.
        /// </param>
        public virtual void Setup(
            AssetLoaderContext assetLoaderContext,
            IGeometryGroup geometryGroup,
            GameObject meshGameObject,
            List<IBlendShapeKey> blendShapeKeys)
        {
        }

        /// <summary>
        /// Provides a way to remap the animation property name or path when creating 
        /// animation curves for blend shape keys, effectively controlling how 
        /// TriLib’s loaded blend shape animations are interpreted in Unity. 
        /// Override this if your system requires a different naming scheme.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// References the model loading data, including loaded objects, user settings,
        /// and runtime information.
        /// </param>
        /// <param name="blendShapeIndex">
        /// The index of the blend shape within the mesh. This value can be used 
        /// to determine the naming or structure of animation curves.
        /// </param>
        /// <returns>
        /// A new or adjusted string representing the animation curve property to which 
        /// this blend shape should be keyed. <c>null</c> by default, indicating no remapping.
        /// </returns>
        public virtual string MapAnimationCurve(
            AssetLoaderContext assetLoaderContext,
            int blendShapeIndex)
        {
            return null;
        }

        /// <summary>
        /// Indicates the source type (e.g., <c>SkinnedMeshRenderer</c> or another component) 
        /// for blend shape animation curves. Override to specify a different type if 
        /// not using the default Unity setup. 
        /// </summary>
        public virtual Type AnimationCurveSourceType { get; }
    }
}
