using System;
using System.Collections.Generic;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>Represents a system for dispatching Actions to execute on the Main Thread.</summary>
    public class Dispatcher : MonoBehaviour
    {
        //private static readonly List<IContextualizedAction> _actions = new List<IContextualizedAction>();

        private static readonly Queue<IContextualizedAction> Actions = new Queue<IContextualizedAction>();

        private static readonly object LockObject = new object();

        private static bool _dontDestroyOnLoad;

        private static Dispatcher _instance;

        private static bool _instanceExists;

        /// <summary>
        /// Gets/Sets whether the Dispatcher instance will be destroyed when a new level is loaded.
        /// </summary>
        public new static bool DontDestroyOnLoad
        {
            get => _instanceExists && _dontDestroyOnLoad;
            set
            {
                if (_instanceExists)
                {
                    return;
                }
                if (_dontDestroyOnLoad && !value)
                {
                    Debug.LogWarning("Disabling 'DontDestroyOnLoad' will destroy the existing Dispatcher instance and cancel all scheduled actions.");
                    Destroy(_instance.gameObject);
                    CheckInstance();
                }
                if (value)
                {
                    UnityEngine.Object.DontDestroyOnLoad(_instance.gameObject);
                }
                _dontDestroyOnLoad = value;
            }
        }
        /// <summary>Ensures a Dispatcher instance exists.</summary>
        public static void CheckInstance()
        {
            if (!_instanceExists)
            {
                var gameObject = new GameObject("Dispatcher");
                gameObject.AddComponent<Dispatcher>();
                gameObject.hideFlags = HideFlags.DontSave;
                _instanceExists = true;
            }
        }

        /// <summary>
        /// Queues an action to be invoked on the Main Thread, handling the async and sync calls automatically.
        /// </summary>
        /// <param name="action">The Action to be queued on the Main Thread.</param>
        /// <param name="context">The Context to be assigned to the Action.</param>
        public static void InvokeAsync<T>(Action<T> action, T context, bool async = true) where T : IAssetLoaderContext
        {
            var contextualizedAction = new ContextualizedAction<T>(action, context);
            InvokeContextualizedActionAsyncOrSync(contextualizedAction, async);
        }

        /// <summary>
        /// Queues an action to be invoked on the Main Thread.
        /// </summary>
        /// <param name="action">The Action to be queued on the Main Thread.</param>
        public static void InvokeAsync(Action action, bool async = true)
        {
            InvokeContextualizedActionAsyncOrSync(new ContextualizedAction(action), async);
        }

        /// <summary>
        /// Queues an action to be invoked on the Main Thread.
        /// </summary>
        /// <param name="action">The Action to be queued on the Main Thread.</param>
        /// <param name="context">The Context to be assigned to the Action.</param>
        public static void InvokeAsyncUnchecked<T>(Action<T> action, T context, bool async = true)
        {
            InvokeContextualizedActionAsyncOrSync(new ContextualizedAction<T>(action, context), async);
        }

        private static void InvokeContextualizedActionAsyncOrSync(IContextualizedAction action, bool async)
        {
            if (async)
            {
                lock (LockObject)
                {
                    Actions.Enqueue(action);
                }
            }
            else
            {
                action.Invoke();
            }
        }

        private void Awake()
        {
            if (_instance)
            {
                DestroyImmediate(this);
            }
            else
            {
                _instance = this;
                _instanceExists = true;
                GameObject.DontDestroyOnLoad(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
                _instanceExists = false;
            }
        }

        private void Update()
        {
            lock (LockObject)
            {
                while (Actions.Count > 0)
                {
                    var contextualizedAction = Actions.Dequeue();
                    try
                    {
                        contextualizedAction.Invoke();
                    }
                    catch (Exception exception)
                    {
                        Actions.Clear();
                        var assetLoaderContextualizedAction = contextualizedAction as ContextualizedAction<AssetLoaderContext>;
                        var context = assetLoaderContextualizedAction?.Context ?? contextualizedAction?.GetContext();
                        var callback = context?.HandleError ?? context?.OnError;
                        if (callback != null)
                        {
                            var contextualizedError = exception as IContextualizedError ?? new ContextualizedError<AssetLoaderContext>(exception, context);
                            callback(contextualizedError);
                            return;
                        }
                        throw new Exception("There was an error while running a dispatch on the main thread." + contextualizedAction, exception);
                    }
                }
            }
        }
    }
}
