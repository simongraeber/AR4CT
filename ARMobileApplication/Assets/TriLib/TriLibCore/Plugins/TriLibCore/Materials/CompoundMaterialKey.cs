using System;
using TriLibCore.Interfaces;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a compound key composed of a material reference and a texture type.
    /// </summary>
    /// <remarks>
    /// This key is typically used in dictionaries or hash-based collections where
    /// both the material instance and the texture type must be considered to uniquely
    /// identify an entry.
    /// </remarks>
    public struct CompoundMaterialKey : IEquatable<CompoundMaterialKey>
    {
        /// <summary>
        /// Gets or sets the material associated with this key.
        /// </summary>
        public IMaterial Material;

        /// <summary>
        /// Gets or sets the texture type associated with this key.
        /// </summary>
        public TextureType Type;

        /// <summary>
        /// Initializes a new instance of the <see cref="CompoundMaterialKey"/> struct.
        /// </summary>
        /// <param name="material">The material component of the key.</param>
        /// <param name="type">The texture type component of the key.</param>
        public CompoundMaterialKey(IMaterial material, TextureType type)
        {
            Material = material;
            Type = type;
        }

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="CompoundMaterialKey"/>.
        /// </summary>
        /// <param name="other">The other key to compare.</param>
        /// <returns>
        /// <c>true</c> if both the material and texture type match; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(CompoundMaterialKey other)
        {
            return Equals(Material, other.Material) && Type == other.Type;
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="CompoundMaterialKey"/> and is equal;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is CompoundMaterialKey other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this instance based on the material and texture type.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return ((Material != null ? Material.GetHashCode() : 0) * 397) ^ (int)Type;
            }
        }

        /// <summary>
        /// Returns a string that represents the current key.
        /// </summary>
        /// <returns>
        /// A string in the format <c>"MaterialName|TextureType"</c>.
        /// </returns>
        public override string ToString()
        {
            return $"{Material.Name}|{Type}";
        }
    }
}
