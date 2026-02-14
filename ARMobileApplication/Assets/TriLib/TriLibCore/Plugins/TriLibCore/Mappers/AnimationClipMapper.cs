using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides a mechanism for post-processing or re-mapping <see cref="AnimationClip"/> instances
    /// within the TriLib loading pipeline. Subclasses can override <see cref="MapArray"/> to manipulate
    /// or customize animation data (e.g., for retargeting, curve simplification, or special playback requirements).
    /// </summary>
    public class AnimationClipMapper : ScriptableObject
    {
        /// <summary>
        /// Indicates the relative priority of this mapper when multiple <see cref="AnimationClipMapper"/> 
        /// instances are defined in <see cref="AssetLoaderOptions.AnimationClipMappers"/>. 
        /// Lower values signify earlier processing, while higher values are tried later 
        /// if earlier mappers do not produce a final result.
        /// </summary>
        public int CheckingOrder;

        /// <summary>
        /// Invoked to process an array of <see cref="AnimationClip"/>s, allowing for modifications 
        /// such as re-structuring clip data, applying custom import settings, or removing unwanted frames.
        ///
        /// <para>
        /// By default, this method returns the original <paramref name="sourceAnimationClips"/> unmodified. 
        /// Inherit from <see cref="AnimationClipMapper"/> to perform custom logic (e.g., 
        /// applying curve simplification, retargeting bone names, etc.).
        /// </para>
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> encapsulating model loading state and references, 
        /// including loaded objects, settings, and callbacks.
        /// </param>
        /// <param name="sourceAnimationClips">The unprocessed array of <see cref="AnimationClip"/> instances.</param>
        /// <returns>An array of <see cref="AnimationClip"/> after processing. Could be the same array, 
        /// a modified version, or a newly created set of clips.</returns>
        public virtual AnimationClip[] MapArray(
            AssetLoaderContext assetLoaderContext,
            AnimationClip[] sourceAnimationClips)
        {
            return sourceAnimationClips;
        }
    }
}
