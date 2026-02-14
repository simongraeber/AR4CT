using System;
using TriLibCore.Interfaces;

namespace TriLibCore
{
    /// <summary>
    /// Represents an Exception with a Context.
    /// </summary>
    public interface IContextualizedError : IAssetLoaderContext
    {
        /// <summary>Gets the Context Object.</summary>
        /// <returns>System.Object.</returns>
        object GetContext();
        /// <summary>Gets the Contextualized Error inner Exception.</summary>
        /// <returns>Exception.</returns>
        Exception GetInnerException();
    }
}