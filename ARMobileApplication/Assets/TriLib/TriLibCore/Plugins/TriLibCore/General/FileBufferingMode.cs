namespace TriLibCore
{
    /// <summary>
    /// Specifies the buffering behavior for file input streams.
    /// </summary>
    public enum FileBufferingMode
    {
        /// <summary>
        /// File buffering is disabled, meaning streams are read directly without being fully loaded into memory.
        /// </summary>
        Disabled,

        /// <summary>
        /// File buffering is enabled only for files that are smaller than 50 MB.
        /// Larger files are read directly without being buffered into memory.
        /// </summary>
        SmallFilesOnly,

        /// <summary>
        /// File buffering is always enabled, regardless of file size.
        /// Streams are fully loaded into memory before processing.
        /// </summary>
        Always
    }
}
