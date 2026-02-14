namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents an interface for objects that provide access 
    /// to the main <see cref="AssetLoaderContext"/>, which holds 
    /// model-loading data and other associated resources.
    /// </summary>
    public interface IAssetLoaderContext
    {
        /// <summary>
        /// Gets the <see cref="AssetLoaderContext"/> reference. 
        /// This context contains metadata, settings, and resources used 
        /// during the model loading process.
        /// </summary>
        AssetLoaderContext Context { get; }
    }
}
