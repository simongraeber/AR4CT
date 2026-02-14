using System;
using System.IO;
using TriLibCore.Interfaces;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a compound key composed of a texture reference and a texture type.
    /// </summary>
    /// <remarks>
    /// This key is typically used in dictionaries or hash-based collections where
    /// both the texture instance and its semantic type must be considered to uniquely
    /// identify an entry.
    /// </remarks>
    public struct CompoundTextureKey : IEquatable<CompoundTextureKey>
    {
        /// <summary>
        /// Gets or sets the texture associated with this key.
        /// </summary>
        public ITexture Texture;

        /// <summary>
        /// Gets or sets the texture type associated with this key.
        /// </summary>
        public TextureType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundTextureKey"/> struct.
        /// </summary>
        /// <param name="texture">The texture component of the key.</param>
        /// <param name="type">The texture type component of the key.</param>
        public CompoundTextureKey(ITexture texture, TextureType type)
        {
            Texture = texture;
            Type = type;
        }

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="CompoundTextureKey"/>.
        /// </summary>
        /// <param name="other">The other key to compare.</param>
        /// <returns>
        /// <c>true</c> if both the texture and texture type match; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(CompoundTextureKey other)
        {
            var equals = Equals(Texture, other.Texture);
            return equals && Type == other.Type;
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="CompoundTextureKey"/> and is equal;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is CompoundTextureKey other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this instance based on the texture and texture type.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Texture?.GetHashCode() ?? 0;
                return (hashCode * 397) ^ (int)Type;
            }
        }

        /// <summary>
        /// Gets the resolved filename associated with the underlying texture, if available.
        /// </summary>
        /// <remarks>
        /// This value is typically used for debugging, logging, or asset resolution purposes.
        /// </remarks>
        public string ResolvedFilename
        {
            get { return Texture?.ResolvedFilename; }
        }
    }
}
