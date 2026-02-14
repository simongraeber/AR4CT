using System.IO;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore
{
    /// <summary>
    /// Contains data and state information for loading and processing a texture 
    /// during a TriLib model import. This context stores both the original source texture 
    /// data and the resulting Unity texture, along with relevant metadata.
    /// </summary>
    public class TextureDataContext : IAssetLoaderContext, IAwaitable
    {
        /// <summary>
        /// The original texture as defined by the source model.
        /// </summary>
        public ITexture Texture;

        /// <summary>
        /// The Unity texture created from the source data.
        /// </summary>
        public Texture OriginalUnityTexture;

        /// <summary>
        /// The width of the texture (in pixels).
        /// </summary>
        public int Width;

        /// <summary>
        /// The height of the texture (in pixels).
        /// </summary>
        public int Height;

        /// <summary>
        /// The original data stream for the texture.
        /// </summary>
        public Stream Stream;

        /// <summary>
        /// The number of color components in the source image.
        /// </summary>
        public int Components;

        /// <summary>
        /// Indicates whether the source texture uses its alpha channel.
        /// </summary>
        public bool HasAlpha;

        /// <summary>
        /// Indicates whether the Unity texture has been successfully loaded.
        /// </summary>
        public bool TextureLoaded;

        /// <summary>
        /// Indicates whether the Unity texture has been created.
        /// </summary>
        public bool TextureCreated;

        /// <summary>
        /// Gets or sets a value indicating whether the texture loading process is complete.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AssetLoaderContext"/> that contains the overall model loading data.
        /// </summary>
        public AssetLoaderContext Context { get; set; }
    }
}
