namespace TriLibCore.General
{
    /// <summary>Represents rigging setup types.</summary>
    public enum AnimationType
    {
        /// <summary>
        /// Legacy rigging type. Adds an Animation Component to the created Game Object.
        /// </summary>
        Legacy,
        /// <summary>
        /// Generic rigging type. Adds an Animator Component to the created Game Object.
        /// </summary>
        Generic,
        /// <summary>
        /// Humanoid rigging type. Adds an Animator Component to the created Game Object and uses any Humanoid Avatar Mapper configured on the Asset Loader Options.
        /// </summary>
        Humanoid,
        /// <summary>
        /// No rigging type.
        /// </summary>
        None
    }
}