using System.IO;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Serves as an abstract base class for locating and opening external data streams within the TriLib loading workflow. 
    /// Inheritors must override <see cref="Map"/> to define how resource filenames 
    /// are resolved into <see cref="Stream"/> objects, allowing for custom logic such as 
    /// path transformations, caching, or network retrieval.
    /// </summary>
    public abstract class ExternalDataMapper : ScriptableObject
    {
        /// <summary>
        /// Attempts to locate and open the data resource corresponding to 
        /// <paramref name="originalFilename"/> in the context of a TriLib model load. 
        /// Implementers can check the file system, network sources, or other data repositories 
        /// to resolve the file path.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing loading settings, callbacks, 
        /// and references to the model’s assets and hierarchy.
        /// </param>
        /// <param name="originalFilename">
        /// The name or partial path of the resource as referenced in the source file 
        /// (e.g., a texture filename or other embedded data pointer).
        /// </param>
        /// <param name="finalPath">
        /// Outputs the resolved, absolute path to the located resource. 
        /// Returns <c>null</c> if the resource could not be found.
        /// </param>
        /// <returns>
        /// A <see cref="Stream"/> to the resource if located successfully; otherwise, 
        /// <c>null</c> if the file could not be found or opened.
        /// </returns>
        public abstract Stream Map(
            AssetLoaderContext assetLoaderContext,
            string originalFilename,
            out string finalPath
        );
    }
}
