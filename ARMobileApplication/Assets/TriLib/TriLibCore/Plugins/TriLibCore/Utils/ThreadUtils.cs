using System;
using System.Threading;
using System.Threading.Tasks;
using TriLibCore.General;
using TriLibCore.Interfaces;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for creating and managing threads (or tasks) in TriLib, 
    /// offering asynchronous execution, error handling, cancellation, and progress reporting.
    /// </summary>
    public static class ThreadUtils
    {
        /// <summary>
        /// Starts a new task (thread) to execute <paramref name="onStart"/> using the provided <typeparamref name="T"/> context. 
        /// If the AssetLoaderContext Async property is <c>false</c>, 
        /// this method will execute <paramref name="onStart"/> synchronously on the main thread.
        /// </summary>
        /// <typeparam name="T">A class type that implements <see cref="IAssetLoaderContext"/>, representing the thread's shared context.</typeparam>
        /// <param name="context">The context which will be passed to the <paramref name="onStart"/> action.</param>
        /// <param name="onStart">The action to execute on a background thread (if asynchronous) or on the main thread (if synchronous).</param>
        /// <param name="onComplete">
        /// An optional action to call on the main thread once <paramref name="onStart"/> completes.
        /// If <c>null</c>, no completion action is invoked.
        /// </param>
        /// <param name="onError">
        /// An optional action to call on the main thread if an exception occurs. 
        /// If not provided, the error is rethrown on the main thread as a <see cref="ContextualizedError{T}"/>.
        /// </param>
        /// <param name="timeout">The thread timeout in seconds. If nonzero, the operation is canceled after this duration.</param>
        /// <param name="name">An optional name for the worker thread.</param>
        /// <param name="startImmediately">
        /// <see langword="true"/> to start the task immediately; <see langword="false"/> to create it without starting.
        /// </param>
        /// <param name="onCompleteSameThread">
        /// An optional action to execute on the worker thread immediately after <paramref name="onStart"/> finishes, 
        /// and before <paramref name="onComplete"/> (which is invoked on the main thread).
        /// </param>
        /// <returns>
        /// A <see cref="Task"/> representing the asynchronous operation if AssetLoaderContext Async is <c>true</c>;
        /// otherwise, <c>null</c> if executed synchronously on the main thread.
        /// </returns>
        public static Task RequestNewThreadFor<T>(
            T context,
            Action<T> onStart,
            Action<T> onComplete = null,
            Action<IContextualizedError> onError = null,
            int timeout = 0,
            string name = null,
            bool startImmediately = true,
            Action<T> onCompleteSameThread = null) where T : class, IAssetLoaderContext
        {
            if (context.Context.CancellationToken == CancellationToken.None)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                if (timeout != 0)
                {
                    cancellationTokenSource.CancelAfter(timeout * 1000);
                }
                context.Context.CancellationToken = cancellationTokenSource.Token;
                context.Context.CancellationTokenSource = cancellationTokenSource;
            }

            // If asynchronous loading is enabled, run on a background task.
            if (context.Context.Async)
            {
                Task task;
                if (startImmediately)
                {
                    task = Task.Run(() =>
                    {
#if !ENABLE_IL2CPP
                        if (!string.IsNullOrWhiteSpace(name))
                        {
                            Thread.CurrentThread.Name = name;
                        }
#endif
                        try
                        {
                            onStart(context);
                            onCompleteSameThread?.Invoke(context);
                            if (onComplete != null)
                            {
                                Dispatcher.InvokeAsync(onComplete, context, context.Context.Async);
                            }
                        }
                        catch (Exception exception)
                        {
                            var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                            Dispatcher.InvokeAsyncUnchecked(onError ?? ReThrow, contextualizedError, context.Context.Async);
                        }
                    });
                }
                else
                {
                    // Create the task without starting it immediately.
                    task = new Task(() =>
                    {
                        try
                        {
                            onStart(context);
                            onCompleteSameThread?.Invoke(context);
                            if (onComplete != null)
                            {
                                Dispatcher.InvokeAsync(onComplete, context, context.Context.Async);
                            }
                        }
                        catch (Exception exception)
                        {
                            var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                            var errorCallback = onError ?? ReThrow;
                            errorCallback(contextualizedError);
                        }
                    }, context.Context.CancellationToken);
                    task.Start();
                }

                context.Context.Tasks.Add(task);
                return task;
            }

            // If not async, run everything synchronously on the main thread.
            try
            {
                onStart(context);
                onCompleteSameThread?.Invoke(context);
                onComplete?.Invoke(context);
            }
            catch (Exception exception)
            {
                if (onError != null)
                {
                    var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                    onError(contextualizedError);
                }
                else
                {
                    var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                    throw contextualizedError;
                }
            }
            return null;
        }

        /// <summary>
        /// Starts a lightweight task on a new thread, optionally waiting before execution and without 
        /// specifying a context, completion callback, or error callback.
        /// </summary>
        /// <param name="onStart">The action to execute on the new thread.</param>
        /// <param name="timeout">
        /// The thread timeout in seconds, not currently applied. Provided for API consistency.
        /// </param>
        /// <param name="name">An optional name for the thread.</param>
        /// <param name="startImmediately">
        /// <see langword="true"/> to start the task immediately; <see langword="false"/> to create it without starting.
        /// </param>
        /// <param name="waitMilliseconds">The delay in milliseconds before <paramref name="onStart"/> is executed.</param>
        /// <returns>A <see cref="Task"/> representing the new thread.</returns>
        public static Task RunThreadSimple(
            Action onStart,
            int timeout = 0,
            string name = null,
            bool startImmediately = true,
            int waitMilliseconds = 0)
        {
            var task = new Task(() =>
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Thread.CurrentThread.Name = name;
                }
                if (waitMilliseconds > 0)
                {
                    Thread.Sleep(waitMilliseconds);
                }
                onStart();
            });

            if (startImmediately)
            {
                task.Start();
            }
            return task;
        }

        /// <summary>
        /// Starts a new task on a background thread using the specified context <typeparamref name="T"/> 
        /// and a <paramref name="cancellationToken"/>. Once execution completes (or fails), the optional <paramref name="onComplete"/> 
        /// or <paramref name="onError"/> will be invoked on the main thread.
        /// </summary>
        /// <typeparam name="T">
        /// The context type that implements <see cref="IAssetLoaderContext"/>; 
        /// it is passed to <paramref name="onStart"/> and other callbacks.
        /// </typeparam>
        /// <param name="context">The asset loader context on which this thread operation should act.</param>
        /// <param name="cancellationToken">
        /// A <see cref="CancellationToken"/> that can be used to halt the operation if canceled. 
        /// If <c>None</c>, a new token is created with an optional timeout.
        /// </param>
        /// <param name="onStart">The action to execute on the new thread.</param>
        /// <param name="onComplete">
        /// An optional action invoked on the main thread once <paramref name="onStart"/> completes.
        /// </param>
        /// <param name="onError">
        /// An optional action invoked on the main thread if an exception occurs. 
        /// If not specified, the error is rethrown on the main thread as a <see cref="ContextualizedError{T}"/>.
        /// </param>
        /// <param name="timeout">The thread timeout in seconds. If nonzero, the task is canceled after this duration.</param>
        /// <param name="name">An optional name for the thread.</param>
        /// <param name="startImmediately">
        /// <see langword="true"/> to start the task immediately; <see langword="false"/> to create it without starting.
        /// </param>
        /// <returns>The <see cref="Task"/> that was created, possibly not started if <paramref name="startImmediately"/> is <c>false</c>.</returns>
        public static Task RunThread<T>(
            T context,
            ref CancellationToken cancellationToken,
            Action<T> onStart,
            Action<T> onComplete = null,
            Action<IContextualizedError> onError = null,
            int timeout = 0,
            string name = null,
            bool startImmediately = true) where T : IAssetLoaderContext
        {
            if (cancellationToken == CancellationToken.None)
            {
                var cancellationTokenSource = new CancellationTokenSource();
                if (timeout != 0)
                {
                    cancellationTokenSource.CancelAfter(timeout * 1000);
                }
                cancellationToken = cancellationTokenSource.Token;
                context.Context.CancellationTokenSource = cancellationTokenSource;
            }

            var task = new Task(() =>
            {
                if (!string.IsNullOrWhiteSpace(name))
                {
                    Thread.CurrentThread.Name = name;
                }

                try
                {
                    var contextualizedAction = new ContextualizedAction<T>(onStart, context);
                    contextualizedAction.Invoke();

                    if (onComplete != null)
                    {
                        Dispatcher.InvokeAsync(onComplete, context, context.Context.Async);
                    }
                }
                catch (Exception exception)
                {
                    if (onError != null)
                    {
                        var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                        Dispatcher.InvokeAsyncUnchecked(onError, contextualizedError, context.Context.Async);
                    }
                    else
                    {
                        var contextualizedError = exception as ContextualizedError<T> ?? new ContextualizedError<T>(exception, context);
                        Dispatcher.InvokeAsyncUnchecked(ReThrow, contextualizedError, context.Context.Async);
                    }
                }
            }, cancellationToken);

            if (startImmediately)
            {
                task.Start();
            }

            context.Context.Tasks.Add(task);
            return task;
        }

        /// <summary>
        /// Rethrows the specified <see cref="IContextualizedError"/> on the main thread by throwing its inner exception.
        /// </summary>
        /// <param name="contextualizedError">The error containing the context and exception.</param>
        private static void ReThrow(IContextualizedError contextualizedError)
        {
            throw contextualizedError.GetInnerException();
        }
    }
}
