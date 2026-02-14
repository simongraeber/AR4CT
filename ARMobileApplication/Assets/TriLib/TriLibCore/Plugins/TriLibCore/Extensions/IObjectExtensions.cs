using System;
using System.Collections.Generic;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Provides extension methods for disposing objects that may or may not implement <see cref="IDisposable"/>.
    /// </summary>
    public static class IObjectExtensions
    {
        /// <summary>
        /// Attempts to dispose the specified <paramref name="obj"/> if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="T">The type parameter representing the object's value type.</typeparam>
        /// <param name="obj">The object to attempt disposing.</param>
        /// <remarks>
        /// If <paramref name="obj"/> does not implement <see cref="IDisposable"/>, this method will do nothing.
        /// No exception will be thrown if the cast to <see cref="IDisposable"/> fails.
        /// </remarks>
        public static void TryToDispose<T>(this object obj)
        {
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }

        /// <summary>
        /// Attempts to dispose the specified <see cref="IList{T}"/> if it implements <see cref="IDisposable"/>.
        /// </summary>
        /// <typeparam name="T">The type parameter representing the items of the list.</typeparam>
        /// <param name="obj">The <see cref="IList{T}"/> to attempt disposing.</param>
        /// <remarks>
        /// If <paramref name="obj"/> does not implement <see cref="IDisposable"/>, this method will do nothing.
        /// No exception will be thrown if the cast to <see cref="IDisposable"/> fails.
        /// </remarks>
        public static void TryToDispose<T>(this IList<T> obj)
        {
            if (obj is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
