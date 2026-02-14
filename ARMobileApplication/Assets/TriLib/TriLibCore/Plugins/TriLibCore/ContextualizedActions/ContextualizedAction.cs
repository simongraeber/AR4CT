using System;
using TriLibCore.Interfaces;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents an <see cref="Action"/> that can be executed on a worker thread
    /// without an associated execution context.
    /// </summary>
    public class ContextualizedAction : IContextualizedAction
    {
        /// <summary>
        /// Action to be invoked on the worker thread.
        /// </summary>
        public readonly Action Action;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualizedAction"/> class
        /// using the given action.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        public ContextualizedAction(Action action)
        {
            Action = action;
        }

        /// <summary>
        /// Gets a value indicating whether the action has completed execution.
        /// This implementation always returns <c>false</c> because no awaitable
        /// context is associated with this action.
        /// </summary>
        public bool Completed => false;

        /// <summary>
        /// Gets the asset loader context associated with this action.
        /// This implementation always returns <c>null</c>.
        /// </summary>
        /// <returns>
        /// Always <c>null</c>, as this action does not provide an execution context.
        /// </returns>
        public AssetLoaderContext GetContext()
        {
            return null;
        }

        /// <summary>
        /// Invokes the action on the worker thread.
        /// </summary>
        public void Invoke()
        {
            Action();
        }

        /// <summary>
        /// Returns a string that represents the current action.
        /// </summary>
        /// <returns>
        /// A string containing the target type and method name of the action,
        /// or <c>Invalid Action</c> if the action is <c>null</c>.
        /// </returns>
        public override string ToString()
        {
            return Action != null
                ? $"{(Action.Target != null ? Action.Target.GetType().Name : "No-Target")}.{Action.Method.Name}"
                : "Invalid Action";
        }
    }

    /// <summary>
    /// Represents an <see cref="Action{T}"/> that can be executed on a worker thread
    /// with an associated context object.
    /// </summary>
    /// <typeparam name="T">
    /// The type of the context object passed to the action.
    /// </typeparam>
    public class ContextualizedAction<T> : IContextualizedAction
    {
        /// <summary>
        /// Action to be invoked on the worker thread.
        /// </summary>
        public readonly Action<T> Action;

        /// <summary>
        /// Context object passed to the action during execution.
        /// </summary>
        public readonly T Context;

        /// <summary>
        /// Initializes a new instance of the <see cref="ContextualizedAction{T}"/> class
        /// using the given action and context.
        /// </summary>
        /// <param name="action">The action to be executed.</param>
        /// <param name="context">The context object passed to the action.</param>
        public ContextualizedAction(Action<T> action, T context)
        {
            Action = action;
            Context = context;
        }

        /// <summary>
        /// Gets a value indicating whether the action has completed execution.
        /// If the context implements <see cref="IAwaitable"/>, this value reflects
        /// the awaitable completion state; otherwise, it always returns <c>false</c>.
        /// </summary>
        public bool Completed
        {
            get
            {
                if (Context is IAwaitable awaitable)
                {
                    return awaitable.Completed;
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the asset loader context associated with this action, if available.
        /// </summary>
        /// <returns>
        /// The underlying <see cref="AssetLoaderContext"/> if the context implements
        /// <see cref="IAssetLoaderContext"/>; otherwise, <c>null</c>.
        /// </returns>
        public AssetLoaderContext GetContext()
        {
            return (Context as IAssetLoaderContext)?.Context;
        }

        /// <summary>
        /// Invokes the action on the worker thread, updating the awaitable completion
        /// state before and after execution when supported.
        /// </summary>
        public void Invoke()
        {
            var awaitable = Context as IAwaitable;
            if (awaitable != null)
            {
                awaitable.Completed = false;
            }

            Action(Context);

            if (awaitable != null)
            {
                awaitable.Completed = true;
            }
        }

        /// <summary>
        /// Returns a string that represents the current action and its completion state.
        /// </summary>
        /// <returns>
        /// A string containing the target type, method name, and completion status,
        /// or <c>Invalid Action</c> if the action is <c>null</c>.
        /// </returns>
        public override string ToString()
        {
            return Action != null
                ? $"{(Action.Target != null ? Action.Target.GetType().Name : "No-Target")}.{Action.Method.Name}. Completed: {Completed}"
                : "Invalid Action";
        }
    }
}
