using UnityEngine;

namespace TriLibCore.Textures
{
    /// <summary>
    /// Represents a class containing the reference to all TriLib default textures.
    /// </summary>
    public static class DefaultTextures
    {
        /// <summary>
        /// The default white texture.
        /// </summary>
        public static Texture2D White => Resources.Load<Texture2D>("Textures/TriLibWhite");
    }
}
