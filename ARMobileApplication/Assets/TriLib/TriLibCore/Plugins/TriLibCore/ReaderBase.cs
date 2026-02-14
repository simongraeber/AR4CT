using System;
using System.Collections.Generic;
using System.IO;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace TriLibCore
{
    /// <summary>
    /// Represents the base class for file-format Readers. 
    /// Defines the core functionality and workflow for loading a model into a <see cref="IRootModel"/>.
    /// </summary>
    public abstract class ReaderBase
    {
        /// <summary>
        /// An optional callback for profiling each loading step. 
        /// Parameters: (operationName, resourceName, duration, memoryUsed).
        /// </summary>
        public static Action<string, string, TimeSpan, long> ProfileStepCallback;

        /// <summary>
        /// Represents the post-loading steps enumeration, used to track the final stages of model processing.
        /// </summary>
        public enum PostLoadingSteps
        {
            /// <summary>
            /// Indicates post-processing animation clips.
            /// </summary>
            PostProcessAnimationClips,

            /// <summary>
            /// Indicates processing textures.
            /// </summary>
            ProcessTextures,

            /// <summary>
            /// Indicates post-processing renderers.
            /// </summary>
            PostProcessRenderers,

            /// <summary>
            /// Indicates that all processing is complete.
            /// </summary>
            FinishedProcessing
        }

        /// <summary>
        /// Gets the name of this reader.
        /// </summary>
        public abstract string Name { get; }

        private string[] _loadingStepEnumNames;

        /// <summary>
        /// Gets the names of the loading steps by reflecting over <see cref="LoadingStepEnumType"/>.
        /// </summary>
        private string[] LoadingStepEnumNames
        {
            get
            {
                if (_loadingStepEnumNames == null)
                {
                    _loadingStepEnumNames = Enum.GetNames(LoadingStepEnumType);
                }
                return _loadingStepEnumNames;
            }
        }

        /// <summary>
        /// Uses the object reader data to create the final model name. 
        /// If <see cref="AssetLoaderOptions.NameMapper"/> is set, it will be used to generate the model name.
        /// </summary>
        /// <param name="assetLoaderContext">The current <see cref="AssetLoaderContext"/>.</param>
        /// <param name="data">The naming data (model name, material name, ID, etc.) to consider.</param>
        /// <param name="model">The loaded model for which to generate a name.</param>
        /// <param name="readerName">The name of this reader.</param>
        /// <returns>The resulting name for the model.</returns>
        public string MapName(AssetLoaderContext assetLoaderContext, ModelNamingData data, IModel model, string readerName)
        {
            if (assetLoaderContext.Options.NameMapper != null)
            {
                var name = assetLoaderContext.Options.NameMapper.Map(assetLoaderContext, data, model, readerName);
                if (!string.IsNullOrWhiteSpace(name))
                {
                    return name;
                }
            }

            if (!string.IsNullOrWhiteSpace(data.ModelName))
            {
                return data.ModelName;
            }
            if (!string.IsNullOrWhiteSpace(data.MaterialName))
            {
                return data.MaterialName;
            }
            if (!string.IsNullOrWhiteSpace(data.Id))
            {
                return data.Id;
            }
            return "No-Name";
        }

        /// <summary>
        /// Gets the total number of loading steps for this reader.
        /// </summary>
        public virtual int LoadingStepsCount
        {
            get
            {
                return LoadingStepEnumNames.Length;
            }
        }

        /// <summary>
        /// Gets the <see cref="Type"/> that defines the loading steps (an enumeration of step definitions).
        /// </summary>
        protected abstract Type LoadingStepEnumType { get; }

        /// <summary>
        /// Provides access to the <see cref="AssetLoaderContext"/> used to load the model.
        /// </summary>
        public AssetLoaderContext AssetLoaderContext { get; private set; }

        private string _filename;
        private Action<AssetLoaderContext, float> _onProgress;
        private int _nameCounter;
        private int _materialCounter;
        private int _textureCounter;
        private int _geometryGroupCounter;
        private int _animationCounter;

        /// <summary>
        /// Reads a model from the specified <paramref name="stream"/> using the given <paramref name="assetLoaderContext"/>,
        /// optionally associating a <paramref name="filename"/> and progress callback <paramref name="onProgress"/>.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> containing the model data to load.</param>
        /// <param name="assetLoaderContext">The context that provides loading options and tracks loaded data.</param>
        /// <param name="filename">
        /// An optional filename for this model if loading from local storage; used for naming or reference.
        /// </param>
        /// <param name="onProgress">
        /// An optional callback invoked to report loading progress (<see cref="AssetLoaderContext.LoadingProgress"/>).
        /// </param>
        /// <returns>The loaded root model, or <c>null</c> if no model is loaded in this method.</returns>
        public virtual IRootModel ReadStream(
            Stream stream,
            AssetLoaderContext assetLoaderContext,
            string filename = null,
            Action<AssetLoaderContext, float> onProgress = null)
        {
            AssetLoaderContext = assetLoaderContext;
            AssetLoaderContext.Reader = this;

            if (AssetLoaderContext.Filename != null && File.Exists(AssetLoaderContext.Filename))
            {
                var lastWriteTime = File.GetLastWriteTime(AssetLoaderContext.Filename);
                AssetLoaderContext.ModificationDate = $"{lastWriteTime.Year}-{lastWriteTime.Month}-{lastWriteTime.Day}-{lastWriteTime.Hour}-{lastWriteTime.Minute}-{lastWriteTime.Second}-{lastWriteTime.Millisecond}";
            }

            _filename = filename;
            _onProgress = onProgress;

            // Initialize progress to 0 for the first step.
            UpdateLoadingPercentage(0f, 0, 1f);
            return null;
        }

        /// <summary>
        /// Reads an external file into a <see cref="Stream"/>, trying any defined external data mapper first.
        /// If the file is not provided by the mapper, attempts to locate it via <see cref="FileUtils.FindFile"/>.
        /// </summary>
        /// <param name="path">The relative or absolute path to the external file.</param>
        /// <returns>A <see cref="Stream"/> for the requested file, or <c>null</c> if the file could not be found.</returns>
        public virtual Stream ReadExternalFile(string path)
        {
            Stream stream = null;
            string finalPath = null;

            if (AssetLoaderContext.Options.ExternalDataMapper != null)
            {
                stream = AssetLoaderContext.Options.ExternalDataMapper.Map(AssetLoaderContext, path, out finalPath);
            }

            if (stream == null)
            {
                finalPath = FileUtils.FindFile(AssetLoaderContext.BasePath, path);
                if (finalPath != null)
                {
                    stream = new FileStream(finalPath, FileMode.Open, FileAccess.Read, FileShare.Read);
                }
            }

            if (finalPath != null)
            {
                if (AssetLoaderContext.LoadedExternalData.ContainsKey(path))
                {
                    if (AssetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        Debug.Log("External file already loaded, tried to load it twice: " + path);
                    }
                }
                else
                {
                    AssetLoaderContext.LoadedExternalData.Add(path, finalPath);
                }
            }

            if (stream == null && AssetLoaderContext.Options.ShowLoadingWarnings)
            {
                Debug.Log($"Could not find external file: {path}");
            }

            // Update loading progress (still zero for this step).
            UpdateLoadingPercentage(0f, 0, 1f);
            return stream;
        }

        /// <summary>
        /// Creates a <see cref="IRootModel"/> specific to the implementing reader.
        /// </summary>
        /// <returns>A new instance of the reader's <see cref="IRootModel"/> implementation.</returns>
        protected abstract IRootModel CreateRootModel();

        /// <summary>
        /// Applies final naming, pivot, and organizational transformations to the model after loading is complete.
        /// Also merges a single child if <see cref="AssetLoaderOptions.MergeSingleChild"/> is set.
        /// </summary>
        /// <param name="model">The top-level <see cref="IRootModel"/> loaded by this reader.</param>
        protected void PostProcessModel(ref IRootModel model)
        {
            if (_filename != null)
            {
                model.Name = FileUtils.GetFilenameWithoutExtension(_filename);
            }
            if (!AssetLoaderContext.Options.DisableObjectsRenaming)
            {
                var namesList = new Dictionary<string, ulong>();
                if (model.AllMaterials != null)
                {
                    if (model.AllMaterials.Count > AssetLoaderContext.Options.MaxObjectsToRename)
                    {
                        if (AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("TriLib will not rename materials for [" + model.Name + "]. Please increase your AssetLoaderOptions.MaxObjectsToRename value.");
                        }
                    }
                    else
                    {
                        namesList.Clear();
                        for (var index = 0; index < model.AllMaterials.Count; index++)
                        {
                            var material = model.AllMaterials[index];
                            FixName(material, "UnnamedMaterial");
                            CollectObjectNames<IMaterial>(material, namesList);
                        }

                        for (var index = 0; index < model.AllMaterials.Count; index++)
                        {
                            var material = model.AllMaterials[index];
                            EnsureUniqueNames(material, model.AllMaterials, namesList);
                        }
                    }
                }
                if (model.AllTextures != null)
                {
                    if (model.AllTextures.Count > AssetLoaderContext.Options.MaxObjectsToRename)
                    {
                        if (AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("TriLib will not rename textures for [" + model.Name + "]. Please increase your AssetLoaderOptions.MaxObjectsToRename value.");
                        }
                    }
                    else
                    {
                        namesList.Clear();
                        for (var index = 0; index < model.AllTextures.Count; index++)
                        {
                            var texture = model.AllTextures[index];
                            FixName(texture, "UnnamedTexture");
                            CollectObjectNames<ITexture>(texture, namesList);
                        }

                        for (var index = 0; index < model.AllTextures.Count; index++)
                        {
                            var texture = model.AllTextures[index];
                            EnsureUniqueNames(texture, model.AllTextures, namesList);
                        }
                    }
                }
                if (model.AllAnimations != null)
                {
                    if (model.AllAnimations.Count > AssetLoaderContext.Options.MaxObjectsToRename)
                    {
                        if (AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("TriLib will not rename animations for [" + model.Name + "]. Please increase your AssetLoaderOptions.MaxObjectsToRename value.");
                        }
                    }
                    else
                    {
                        namesList.Clear();
                        for (var index = 0; index < model.AllAnimations.Count; index++)
                        {
                            var animation = model.AllAnimations[index];
                            FixName(animation, "UnnamedAnimation");
                            CollectObjectNames<IAnimation>(animation, namesList);
                        }

                        for (var index = 0; index < model.AllAnimations.Count; index++)
                        {
                            var animation = model.AllAnimations[index];
                            EnsureUniqueNames(animation, model.AllAnimations, namesList);
                        }
                    }
                }
                if (model.AllModels != null)
                {
                    if (model.AllModels.Count > AssetLoaderContext.Options.MaxObjectsToRename)
                    {
                        if (AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("TriLib will not rename sub-models for the loaded model. Please increase your AssetLoaderOptions.MaxObjectsToRename value.");
                        }
                    }
                    else
                    {
                        FixName(model, "UnnamedModel");
                        var models = new List<IObject>();
                        ProcessHierarchy(model, models, namesList);
                    }
                }
                if (model.AllGeometryGroups != null)
                {
                    if (model.AllGeometryGroups.Count > AssetLoaderContext.Options.MaxObjectsToRename)
                    {
                        if (AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("TriLib will not rename geometries for [" + model.Name + "]. Please increase your AssetLoaderOptions.MaxObjectsToRename value.");
                        }
                    }
                    else
                    {
                        namesList.Clear();
                        for (var index = 0; index < model.AllGeometryGroups.Count; index++)
                        {
                            var geometryGroup = model.AllGeometryGroups[index];
                            FixName(geometryGroup, "UnnamedGeometryGroup");
                            CollectObjectNames<IGeometryGroup>(geometryGroup, namesList);
                        }

                        for (var index = 0; index < model.AllGeometryGroups.Count; index++)
                        {
                            var geometryGroup = model.AllGeometryGroups[index];
                            EnsureUniqueNames(geometryGroup, model.AllGeometryGroups, namesList);
                        }

                        for (var index = 0; index < model.AllGeometryGroups.Count; index++)
                        {
                            var geometryGroup = model.AllGeometryGroups[index];
                            if (geometryGroup.BlendShapeKeys == null || geometryGroup.BlendShapeKeys.Count == 0)
                            {
                                continue;
                            }

                            namesList.Clear();
                            for (var blendShapeIndex = 0; blendShapeIndex < geometryGroup.BlendShapeKeys.Count; blendShapeIndex++)
                            {
                                var blendShapeKey = geometryGroup.BlendShapeKeys[blendShapeIndex];
                                FixName(blendShapeKey, "UnnamedBlendShapeKey");
                                CollectObjectNames<IBlendShapeKey>(blendShapeKey, namesList);
                            }

                            for (var blendShapeIndex = 0; blendShapeIndex < geometryGroup.BlendShapeKeys.Count; blendShapeIndex++)
                            {
                                var blendShapeKey = geometryGroup.BlendShapeKeys[blendShapeIndex];
                                EnsureUniqueNames(blendShapeKey, geometryGroup.BlendShapeKeys, namesList);
                            }
                        }
                    }
                }
            }

            if (AssetLoaderContext.Options.SortHierarchyByName)
            {
                model.SortByName();
            }

            var modelGeometryGroups = model.AllGeometryGroups;
            if (modelGeometryGroups != null)
            {
                for (var i = 0; i < modelGeometryGroups.Count; i++)
                {
                    var geometryGroup = modelGeometryGroups[i];
                    if (string.IsNullOrEmpty(geometryGroup.Name))
                    {
                        geometryGroup.Name = (++_geometryGroupCounter).ToString();
                    }
                }
            }

            var modelAnimations = model.AllAnimations;
            if (modelAnimations != null)
            {
                for (var i = 0; i < modelAnimations.Count; i++)
                {
                    var animation = modelAnimations[i];
                    if (string.IsNullOrEmpty(animation.Name))
                    {
                        animation.Name = (++_animationCounter).ToString();
                    }
                }
            }

            var allMaterials = model.AllMaterials;
            if (allMaterials != null)
            {
                for (var i = 0; i < allMaterials.Count; i++)
                {
                    var material = allMaterials[i];
                    if (string.IsNullOrEmpty(material.Name))
                    {
                        material.Name = (++_materialCounter).ToString();
                    }
                }
            }

            var modelTextures = model.AllTextures;
            if (modelTextures != null)
            {
                for (var i = 0; i < modelTextures.Count; i++)
                {
                    var texture = modelTextures[i];
                    if (string.IsNullOrEmpty(texture.Name))
                    {
                        texture.Name = (++_textureCounter).ToString();
                    }
                }
            }

            _nameCounter = 0;

            if (AssetLoaderContext.Options.PivotPosition != PivotPosition.Default)
            {
                model.MovePivot(AssetLoaderContext);
            }

            PostProcessSubModel(model, model);

            model = HandleSingleChild(model);
        }

        private static void FixName(IObject @object, string defaultName)
        {
            if (string.IsNullOrWhiteSpace(@object.Name))
            {
                @object.Name = defaultName;
            }
        }

        private static void ProcessHierarchy(IModel model, IList<IObject> models, Dictionary<string, ulong> names)
        {
            models.Clear();
            names.Clear();
            if (model.Children != null)
            {
                foreach (var child in model.Children)
                {
                    FixName(child, "UnnamedModel");
                    CollectObjectNames<IModel>(child, names);
                    models.Add(child);
                }

                for (var i = models.Count - 1; i >= 0; i--)
                {
                    var child = models[i];
                    EnsureUniqueNames(child, models, names, true);
                }

                foreach (var child in model.Children)
                {
                    ProcessHierarchy(child, models, names);
                }
            }
        }

        private static void CollectObjectNames<T>(IObject sourceObject, Dictionary<string, ulong> names)
            where T : IObject
        {
            int lastIndexOfDigit;
            for (lastIndexOfDigit = sourceObject.Name.Length - 1; lastIndexOfDigit >= 0; lastIndexOfDigit--)
            {
                var @char = sourceObject.Name[lastIndexOfDigit];
                if (!char.IsDigit(@char))
                {
                    break;
                }
            }

            ulong counter;
            string onlyName;
            if (lastIndexOfDigit < sourceObject.Name.Length - 1)
            {
                if (!ulong.TryParse(sourceObject.Name.Substring(lastIndexOfDigit + 1), out counter))
                {
                    counter = ulong.MaxValue;
                }
                onlyName = sourceObject.Name.Substring(0, lastIndexOfDigit + 1);
            }
            else
            {
                counter = 0;
                onlyName = sourceObject.Name;
            }

            if (names.TryGetValue(onlyName, out var existingCounter))
            {
                var newName = Math.Max(counter, existingCounter);
                names[onlyName] = newName;
            }
            else
            {
                names.Add(onlyName, counter);
            }
        }

        private static void EnsureUniqueNames<T>(IObject sourceObject, IList<T> objects, Dictionary<string, ulong> names, bool addSpace = false)
            where T : IObject
        {
            int lastIndexOfDigit;
            for (lastIndexOfDigit = sourceObject.Name.Length - 1; lastIndexOfDigit >= 0; lastIndexOfDigit--)
            {
                var @char = sourceObject.Name[lastIndexOfDigit];
                if (!char.IsDigit(@char))
                {
                    break;
                }
            }

            var onlyName = lastIndexOfDigit < sourceObject.Name.Length - 1
                ? sourceObject.Name.Substring(0, lastIndexOfDigit + 1)
                : sourceObject.Name;

            foreach (var destinationObject in objects)
            {
                if (!ReferenceEquals(sourceObject, destinationObject) && destinationObject.Name == sourceObject.Name)
                {
                    var existingCounter = names[onlyName];
                    if (existingCounter >= ulong.MaxValue)
                    {
                        sourceObject.Name = $"{onlyName}{Guid.NewGuid()}";
                    }
                    else
                    {
                        var counter = names[onlyName] + 1;
                        names[onlyName] = counter;
                        sourceObject.Name = addSpace ? $"{onlyName} {counter}" : $"{onlyName}{counter}";
                    }
                    break;
                }
            }
        }

        private void PostProcessSubModel(IRootModel rootModel, IModel model)
        {
            if (string.IsNullOrEmpty(model.Name))
            {
                model.Name = (++_nameCounter).ToString();
            }

            if (model.HasCustomPivot)
            {
                var parentMatrix = model.Parent.GetGlobalMatrix();
                var matrix = parentMatrix * Matrix4x4.Translate(model.LocalPosition);
                var pivotOffset = matrix.inverse.MultiplyPoint(model.Pivot);
                model.LocalPosition += pivotOffset;
            }

            var modelGeometryGroup = model.GeometryGroup;
            if (modelGeometryGroup != null)
            {
                if (model.HasCustomPivot)
                {
                    var originalMatrix = model.OriginalGlobalMatrix;
                    var originalWorldPosition = originalMatrix.GetMatrixPosition();
                    var newMatrix = model.GetGlobalMatrix();
                    var newWorldPosition = newMatrix.GetMatrixPosition();
                    var offset = originalWorldPosition - newWorldPosition;
                    modelGeometryGroup.Pivot = newMatrix.inverse.MultiplyVector(offset);
                }

                var materialIndices = model.MaterialIndices;
                if (materialIndices == null && modelGeometryGroup.GeometriesData.Count > 0)
                {
                    materialIndices = new int[modelGeometryGroup.GeometriesData.Count];
                    materialIndices[0] = -1;
                    model.MaterialIndices = materialIndices;
                }
            }

            if (model.Children != null)
            {
                for (var i = 0; i < model.Children.Count; i++)
                {
                    var child = model.Children[i];
                    PostProcessSubModel(rootModel, child);
                }
            }
        }

        /// <summary>
        /// Updates the model loading progress percentage. 
        /// This method is typically called at various stages of the loading and post-processing pipeline.
        /// </summary>
        /// <param name="value">A floating-point value representing the current sub-step progress.</param>
        /// <param name="step">The zero-based index of the current step.</param>
        /// <param name="maxValue">
        /// The maximum expected progress for the current step; if greater than zero, <paramref name="value"/> is normalized by this value.
        /// </param>
        public void UpdateLoadingPercentage(float value, int step = 0, float maxValue = 0f)
        {
            AssetLoaderContext.CancellationToken.ThrowIfCancellationRequested();

            if (_onProgress != null)
            {
                if (maxValue > 0f)
                {
                    value /= maxValue;
                }

                var finalStepsCount = LoadingStepsCount + (int)PostLoadingSteps.FinishedProcessing + 1;
                var valuePercent = value / finalStepsCount;
                var percentPerStep = 1f / finalStepsCount;
                var stepPercent = step * percentPerStep;
                var finalValue = stepPercent + valuePercent;

                AssetLoaderContext.LoadingProgress = finalValue;
                Dispatcher.InvokeAsync(NotifyProgress, AssetLoaderContext, AssetLoaderContext.Async);
            }
        }

        private void NotifyProgress(AssetLoaderContext assetLoaderContext)
        {
            _onProgress(assetLoaderContext, assetLoaderContext.LoadingProgress);
        }

        private IRootModel HandleSingleChild(IRootModel rootModel)
        {
            if (AssetLoaderContext.Options.MergeSingleChild &&
                rootModel.Children != null &&
                rootModel.Children.Count == 1)
            {
                var childModel = rootModel.Children[0];
                var childRootModel = CreateRootModel();

                childRootModel.Name = rootModel.Name;
                childRootModel.AllTextures = rootModel.AllTextures;
                childRootModel.AllAnimations = rootModel.AllAnimations;

                if (rootModel.AllAnimations != null)
                {
                    for (var animationIndex = 0; animationIndex < rootModel.AllAnimations.Count; animationIndex++)
                    {
                        var animation = rootModel.AllAnimations[animationIndex];
                        if (animation.AnimationCurveBindings == null)
                        {
                            continue;
                        }

                        for (var animationCurveBindingIndex = 0; animationCurveBindingIndex < animation.AnimationCurveBindings.Count; animationCurveBindingIndex++)
                        {
                            var animationCurveBinding = animation.AnimationCurveBindings[animationCurveBindingIndex];
                            if (animationCurveBinding.Model == childModel)
                            {
                                animationCurveBinding.Model = childRootModel;
                            }
                        }
                    }
                }

                childRootModel.AllGeometryGroups = rootModel.AllGeometryGroups;
                childRootModel.AllMaterials = rootModel.AllMaterials;
                childRootModel.AllCameras = rootModel.AllCameras;
                childRootModel.AllLights = rootModel.AllLights;
                childRootModel.AllModels = new List<IModel>(rootModel.AllModels);
                childRootModel.AllModels.Remove(rootModel);
                childRootModel.UserProperties = rootModel.UserProperties;

                if (childModel.Children != null)
                {
                    for (var i = childModel.Children.Count - 1; i >= 0; i--)
                    {
                        var child = childModel.Children[i];
                        child.Parent = childRootModel;
                    }
                }

                childRootModel.Children = childModel.Children;
                childRootModel.BindPoses = childModel.BindPoses;
                childRootModel.Bones = childModel.Bones;
                childRootModel.GeometryGroup = childModel.GeometryGroup;
                childRootModel.IsBone = childModel.IsBone;
                childRootModel.LocalPosition = Vector3.zero;
                childRootModel.LocalRotation = childModel.LocalRotation;
                childRootModel.LocalScale = childModel.LocalScale;
                childRootModel.MaterialIndices = childModel.MaterialIndices;
                childRootModel.Used = childModel.Used;
                childRootModel.Visibility = childModel.Visibility;

                return childRootModel;
            }
            return rootModel;
        }

        /// <summary>
        /// Ensures that the given <paramref name="stream"/> is buffered in memory if needed, 
        /// based on the <see cref="AssetLoaderOptions.BufferizeFiles"/> setting.
        /// </summary>
        /// <param name="stream">The <see cref="Stream"/> to possibly replace with a buffered <see cref="MemoryStream"/>.</param>
        public void SetupStream(ref Stream stream)
        {
            // Only buffer if the stream is not already a MemoryStream and buffering is requested.
            if (!(stream is MemoryStream) &&
                (
                    AssetLoaderContext.Options.BufferizeFiles == FileBufferingMode.Always ||
                    (
                        AssetLoaderContext.Options.BufferizeFiles == FileBufferingMode.SmallFilesOnly &&
                        stream.Length <= 50000000
                    )
                ))
            {
                var data = stream.ReadBytes();
                stream = new MemoryStream(data, 0, data.Length, false, true);
            }
        }
    }
}
