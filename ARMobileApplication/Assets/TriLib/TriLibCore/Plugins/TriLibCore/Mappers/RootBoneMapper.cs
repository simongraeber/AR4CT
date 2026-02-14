using System;
using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides functionality to locate a “root bone” for rigging or animation setup 
    /// within the loaded model hierarchy. This can be essential when binding skin meshes 
    /// or humanoid rigs, as well as customizing bone references in scripts.
    /// </summary>
    public class RootBoneMapper : ScriptableObject
    {
        /// <summary>
        /// Attempts to find a root bone in the loaded model hierarchy, returning the 
        /// <see cref="Transform"/> to be used as the topmost bone for binding or rigging. 
        /// This method is now marked as <c>Obsolete</c>; use <see cref="Map(AssetLoaderContext, IList{Transform})"/> instead.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> referencing loaded <see cref="GameObject"/> data, 
        /// user options, and other contextual information.
        /// </param>
        /// <returns>
        /// The root bone <see cref="Transform"/> if one is found or chosen; 
        /// by default, the method returns <c>assetLoaderContext.RootGameObject.transform</c>.
        /// </returns>
        [Obsolete("Please use the override that accepts a list of bones instead.")]
        public virtual Transform Map(AssetLoaderContext assetLoaderContext)
        {
            return assetLoaderContext.RootGameObject.transform;
        }

        /// <summary>
        /// Finds a suitable root bone from a provided list of potential bone transforms. 
        /// This is the highest-level bone in the hierarchy that others descend from. 
        /// Subclasses should override this method to implement more advanced selection logic 
        /// (e.g., checking naming patterns, evaluating bone connections, etc.).
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> referencing the loaded model’s GameObjects, 
        /// user-defined import options, and callbacks.
        /// </param>
        /// <param name="bones">
        /// An ordered or unordered list of transforms that may qualify as bones in the 
        /// skeleton hierarchy. Subclasses can evaluate these to decide which one 
        /// is truly at the top of the chain.
        /// </param>
        /// <returns>
        /// The <see cref="Transform"/> chosen to serve as the root bone. By default, 
        /// this method returns <c>assetLoaderContext.RootGameObject.transform</c>.
        /// </returns>
        public virtual Transform Map(AssetLoaderContext assetLoaderContext, IList<Transform> bones)
        {
            return assetLoaderContext.RootGameObject.transform;
        }
    }
}
