using System.IO;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>Represents a TriLib Texture.</summary>
    public interface ITexture : IObject
    {
        /// <summary>Gets a sub-texture, in case of layered Textures.</summary>
        /// <param name="index">The Sub-Texture index.</param>
        /// <returns>The sub-texture, if exists, otherwise, returns this instance.</returns>
        ITexture GetSubTexture(int index);

        /// <summary>Gets the sub-textures count. (Zero in case of a non-layered Texture)</summary>
        /// <returns>The sub-textures count.</returns>
        int GetSubTextureCount();

        /// <summary>Gets a sub-texture weight.</summary>
        /// <param name="index">The Sub-Texture index.</param>
        /// <returns>The sub-texture weight.</returns>
        float GetWeight(int index);

        /// <summary>Adds a sub-texture to the Texture.</summary>
        /// <param name="texture">The Sub-texture to be added.</param>
        void AddTexture(ITexture texture);

        /// <summary>
        /// Gets/Sets the embedded Texture pixel data stream, in case of embedded textures, otherwise, the value should be <c>null</c>.
        /// </summary>
        Stream DataStream { get; set; }

        /// <summary>Gets/Sets the Texture filename.</summary>
        string Filename { get; set; }

        /// <summary>Gets/Sets Texture horizontal Wrap Mode.</summary>
        TextureWrapMode WrapModeU { get; set; }

        /// <summary>Gets/Sets the Texture vertical Wrap Mode.</summary>
        TextureWrapMode WrapModeV { get; set; }

        /// <summary>Gets/Sets the Texture tilling.</summary>
        Vector2 Tiling { get; set; }

        /// <summary>Gets/Sets the Texture offset.</summary>
        Vector2 Offset { get; set; }

        /// <summary>Gets/Sets the  full path to the file when TriLib resolves it.</summary>
        string ResolvedFilename { get; set; }

        /// <summary>Checks if this Texture is valid.</summary>
        bool IsValid { get; }

        /// <summary>
        /// Gets/Sets the format of this texture.
        /// </summary>
        General.TextureFormat TextureFormat { get; set; }
    }
}