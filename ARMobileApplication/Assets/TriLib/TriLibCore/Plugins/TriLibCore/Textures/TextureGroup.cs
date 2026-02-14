using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a Texture Group, containing the TriLib and Unity Texture.
    /// </summary>
    public struct TextureGroup 
    {
        /// <summary>
        /// Original Unity Texture (without any processing).
        /// </summary>
        public Texture OriginalUnityTexture;

        /// <summary>
        /// Unity Texture (can be processed).
        /// </summary>
        public Texture UnityTexture;

        /// <summary>
        /// TriLib Texture.
        /// </summary>
        public ITexture Texture;

        /// <summary>
        /// Creates a new TextureGroup using the given parameters.
        /// </summary>
        /// <param name="originalUnityTexture">Original Unity Texture (without any processing).</param>
        /// <param name="unityTexture">Unity Texture (can be processed).</param>
        /// <param name="texture">TriLib Texture.</param>
        public TextureGroup(Texture originalUnityTexture, Texture unityTexture, ITexture texture)
        {
            OriginalUnityTexture = originalUnityTexture;
            UnityTexture = unityTexture;
            Texture = texture;
        }
    }
}