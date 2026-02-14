namespace TriLibCore.Interfaces
{

    /// <summary>
    /// Represents an awaitable process.
    /// </summary>
    public interface IAwaitable
    {
        /// <summary>
        /// Indicates whether this Awaitable is completed.
        /// </summary>
        bool Completed { get; set; }
    }
}
