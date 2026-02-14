using System;
using TriLibCore.Interfaces;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides methods for comparing and hashing <see cref="ITexture"/> instances.
    /// </summary>
    public static class TextureComparators
    {
        /// <summary>
        /// Determines whether two <see cref="ITexture"/> instances represent the same texture.
        /// This method primarily checks if both textures have the same short file name or the same name.
        /// </summary>
        /// <param name="a">The first texture to compare.</param>
        /// <param name="b">The second texture to compare.</param>
        /// <returns>
        /// <see langword="true"/> if both textures are considered equal; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool TextureEquals(ITexture a, ITexture b)
        {
            // If 'b' is null, comparison is automatically false.
            // Otherwise, compare the short file names or texture names.
            var equals = b != null && (CompareFilenameSafe(a.Filename, b.Filename) || a.Name == b.Name);
            return equals;
        }

        /// <summary>
        /// Safely compares two file names by using only the short file names (i.e., without directory paths).
        /// </summary>
        /// <param name="a">The first file name.</param>
        /// <param name="b">The second file name.</param>
        /// <returns>
        /// <see langword="true"/> if the short file names match; otherwise, <see langword="false"/>.
        /// If both file names are <see langword="null"/>, returns <see langword="false"/>.
        /// </returns>
        private static bool CompareFilenameSafe(string a, string b)
        {
            if (a == null && b == null)
            {
                return false;
            }
            if (a != null)
            {
                a = FileUtils.GetShortFilename(a);
                if (b != null)
                {
                    b = FileUtils.GetShortFilename(b);
                }
            }
            return a == b;
        }

        /// <summary>
        /// Determines whether an <see cref="ITexture"/> instance is equal to the specified object.
        /// </summary>
        /// <param name="a">The texture to compare.</param>
        /// <param name="b">The object to compare against.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="b"/> is an <see cref="ITexture"/> and represents the same texture as <paramref name="a"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool Equals(ITexture a, object b)
        {
            // If 'b' is not an ITexture, return false immediately.
            return b is ITexture other && TextureEquals(a, other);
        }

        /// <summary>
        /// Generates a hash code for the specified <see cref="ITexture"/> using its short file name or name.
        /// </summary>
        /// <param name="a">The texture for which to generate a hash code.</param>
        /// <returns>
        /// An integer hash code suitable for use in data structures like hash tables, or zero if <paramref name="a"/> is <see langword="null"/>.
        /// </returns>
        public static int GetHashCode(ITexture a)
        {
            var hash = 0;
            if (a == null)
            {
                return hash;
            }

            if (!string.IsNullOrEmpty(a.Filename))
            {
                // Attempt to get a short file name to hash.
                var shortFilename = FileUtils.GetShortFilename(a.Filename);
                if (!string.IsNullOrEmpty(shortFilename))
                {
                    hash = shortFilename.GetHashCode();
                }
                // If the short file name is empty, fall back to the texture's name.
                else if (!string.IsNullOrEmpty(a.Name))
                {
                    hash = a.Name.GetHashCode();
                }
            }

            return hash;
        }
    }
}
