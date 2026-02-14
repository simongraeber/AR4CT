using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>Represents a Mapper used to process Animation Controller Clips.</summary>
    public abstract class AnimatorControllerMapper : ScriptableObject
    {
        /// <summary>Processes the given Animation Clip.</summary>
        /// <param name="assetLoaderContext">The Asset Loader Context reference. Asset Loader Context contains the Model loading data.</param>
        /// <param name="source">The source Animation Clip to process.</param>
        public abstract void Map(AssetLoaderContext assetLoaderContext, AnimationClip source);
    }
}