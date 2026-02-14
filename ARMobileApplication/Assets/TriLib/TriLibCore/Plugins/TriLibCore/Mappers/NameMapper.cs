using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides a mechanism for generating custom <see cref="GameObject"/> names 
    /// in the scene, based on metadata from the source 3D model. By inheriting 
    /// <see cref="NameMapper"/> and overriding <see cref="Map"/>, users can implement 
    /// specialized naming schemes (e.g., appending model part numbers, classes, or other identifiers).
    /// </summary>
    public class NameMapper : ScriptableObject
    {
        /// <summary>
        /// Generates a custom name for the loaded <see cref="GameObject"/> based on the provided model data. 
        /// If this method returns <c>null</c>, TriLib will fall back to its default naming strategy.
        /// </summary>
        /// <remarks>
        /// Returning <c>null</c> indicates that the default TriLib naming pattern should be used. 
        /// Otherwise, the returned string is applied directly as the <see cref="GameObject"/> name.
        /// </remarks>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing references to the model’s loading options, 
        /// callbacks, and the root <see cref="GameObject"/> structure.
        /// </param>
        /// <param name="data">
        /// A <see cref="ModelNamingData"/> object containing fields such as original name, class, ID, 
        /// and part number from the source file.
        /// </param>
        /// <param name="model">
        /// The <see cref="IModel"/> representing a portion of the loaded hierarchy 
        /// (e.g., a mesh segment or node).
        /// </param>
        /// <param name="readerName">
        /// The name of the TriLib reader used to load the model data (e.g., “FBXReader”, “GltfReader”).
        /// </param>
        /// <returns>
        /// A string representing the desired <see cref="GameObject"/> name. If <c>null</c>, 
        /// the default TriLib naming will be used instead.
        /// </returns>
        public virtual string Map(
            AssetLoaderContext assetLoaderContext,
            ModelNamingData data,
            IModel model,
            string readerName)
        {
            return null;
        }
    }
}
