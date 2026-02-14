using System;
using TriLibCore.Interfaces;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides a lightweight wrapper around <see cref="AssetLoaderContext"/> to allow
    /// dispatching completion callbacks while exposing the asset loader context.
    /// </summary>
    internal class DispatcherContext : IAssetLoaderContext
    {
        /// <summary>
        /// Callback invoked when the asset loading process finishes.
        /// </summary>
        public Action<AssetLoaderContext> OnFinish;

        /// <summary>
        /// Gets or sets the underlying asset loader context.
        /// </summary>
        public AssetLoaderContext Context { get; set; }
    }
}
