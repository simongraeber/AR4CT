#pragma warning disable 618
using System;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides a mechanism to locate and load texture data for a model, either by 
    /// leveraging its original filename or tapping into embedded resources. 
    /// <see cref="TextureMapper"/> can be subclassed to implement specialized 
    /// texture-finding strategies—for instance, searching alternative file paths or 
    /// retrieving assets from remote sources.
    /// </summary>
    public class TextureMapper : ScriptableObject
    {
        /// <summary>
        /// Indicates the relative priority of this mapper when multiple <see cref="TextureMapper"/> 
        /// instances are present in an <see cref="AssetLoaderOptions"/>. Mappers with lower 
        /// <c>CheckingOrder</c> values are attempted first.
        /// </summary>
        public int CheckingOrder;

        /// <summary>
        /// Attempts to retrieve a <see cref="TextureLoadingContext"/> that points to the data stream
        /// for the specified <paramref name="texture"/>. This method is marked as <c>Obsolete</c>,
        /// and the recommended approach is to use the overload that accepts 
        /// <see cref="TextureLoadingContext"/> instead.
        /// </summary>
        /// <remarks>
        /// Returning <c>null</c> indicates that this mapper did not find a suitable data source, 
        /// so TriLib may attempt other mappers.
        /// </remarks>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> providing references to loaded model data, user 
        /// options, and other contextual information.
        /// </param>
        /// <param name="texture">
        /// The TriLib <see cref="ITexture"/> instance containing metadata (filename, embedded data pointers, etc.).
        /// </param>
        /// <returns>
        /// A <see cref="TextureLoadingContext"/> containing the data <see cref="System.IO.Stream"/> 
        /// if successful; otherwise <c>null</c>.
        /// </returns>
        [Obsolete("Please use the second override accepting a TextureLoadingContext.")]
        public virtual TextureLoadingContext Map(AssetLoaderContext assetLoaderContext, ITexture texture)
        {
            return null;
        }

        /// <summary>
        /// Attempts to retrieve or open the data stream for <paramref name="textureLoadingContext"/>. 
        /// By default, this method calls the obsolete <see cref="Map(AssetLoaderContext, ITexture)"/> method 
        /// to maintain backwards compatibility, then assigns its resulting <see cref="System.IO.Stream"/> 
        /// to <paramref name="textureLoadingContext"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// Holds data pertinent to loading a specific texture (e.g., the <see cref="ITexture"/> reference, 
        /// <see cref="AssetLoaderContext"/>, and any preexisting stream references).
        /// </param>
        public virtual void Map(TextureLoadingContext textureLoadingContext)
        {
            var newTextureLoadingContext = Map(textureLoadingContext.Context, textureLoadingContext.Texture);
            if (newTextureLoadingContext != null)
            {
                textureLoadingContext.Stream = newTextureLoadingContext.Stream;
            }
        }
    }
}
