using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TriLibCore
{
    /// <summary>
    /// Serves as the central context for managing a model loading operation in TriLib. 
    /// This class stores references to the loaded objects (GameObjects, textures, materials),
    /// user options, callbacks, and intermediate state (e.g., asynchronous tasks, in-flight texture loads).
    /// </summary>
    public class AssetLoaderContext : IAssetLoaderContext, IAwaitable
    {
        /// <summary>
        /// A list of Unity objects (e.g., <see cref="Texture2D"/>, <see cref="Material"/>, <see cref="Mesh"/>)
        /// that have been allocated during the model loading process.
        /// </summary>
        public readonly List<Object> Allocations = new List<Object>();

        /// <summary>
        /// A mapping between created <see cref="GameObject"/>s and their hierarchy paths
        /// within the loaded model (e.g., “Root/Mesh001”). This is useful for tracking 
        /// or debugging the final hierarchy after import.
        /// </summary>
        public readonly Dictionary<GameObject, string> GameObjectPaths = new Dictionary<GameObject, string>();

        /// <summary>
        /// A mapping between TriLib model representations (<see cref="IModel"/>) 
        /// and the corresponding <see cref="GameObject"/>s created for them in Unity.
        /// </summary>
        public readonly Dictionary<IModel, GameObject> GameObjects = new Dictionary<IModel, GameObject>();

        /// <summary>
        /// A thread-safe dictionary mapping TriLib materials (<see cref="IMaterial"/>)
        /// to Unity <see cref="Material"/> objects generated on-the-fly. 
        /// This can be used to look up or reuse already created materials.
        /// </summary>
        public readonly ConcurrentDictionary<IMaterial, Material> GeneratedMaterials = new ConcurrentDictionary<IMaterial, Material>();

        /// <summary>
        /// A thread-safe dictionary storing compound textures keyed by a <see cref="CompoundTextureKey"/>, 
        /// which represents the texture plus its usage type (e.g., diffuse, normal, metallic).
        /// Each entry references the <see cref="TextureLoadingContext"/> used to load that texture.
        /// </summary>
        public readonly ConcurrentDictionary<CompoundTextureKey, TextureLoadingContext> LoadedCompoundTextures
            = new ConcurrentDictionary<CompoundTextureKey, TextureLoadingContext>();

        /// <summary>
        /// A thread-safe dictionary that maps shortened external resource filenames 
        /// to their fully resolved paths on disk or elsewhere.
        /// </summary>
        public readonly ConcurrentDictionary<string, string> LoadedExternalData
            = new ConcurrentDictionary<string, string>();

        /// <summary>
        /// A thread-safe dictionary linking TriLib materials (<see cref="IMaterial"/>) to the 
        /// generated Unity <see cref="Material"/> objects. This complements <see cref="GeneratedMaterials"/> 
        /// in storing final user-facing materials.
        /// </summary>
        public readonly ConcurrentDictionary<IMaterial, Material> LoadedMaterials
            = new ConcurrentDictionary<IMaterial, Material>();

        /// <summary>
        /// A thread-safe dictionary mapping TriLib textures (<see cref="ITexture"/>) 
        /// to their <see cref="TextureLoadingContext"/> results, used to prevent redundant 
        /// texture loading and allow reuse where possible.
        /// </summary>
        public readonly ConcurrentDictionary<ITexture, TextureLoadingContext> LoadedTextures
            = new ConcurrentDictionary<ITexture, TextureLoadingContext>();

        /// <summary>
        /// A thread-safe dictionary linking TriLib materials (<see cref="IMaterial"/>) to their 
        /// <see cref="MaterialRendererContext"/> objects, which contain references to the Unity 
        /// renderer and geometry data needing that material.
        /// </summary>
        public readonly ConcurrentDictionary<IMaterial, List<MaterialRendererContext>> MaterialRenderers
            = new ConcurrentDictionary<IMaterial, List<MaterialRendererContext>>();

        /// <summary>
        /// A thread-safe dictionary linking a <see cref="CompoundMaterialKey"/> (material + texture type) 
        /// to the loaded <see cref="TextureLoadingContext"/>. This ensures that a texture used 
        /// in multiple material slots is loaded or referenced consistently.
        /// </summary>
        public readonly ConcurrentDictionary<CompoundMaterialKey, TextureLoadingContext> MaterialTextures
            = new ConcurrentDictionary<CompoundMaterialKey, TextureLoadingContext>();

        /// <summary>
        /// A mapping between <see cref="GameObject"/>s created during loading and 
        /// the corresponding TriLib <see cref="IModel"/> representations (e.g., for referencing 
        /// raw geometry data).
        /// </summary>
        public readonly Dictionary<GameObject, IModel> Models = new Dictionary<GameObject, IModel>();

        /// <summary>
        /// Tracks whether particular <see cref="Texture"/> objects have already been checked for alpha channels,
        /// avoiding repeated checks or overhead.
        /// </summary>
        public readonly Dictionary<Texture, bool> TexturesWithAlphaChecked = new Dictionary<Texture, bool>();

        /// <summary>
        /// Keeps a set of textures that have been successfully applied to renderers,
        /// which helps differentiate between used and unused textures 
        /// (for optional discarding of unused ones).
        /// </summary>
        public readonly HashSet<Texture> UsedTextures = new HashSet<Texture>();

        /// <summary>
        /// Indicates whether double-sided materials (i.e., materials requiring backside rendering) 
        /// were applied to the last processed mesh. This can help track special mesh rendering states.
        /// </summary>
        public bool AppliedDoubleSidedMaterials;

        /// <summary>
        /// If <c>true</c>, the model loads asynchronously whenever possible. If <c>false</c>, 
        /// TriLib forces a synchronous approach. This affects how tasks and threads are used.
        /// </summary>
        public bool Async = true;

        /// <summary>
        /// The directory or base path from which the model or resources are loaded. 
        /// Used for finding external textures or other data.
        /// </summary>
        public string BasePath;

        /// <summary>
        /// A token used to manage the timeout or cancellation of the loading process. 
        /// If triggered, loading tasks are halted before completion.
        /// </summary>
        public CancellationToken CancellationToken;

        /// <summary>
        /// A source object that can be invoked to cancel model loading manually (e.g., by user input). 
        /// Cancels tasks reliant on <see cref="CancellationToken"/>.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource;

        /// <summary>
        /// Arbitrary user-defined data or metadata attached to this context. 
        /// This object can store additional loading parameters, usage info, or placeholders needed by custom logic.
        /// </summary>
        public object CustomData;

        /// <summary>
        /// When loading from a stream rather than a local file, this stores the file extension 
        /// (e.g., "fbx", "gltf") so TriLib can determine an appropriate reader or parser.
        /// </summary>
        public string FileExtension;

        /// <summary>
        /// When loading from the local file system, this indicates the path or filename to load. 
        /// This may be combined with <see cref="BasePath"/> for resource lookups.
        /// </summary>
        public string Filename;

        /// <summary>
        /// If <c>true</c>, TriLib defers the actual start of loading tasks, letting the user 
        /// or calling code chain multiple tasks or manually schedule the execution.
        /// </summary>
        public bool HaltTasks;

        /// <summary>
        /// A delegate for internal error handling, invoked before <see cref="OnError"/>. 
        /// This allows additional checks or logging prior to the user-facing error callback.
        /// </summary>
        public Action<IContextualizedError> HandleError;

        /// <summary>
        /// If <c>true</c>, indicates that the current loading operation involves a .zip file,
        /// requiring AssetLoaderZip or related logic to decompress and parse contents.
        /// </summary>
        public bool IsZipFile;

        /// <summary>
        /// Tracks the overall progress of model loading (a float between 0 and 1).
        /// </summary>
        public float LoadingProgress;

        /// <summary>
        /// Represents the loading step (an integer) for more granular tracking of progress 
        /// (e.g., reading geometry, loading textures, building materials).
        /// </summary>
        public int LoadingStep;

        /// <summary>
        /// Potentially stores a file modification date or version retrieved by format-specific data. 
        /// This may be used for caching or informational displays.
        /// </summary>
        public string ModificationDate;

        /// <summary>
        /// A callback invoked on the main thread if an error occurs during loading. 
        /// Often used to notify the user or perform cleanup tasks.
        /// </summary>
        public Action<IContextualizedError> OnError;

        /// <summary>
        /// A callback invoked once the model’s core structure is loaded, 
        /// but possibly before all textures and materials finish processing. 
        /// This occurs on the main thread.
        /// </summary>
        public Action<AssetLoaderContext> OnLoad;

        /// <summary>
        /// A callback invoked once the model and all relevant resources (e.g., textures, 
        /// materials, animations) have finished loading. Called on the main thread.
        /// </summary>
        public Action<AssetLoaderContext> OnMaterialsLoad;

        /// <summary>
        /// A callback invoked on a background thread, providing a chance to manipulate or inspect 
        /// data before Unity <see cref="GameObject"/>s are instantiated (only when multi-threading is enabled).
        /// </summary>
        public Action<AssetLoaderContext> OnPreLoad;

        /// <summary>
        /// A callback that reports loading progress changes, providing both the current 
        /// <see cref="AssetLoaderContext"/> and a float progress value (0–1).
        /// </summary>
        public Action<AssetLoaderContext, float> OnProgress;

        /// <summary>
        /// The <see cref="AssetLoaderOptions"/> governing how the model is imported
        /// (e.g., whether to generate colliders, import animations, or apply custom material mappers).
        /// </summary>
        public AssetLoaderOptions Options;

        /// <summary>
        /// Tracks the previously reported loading step, aiding in detecting changes for 
        /// incremental updates or logging. Defaults to <c>-1</c> to indicate uninitialized.
        /// </summary>
        public int PreviousLoadingStep = -1;

        /// <summary>
        /// References the internal <see cref="ReaderBase"/> that’s parsing or converting the file. 
        /// Specific implementations (e.g., <c>FBXReader</c>, <c>GltfReader</c>) fill in data 
        /// for geometry, bones, and materials.
        /// </summary>
        public ReaderBase Reader;

        /// <summary>
        /// The <see cref="GameObject"/> representing the top-level node of the loaded model. 
        /// Contains or references all other child meshes, transforms, and data.
        /// </summary>
        public GameObject RootGameObject;

        /// <summary>
        /// An interface representing the root model structure (<see cref="IRootModel"/>) 
        /// of the loaded file. It can contain sub-models, materials, animations, etc.
        /// </summary>
        public IRootModel RootModel;

        /// <summary>
        /// The <see cref="Stream"/> used to load the model if loading from memory or 
        /// network data rather than a filesystem path.
        /// </summary>
        public Stream Stream;

        /// <summary>
        /// A reference to the main <see cref="Task"/> used for the loading operation (if asynchronous). 
        /// This allows for concurrency and non-blocking behavior during model import.
        /// </summary>
        public Task Task;

        /// <summary>
        /// A list of <see cref="Task"/> instances related to this loading operation, 
        /// used to manage parallel or sequential tasks (texture loading, post-processing, etc.).
        /// </summary>
        public List<Task> Tasks = new List<Task>();

        /// <summary>
        /// A user-specified or automatically created <see cref="GameObject"/> that serves 
        /// as the parent object for the entire loaded model hierarchy.
        /// </summary>
        public GameObject WrapperGameObject;

        private Stopwatch _stopwatch;

        /// <summary>
        /// If <c>true</c>, indicates that the model loading process has finished, 
        /// including asynchronous tasks. If <c>false</c>, the pipeline is still in progress.
        /// </summary>
        public bool Completed { get; set; }

        /// <inheritdoc />
        AssetLoaderContext IAssetLoaderContext.Context => this;

        /// <summary>
        /// A queue holding context-specific actions that can be dispatched 
        /// on the main thread or processed in other contexts. Useful for advanced 
        /// customization or specialized event scheduling.
        /// </summary>
        public Queue<IContextualizedAction> CustomDispatcherQueue { get; } = new Queue<IContextualizedAction>();

        /// <summary>
        /// The <c>Application.persistentDataPath</c> or another user-defined path 
        /// for storing or retrieving extracted or cached resources (e.g., embedded textures).
        /// </summary>
        public string PersistentDataPath { get; set; }

        /// <summary>
        /// Records a newly loaded texture’s context in <see cref="MaterialTextures"/> 
        /// to track the usage of that texture by a specific material/slot combination.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> holding the TriLib texture and additional metadata.
        /// </param>
        public void AddMaterialTexture(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.Texture == null)
            {
                return;
            }
            var compoundMaterialKey = new CompoundMaterialKey(textureLoadingContext.MaterialMapperContext.Material, textureLoadingContext.TextureType);
            if (MaterialTextures.ContainsKey(compoundMaterialKey))
            {
                return;
            }
            MaterialTextures.Add(compoundMaterialKey, textureLoadingContext);
        }

        /// <summary>
        /// Marks a <see cref="Texture"/> as used by at least one renderer or material, 
        /// so TriLib knows not to discard it when cleaning up unused allocations.
        /// </summary>
        /// <param name="texture">The texture being used by a model’s material.</param>
        public void AddUsedTexture(Texture texture)
        {
            UsedTextures.Add(texture);
        }

        /// <summary>
        /// Removes any <see cref="Texture"/> that isn’t referenced in <see cref="UsedTextures"/> from 
        /// the <see cref="Allocations"/> list and destroys it, freeing memory. Useful if your model 
        /// has optional or generated textures that are never applied.
        /// </summary>
        public void DiscardUnusedTextures()
        {
            for (var i = Allocations.Count - 1; i >= 0; i--)
            {
                var allocation = Allocations[i];
                if (allocation is Texture texture && !UsedTextures.Contains(texture))
                {
                    Allocations.Remove(texture);
                    if (Application.isPlaying)
                    {
                        Object.Destroy(texture);
                    }
                    else
                    {
                        Object.DestroyImmediate(texture);
                    }
                }
            }
        }

        /// <summary>
        /// Yields execution back to Unity if <see cref="AssetLoaderOptions.UseCoroutines"/> is enabled, 
        /// preventing long blocking operations on the main thread. Exits immediately if coroutines are disabled.
        /// </summary>
        /// <returns>
        /// An <see cref="IEnumerable"/> that yields once if the elapsed time since 
        /// the last yield exceeded <see cref="AssetLoaderOptions.MaxCoroutineDelayInMS"/>, 
        /// otherwise yields nothing.
        /// </returns>
        public IEnumerable ReleaseMainThread()
        {
            if (!Options.UseCoroutines)
            {
                yield break;
            }
            if (!_stopwatch.IsRunning)
            {
                _stopwatch.Start();
            }
            if (_stopwatch.ElapsedMilliseconds > Options.MaxCoroutineDelayInMS)
            {
                yield return null;
                _stopwatch.Restart();
            }
        }

        /// <summary>
        /// Performs initial setup for this loader context, such as preparing a 
        /// <see cref="Stopwatch"/> for timing coroutine yields if <see cref="AssetLoaderOptions.UseCoroutines"/> is true.
        /// </summary>
        public void Setup()
        {
            if (Options.UseCoroutines)
            {
                _stopwatch = new Stopwatch();
            }
        }

        /// <summary>
        /// Checks whether a texture with the specified <see cref="TextureLoadingContext"/> already exists
        /// in <see cref="LoadedCompoundTextures"/>. If found, reuses the existing data to avoid duplicating memory or I/O.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// Contains references to the TriLib texture, texture type, and the pipeline context.
        /// </param>
        /// <param name="existingTextureLoadingContext">
        /// An output parameter set to the found <see cref="TextureLoadingContext"/>, if any.
        /// </param>
        /// <returns><c>true</c> if the texture was found, otherwise <c>false</c>.</returns>
        public bool TryGetCompoundTexture(TextureLoadingContext textureLoadingContext, out TextureLoadingContext existingTextureLoadingContext)
        {
            var compoundTextureKey = new CompoundTextureKey(textureLoadingContext.Texture, textureLoadingContext.TextureType);
            return LoadedCompoundTextures.TryGetValue(compoundTextureKey, out existingTextureLoadingContext);
        }

        /// <summary>
        /// Checks whether the specified TriLib <see cref="ITexture"/> is already listed in <see cref="LoadedTextures"/>.
        /// If found, reuses its <see cref="TextureLoadingContext"/> so no redundant loading is performed.
        /// </summary>
        /// <param name="textureLoadingContext">Contains the TriLib texture reference.</param>
        /// <param name="existingTextureLoadingContext">
        /// Output parameter set to the found <see cref="TextureLoadingContext"/>, if any.
        /// </param>
        /// <returns><c>true</c> if the texture was previously loaded, otherwise <c>false</c>.</returns>
        public bool TryGetLoadedTexture(TextureLoadingContext textureLoadingContext, out TextureLoadingContext existingTextureLoadingContext)
        {
            return LoadedTextures.TryGetValue(textureLoadingContext.Texture, out existingTextureLoadingContext);
        }

        /// <summary>
        /// Searches for a processed texture (already loaded and stored) based on the given material and texture type. 
        /// If found, sets <paramref name="loadedTexture"/> and returns <c>true</c>.
        /// </summary>
        /// <param name="material">The source TriLib material.</param>
        /// <param name="textureType">A <see cref="TextureType"/> indicating diffuse, normal, etc.</param>
        /// <param name="loadedTexture">The Unity <see cref="Texture"/> that was found, or <c>null</c> if none exists.</param>
        /// <returns><c>true</c> if a texture was found, <c>false</c> if not.</returns>
        public bool TryGetMaterialTexture(IMaterial material, TextureType textureType, out Texture loadedTexture)
        {
            if (MaterialTextures.TryGetValue(new CompoundMaterialKey(material, textureType), out var textureGroup))
            {
                loadedTexture = textureGroup.UnityTexture;
                return true;
            }
            loadedTexture = null;
            return false;
        }

        /// <summary>
        /// Called to register a new <see cref="TextureLoadingContext"/> or reuse an existing one
        /// if the same texture reference is found. This avoids duplicate loads and ensures 
        /// consistent texture usage across multiple materials or submeshes.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> describing the texture, type, and pipeline context.
        /// </param>
        public void TryRegisterTexture(TextureLoadingContext textureLoadingContext)
        {
            // First check if the same (texture + type) was loaded as a compound texture
            if (TryGetCompoundTexture(textureLoadingContext, out var compoundTextureLoadingContext))
            {
                textureLoadingContext.TextureDataContext = compoundTextureLoadingContext.TextureDataContext;
                textureLoadingContext.Width = compoundTextureLoadingContext.Width;
                textureLoadingContext.Height = compoundTextureLoadingContext.Height;
                textureLoadingContext.UnityTexture = compoundTextureLoadingContext.UnityTexture;
                textureLoadingContext.OriginalUnityTexture = compoundTextureLoadingContext.OriginalUnityTexture;
                textureLoadingContext.TextureLoaded = true;
            }
            else
            {
                // If not found, check if the texture is already in LoadedTextures
                if (TryGetLoadedTexture(textureLoadingContext, out var loadedTextureLoadingContext))
                {
                    textureLoadingContext.TextureDataContext = loadedTextureLoadingContext.TextureDataContext;
                    textureLoadingContext.Width = loadedTextureLoadingContext.Width;
                    textureLoadingContext.Height = loadedTextureLoadingContext.Height;
                    textureLoadingContext.UnityTexture = loadedTextureLoadingContext.OriginalUnityTexture;
                    textureLoadingContext.OriginalUnityTexture = loadedTextureLoadingContext.OriginalUnityTexture;
                    textureLoadingContext.TextureLoaded = true;
                }
                else if (textureLoadingContext.UnityTexture != null)
                {
                    // If it wasn't found but a UnityTexture is already assigned, add it
                    AddLoadedTexture(textureLoadingContext);
                }
                AddCompoundTexture(textureLoadingContext);
            }
            AddMaterialTexture(textureLoadingContext);
        }

        /// <summary>
        /// Adds the specified texture to the <see cref="LoadedCompoundTextures"/> dictionary,
        /// associating it with a <see cref="CompoundTextureKey"/> if it’s not already present.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The context containing the TriLib texture reference and other load info.
        /// </param>
        private void AddCompoundTexture(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.Texture == null)
            {
                return;
            }
            var compoundTextureKey = new CompoundTextureKey(textureLoadingContext.Texture, textureLoadingContext.TextureType);
            if (!LoadedCompoundTextures.ContainsKey(compoundTextureKey))
            {
                LoadedCompoundTextures.Add(compoundTextureKey, textureLoadingContext);
            }
        }

        /// <summary>
        /// Adds the specified texture to the <see cref="LoadedTextures"/> dictionary, ensuring 
        /// that the same <see cref="ITexture"/> reference is not loaded multiple times.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The context containing metadata (width, height, data streams) for the newly loaded texture.
        /// </param>
        private void AddLoadedTexture(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.Texture == null)
            {
                return;
            }
            if (!LoadedTextures.ContainsKey(textureLoadingContext.Texture))
            {
                LoadedTextures.Add(textureLoadingContext.Texture, textureLoadingContext);
            }
        }
    }
}
