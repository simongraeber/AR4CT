namespace TriLibCore.General
{
    /// <summary>
    /// Represents tangent interpolation modes.
    /// </summary>
    public enum TangentMode
    {
        /// <summary>
        /// Editable tangent mode.
        /// </summary>
        Editable = 0,
        /// <summary>
        /// Smooth tangent mode.
        /// </summary>
        Smooth = 1,
        /// <summary>
        /// Linear tangent mode.
        /// </summary>
        Linear = 2,
        /// <summary>
        /// Stepped tangent mode.
        /// </summary>
        Stepped = Linear | Smooth
    }
}