using System;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents an exception generated on a worker thread that carries
    /// an associated context object.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the context object associated with the error.
    /// </typeparam>
    public class ContextualizedError<T> : Exception, IContextualizedError
    {
        /// <summary>
        /// Context object passed to the thread that generated the exception.
        /// </summary>
        public readonly T ErrorContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualizedError{T}"/> class
        /// using the given inner exception and context object.
        /// </summary>
        /// <param name="innerException">
        /// The underlying exception that caused the error.
        /// </param>
        /// <param name="errorContext">
        /// The context object associated with the error.
        /// </param>
        public ContextualizedError(Exception innerException, T errorContext)
            : base("A contextualized error has occurred.", innerException)
        {
            ErrorContext = errorContext;
        }

        /// <summary>
        /// Gets the context object associated with this error.
        /// </summary>
        /// <returns>
        /// The context object originally passed to the thread.
        /// </returns>
        public object GetContext()
        {
            return ErrorContext;
        }

        /// <summary>
        /// Gets the inner exception that caused this contextualized error.
        /// </summary>
        /// <returns>
        /// The underlying <see cref="Exception"/> instance.
        /// </returns>
        public Exception GetInnerException()
        {
            return InnerException;
        }

        /// <summary>
        /// Gets or sets the asset loader context that was active when the error
        /// was generated.
        /// </summary>
        public AssetLoaderContext Context { get; set; }
    }
}
