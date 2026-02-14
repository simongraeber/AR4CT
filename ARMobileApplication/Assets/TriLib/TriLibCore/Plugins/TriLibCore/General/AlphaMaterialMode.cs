namespace TriLibCore.General
{
    /// <summary>
    /// Represents methods to handle alpha (transparent) materials.
    /// </summary>
    public enum AlphaMaterialMode
    {
        /// <summary>
        /// Does not create any alpha material and uses opaque materials instead.
        /// </summary>
        None,
        /// <summary>
        /// Creates cutout alpha materials where applicable.
        /// </summary>
        Cutout,
        /// <summary>
        /// Creates transparent (alpha) materials where applicable.
        /// </summary>
        Transparent,
        /// <summary>
        /// Creates both materials and uses the second one as a copy from the semi-transparent mesh.
        /// </summary>
        CutoutAndTransparent
    }
}
