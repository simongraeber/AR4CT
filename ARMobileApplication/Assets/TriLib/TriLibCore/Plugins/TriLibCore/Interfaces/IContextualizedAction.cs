namespace TriLibCore.Interfaces
{
    /// <summary>Represents a Contextualized Action interface.</summary>
    public interface IContextualizedAction
    {
        /// <summary>Invokes this Action without using any context.</summary>
        void Invoke();

        /// <summary>
        /// Returns the Context used in this Action.
        /// </summary>
        /// <returns></returns>
        AssetLoaderContext GetContext();

        /// <summary>
        /// Indicates whether this Action is completed.
        /// </summary>
        bool Completed { get; }
    }
}