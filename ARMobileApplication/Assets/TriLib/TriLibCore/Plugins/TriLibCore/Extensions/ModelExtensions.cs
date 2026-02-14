using System.Collections.Generic;
using System.Linq;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Provides extension methods to facilitate various operations on <see cref="IModel"/> objects,
    /// such as calculating transforms, retrieving bones, sorting children, and adjusting pivots.
    /// </summary>
    public static class ModelExtensions
    {
        /// <summary>
        /// Computes the local transformation matrix for this model using its
        /// <see cref="IModel.LocalPosition"/>, <see cref="IModel.LocalRotation"/>, and <see cref="IModel.LocalScale"/>.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to compute the local matrix for.</param>
        /// <returns>
        /// A <see cref="Matrix4x4"/> representing the local position, rotation, and scale of the model.
        /// </returns>
        public static Matrix4x4 GetLocalMatrix(this IModel model)
        {
            return Matrix4x4.TRS(model.LocalPosition, model.LocalRotation, model.LocalScale);
        }

        /// <summary>
        /// Computes the global transformation matrix for this model by concatenating its
        /// <see cref="GetLocalMatrix"/> with its parent's global matrix (and so on up the hierarchy).
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to compute the global matrix for.</param>
        /// <returns>
        /// A <see cref="Matrix4x4"/> representing the global position, rotation, and scale of the model
        /// relative to the world.
        /// </returns>
        public static Matrix4x4 GetGlobalMatrix(this IModel model)
        {
            var matrix = model.GetLocalMatrix();
            var parent = model.Parent;
            while (parent != null)
            {
                matrix = parent.GetLocalMatrix() * matrix;
                parent = parent.Parent;
            }
            return matrix;
        }

        /// <summary>
        /// Computes the local transformation matrix for this model using its
        /// <see cref="IModel.LocalPosition"/> and <see cref="IModel.LocalRotation"/>, but without scale.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to compute the local matrix for.</param>
        /// <returns>
        /// A <see cref="Matrix4x4"/> representing the local position and rotation of the model with a uniform scale of 1.
        /// </returns>
        public static Matrix4x4 GetLocalMatrixNoScale(this IModel model)
        {
            return Matrix4x4.TRS(model.LocalPosition, model.LocalRotation, Vector3.one);
        }

        /// <summary>
        /// Computes the global transformation matrix for this model by concatenating its
        /// <see cref="GetLocalMatrixNoScale"/> with its parent's global matrix (and so on up the hierarchy),
        /// ignoring scale.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to compute the global matrix for.</param>
        /// <returns>
        /// A <see cref="Matrix4x4"/> representing the global position and rotation of the model relative to the world,
        /// ignoring scaling.
        /// </returns>
        public static Matrix4x4 GetGlobalMatrixNoScale(this IModel model)
        {
            var matrix = model.GetLocalMatrixNoScale();
            var parent = model.Parent;
            while (parent != null)
            {
                matrix = parent.GetLocalMatrix() * matrix;
                parent = parent.Parent;
            }
            return matrix;
        }

        /// <summary>
        /// Computes the global transformation matrix for the parent of this model.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> whose parent hierarchy matrix is computed.</param>
        /// <returns>A <see cref="Matrix4x4"/> representing the parent's global matrix.</returns>
        public static Matrix4x4 GetGlobalParentMatrix(this IModel model)
        {
            var matrix = Matrix4x4.identity;
            var parent = model.Parent;
            while (parent != null)
            {
                matrix = parent.GetLocalMatrix() * matrix;
                parent = parent.Parent;
            }
            return matrix;
        }

        /// <summary>
        /// Finds all <see cref="GameObject"/> instances with only Transform components under this model
        /// and appends them to the specified <paramref name="bones"/> list.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to search for bone-like objects.</param>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing references to the model hierarchy and loaded <see cref="GameObject"/>s.
        /// </param>
        /// <param name="bones">The list to which discovered bone transforms will be added.</param>
        public static void GetBones(this IModel model, AssetLoaderContext assetLoaderContext, List<Transform> bones)
        {
            if (assetLoaderContext.RootModel?.AllModels == null)
            {
                return;
            }
            var tempBones = new List<Transform>(assetLoaderContext.RootModel.AllModels.Count);

            // First, try to add this model if it's considered a bone.
            TryToAddBone(assetLoaderContext, tempBones, model);

            // If none were added as bones, try adding empty models (i.e., transform-only objects).
            if (tempBones.Count > 0)
            {
                bones.AddRange(tempBones);
                return;
            }
            TryToAddEmptyModel(assetLoaderContext, tempBones, model);
        }

        /// <summary>
        /// Recursively sorts the children of this model by their name in ascending order.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> whose children will be sorted.</param>
        public static void SortByName(this IModel model)
        {
            if (model.Children != null)
            {
                model.Children = model.Children.OrderBy(x => x.Name).ToList();
                for (var i = 0; i < model.Children.Count; i++)
                {
                    var child = model.Children[i];
                    child.SortByName();
                }
            }
        }

        /// <summary>
        /// Recursively counts the total number of child models under this model.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> whose child count is needed.</param>
        /// <returns>An integer representing the total count of child models.</returns>
        public static int CountChild(this IModel model)
        {
            var children = model.Children;
            int childCount;
            if (children != null)
            {
                childCount = children.Count;
                for (var i = 0; i < children.Count; i++)
                {
                    var child = children[i];
                    childCount += CountChild(child);
                }
            }
            else
            {
                childCount = 0;
            }
            return childCount;
        }

        /// <summary>
        /// Calculates the local or global bounds of the given <see cref="IModel"/>,
        /// taking into account its geometry and the geometry of its children.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> to calculate bounds for.</param>
        /// <returns>A <see cref="Bounds"/> object representing the model's bounds in world space.</returns>
        /// <remarks>
        /// The method traverses the entire hierarchy of the model, using <see cref="GetGlobalMatrix(IModel)"/>
        /// to determine the final position of each vertex in world space.
        /// </remarks>
        public static Bounds CalculateBounds(this IModel model)
        {
            var bounds = new Bounds();
            var firstVertex = true;
            CalculateBounds(model, ref bounds, ref firstVertex);
            return bounds;
        }

        /// <summary>
        /// Moves the pivot of the given <see cref="IModel"/> according to the <see cref="PivotPosition"/>
        /// set in the provided <see cref="AssetLoaderContext"/>. If the model has skinning data, the pivot is not moved.
        /// </summary>
        /// <param name="model">The <see cref="IModel"/> whose pivot will be moved.</param>
        /// <param name="assetLoaderContext">
        /// The context object containing options for moving the pivot and references to other models.
        /// </param>
        /// <remarks>
        /// This operation is skipped if the model contains bind poses (i.e., it's skinned), to avoid
        /// invalidating the skinning data.
        /// </remarks>
        public static void MovePivot(this IModel model, AssetLoaderContext assetLoaderContext)
        {
            var hasBindPoses = false;

            // Local function to detect if this model (or its children) contain any bind poses.
            void CheckBindPoses(IModel toCheck)
            {
                if (toCheck.BindPoses != null && toCheck.BindPoses.Length > 0)
                {
                    hasBindPoses = true;
                    return;
                }
                if (toCheck.CountChild() > 0)
                {
                    foreach (var child in toCheck.Children)
                    {
                        CheckBindPoses(child);
                    }
                }
            }

            CheckBindPoses(model);

            // If there are bind poses, do not move the pivot.
            if (hasBindPoses)
            {
                if (assetLoaderContext.Options.ShowLoadingWarnings)
                {
                    Debug.LogWarning("TriLib can't move the pivot of skinned meshes.");
                }
                return;
            }

            // Calculate the original pivot position based on user settings.
            Vector3 originalPivot;
            var bounds = CalculateBounds(model);
            switch (assetLoaderContext.Options.PivotPosition)
            {
                case PivotPosition.Center:
                    originalPivot = bounds.center;
                    break;
                case PivotPosition.Bottom:
                    originalPivot = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
                    break;
                default: // PivotPosition.Top
                    originalPivot = new Vector3(bounds.center.x, bounds.max.y, bounds.center.z);
                    break;
            }

            // Recursively set the pivot on this model and its children (unless it's the root).
            void SetPivot(IModel toSet)
            {
                if (toSet.Parent != null)
                {
                    toSet.Pivot = originalPivot;
                    toSet.HasCustomPivot = true;
                }
                if (toSet.CountChild() > 0)
                {
                    foreach (var child in toSet.Children)
                    {
                        SetPivot(child);
                    }
                }
            }

            SetPivot(model);
        }

        #region Private Utility Methods

        /// <summary>
        /// Recursively traverses the model hierarchy, encapsulating vertex positions
        /// into the given <paramref name="bounds"/>, in world coordinates.
        /// </summary>
        /// <param name="model">The model whose vertices will be encapsulated.</param>
        /// <param name="bounds">A reference to the <see cref="Bounds"/> instance being updated.</param>
        /// <param name="firstVertex">
        /// A boolean flag indicating if this is the first vertex. This helps initialize the bounds properly.
        /// </param>
        private static void CalculateBounds(this IModel model, ref Bounds bounds, ref bool firstVertex)
        {
            var localToWorldMatrix = GetGlobalMatrix(model);
            if (model.GeometryGroup != null)
            {
                for (var i = 0; i < model.GeometryGroup.VerticesDataCount; i++)
                {
                    var vertex = model.GeometryGroup.Positions[i];
                    var globalVertex = localToWorldMatrix.MultiplyPoint(vertex);
                    if (firstVertex)
                    {
                        bounds.size = Vector3.zero;
                        bounds.center = globalVertex;
                        firstVertex = false;
                    }
                    else
                    {
                        bounds.Encapsulate(globalVertex);
                    }
                }
            }

            if (model.Children != null)
            {
                for (var i = 0; i < model.Children.Count; i++)
                {
                    var child = model.Children[i];
                    CalculateBounds(child, ref bounds, ref firstVertex);
                }
            }
        }

        /// <summary>
        /// Recursively attempts to add transform-only objects (i.e., empty models with no geometry)
        /// as bones to the specified list.
        /// </summary>
        /// <param name="assetLoaderContext">The context for loading assets and models.</param>
        /// <param name="bones">The list of bones to add to.</param>
        /// <param name="emptyModel">The model to check.</param>
        private static void TryToAddEmptyModel(AssetLoaderContext assetLoaderContext, List<Transform> bones, IModel emptyModel)
        {
            if (emptyModel.Parent != null && emptyModel.GeometryGroup == null)
            {
                bones.Add(assetLoaderContext.GameObjects[emptyModel].transform);
            }
            if (emptyModel.Children != null)
            {
                for (var i = 0; i < emptyModel.Children.Count; i++)
                {
                    var child = emptyModel.Children[i];
                    TryToAddEmptyModel(assetLoaderContext, bones, child);
                }
            }
        }

        /// <summary>
        /// Recursively attempts to add models flagged as bones (<see cref="IModel.IsBone"/>) to the specified list.
        /// </summary>
        /// <param name="assetLoaderContext">The context for loading assets and models.</param>
        /// <param name="tempBones">A temporary list of bones.</param>
        /// <param name="bone">The model to check.</param>
        private static void TryToAddBone(AssetLoaderContext assetLoaderContext, List<Transform> tempBones, IModel bone)
        {
            if (bone.IsBone)
            {
                tempBones.Add(assetLoaderContext.GameObjects[bone].transform);
            }
            if (bone.Children != null)
            {
                for (var i = 0; i < bone.Children.Count; i++)
                {
                    var child = bone.Children[i];
                    TryToAddBone(assetLoaderContext, tempBones, child);
                }
            }
        }

        #endregion
    }
}
