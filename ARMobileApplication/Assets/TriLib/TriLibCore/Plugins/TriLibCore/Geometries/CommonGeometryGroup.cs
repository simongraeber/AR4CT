using System.Collections;
using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using BoneWeight1 = UnityEngine.BoneWeight1;
using Debug = UnityEngine.Debug;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Represents a TriLib geometry group.
    /// A geometry group contains one or more child geometries (typically sub-meshes)
    /// and aggregates vertex data used to build a Unity <see cref="Mesh"/>.
    /// </summary>
    public class CommonGeometryGroup : IGeometryGroup
    {
        /// <summary>
        /// Default per-vertex bone weight list capacity used while collecting raw bone weights.
        /// </summary>
        private const int BoneWeightVerticesCapacity = 4;

        private Dictionary<PointerVertexData, int> _mergedVertices;
        private int _mergedVertexCount;
        private bool _initialized;

        /// <summary>
        /// Temporary color used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Color TempColor;

        /// <summary>
        /// Temporary normal used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector3 TempNormal;

        /// <summary>
        /// Temporary position used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector3 TempPosition;

        /// <summary>
        /// Temporary tangent used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector4 TempTangent;

        /// <summary>
        /// Temporary UV0 used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector2 TempUV1;

        /// <summary>
        /// Temporary UV1 used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector2 TempUV2;

        /// <summary>
        /// Temporary UV2 used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector2 TempUV3;

        /// <summary>
        /// Temporary UV3 used to stage vertex data before it is committed to the internal lists.
        /// </summary>
        public Vector2 TempUV4;

        /// <summary>
        /// Temporary original vertex index used to stage vertex data before it is committed.
        /// </summary>
        public int TempOriginalVertexIndex;

        /// <summary>
        /// Gets or sets the blend shape keys associated with this geometry group.
        /// </summary>
        public List<IBlendShapeKey> BlendShapeKeys { get; set; }

        /// <summary>
        /// Gets the geometries dictionary indexed by a material/quad derived key.
        /// Each entry typically maps to a Unity sub-mesh.
        /// </summary>
        public Dictionary<int, IGeometry> GeometriesData { get; private set; }

        /// <summary>
        /// Gets or sets the expected geometry capacity (sub-mesh count hint).
        /// </summary>
        public int GeometryCapacity { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has skinning data.
        /// </summary>
        public bool HasSkin { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has vertex colors.
        /// </summary>
        public bool HasColors { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether invalid normals were detected while adding vertices.
        /// </summary>
        public bool HasInvalidNormals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has normals.
        /// </summary>
        public bool HasNormals { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has tangents.
        /// </summary>
        public bool HasTangents { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has UV0.
        /// </summary>
        public bool HasUv1 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has UV1.
        /// </summary>
        public bool HasUv2 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has UV2.
        /// </summary>
        public bool HasUv3 { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group has UV3.
        /// </summary>
        public bool HasUv4 { get; set; }

        /// <summary>
        /// Gets or sets the Unity mesh created by this geometry group.
        /// </summary>
        public Mesh Mesh { get; set; }

        /// <summary>
        /// Gets or sets the geometry group name (assigned to the resulting mesh name).
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the vertex positions list.
        /// </summary>
        public List<Vector3> Positions { get; set; }

        /// <summary>
        /// Gets or sets the vertex normals list.
        /// </summary>
        public List<Vector3> Normals { get; set; }

        /// <summary>
        /// Gets or sets the vertex tangents list.
        /// </summary>
        public List<Vector4> Tangents { get; set; }

        /// <summary>
        /// Gets or sets the vertex colors list.
        /// </summary>
        public List<Color> Colors { get; set; }

        /// <summary>
        /// Gets or sets the UV0 list.
        /// </summary>
        public List<Vector2> UVs1 { get; set; }

        /// <summary>
        /// Gets or sets the UV1 list.
        /// </summary>
        public List<Vector2> UVs2 { get; set; }

        /// <summary>
        /// Gets or sets the UV2 list.
        /// </summary>
        public List<Vector2> UVs3 { get; set; }

        /// <summary>
        /// Gets or sets the UV3 list.
        /// </summary>
        public List<Vector2> UVs4 { get; set; }

        /// <summary>
        /// Gets or sets the original vertex indices, mapping each stored vertex entry to its source vertex index.
        /// </summary>
        public List<int> OriginalVertexIndices { get; set; }

        /// <summary>
        /// Gets or sets a local pivot offset applied to vertex positions when possible.
        /// </summary>
        public Vector3 Pivot { get; set; }

        /// <summary>
        /// Gets or sets raw per-vertex bone weights indexed by original vertex index.
        /// </summary>
        public Dictionary<int, List<BoneWeight1>> RawBoneWeights { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry group was used/consumed by the importer pipeline.
        /// </summary>
        public bool Used { get; set; }

        /// <summary>
        /// Gets or sets the expected vertex capacity (allocation hint).
        /// </summary>
        public int VerticesCapacity { get; set; }

        /// <summary>
        /// Gets the number of vertices currently stored in this geometry group.
        /// </summary>
        public int VerticesDataCount => Positions.Count;

        /// <summary>
        /// Creates a new <see cref="CommonGeometryGroup"/> with the given vertex attribute flags.
        /// </summary>
        /// <param name="hasNormal">Whether the group contains normals.</param>
        /// <param name="hasTangent">Whether the group contains tangents.</param>
        /// <param name="hasColor">Whether the group contains vertex colors.</param>
        /// <param name="hasUV0">Whether the group contains UV0.</param>
        /// <param name="hasUV1">Whether the group contains UV1.</param>
        /// <param name="hasUV2">Whether the group contains UV2.</param>
        /// <param name="hasUV3">Whether the group contains UV3.</param>
        /// <param name="hasSkin">Whether the group contains skinning data.</param>
        /// <returns>A configured <see cref="CommonGeometryGroup"/> instance.</returns>
        public static CommonGeometryGroup Create(bool hasNormal, bool hasTangent, bool hasColor, bool hasUV0, bool hasUV1, bool hasUV2, bool hasUV3, bool hasSkin)
        {
            var geometryGroup = new CommonGeometryGroup();
            geometryGroup.HasNormals = hasNormal;
            geometryGroup.HasTangents = hasTangent;
            geometryGroup.HasColors = hasColor;
            geometryGroup.HasUv1 = hasUV0;
            geometryGroup.HasUv2 = hasUV1;
            geometryGroup.HasUv3 = hasUV2;
            geometryGroup.HasUv4 = hasUV3;
            geometryGroup.HasSkin = hasSkin;
            return geometryGroup;
        }

        /// <summary>
        /// Adds a raw <see cref="BoneWeight1"/> entry for the given original vertex index.
        /// </summary>
        /// <param name="vertexIndex">The original vertex index the weight belongs to.</param>
        /// <param name="boneWeight1">The bone weight entry to add.</param>
        public void AddBoneWeight(int vertexIndex, BoneWeight1 boneWeight1)
        {
            if (RawBoneWeights == null)
            {
                RawBoneWeights = new Dictionary<int, List<BoneWeight1>>(VerticesCapacity);
            }
            if (!RawBoneWeights.TryGetValue(vertexIndex, out var vertexBoneWeights))
            {
                vertexBoneWeights = new List<BoneWeight1>(BoneWeightVerticesCapacity);
                RawBoneWeights.Add(vertexIndex, vertexBoneWeights);
            }
            vertexBoneWeights.Add(boneWeight1);
        }

        /// <summary>
        /// Adds (or merges) a vertex into this geometry group and returns the resulting vertex data index.
        /// </summary>
        /// <remarks>
        /// When <c>AssetLoaderOptions.MergeVertices</c> is enabled, this method attempts to reuse an existing
        /// vertex entry by hashing/comparing the staged vertex data; otherwise, a new vertex entry is always created.
        /// </remarks>
        /// <param name="assetLoaderContext">The asset loader context and options.</param>
        /// <param name="vertexIndex">The original vertex index from the source mesh.</param>
        /// <param name="position">Vertex position.</param>
        /// <param name="normal">Vertex normal.</param>
        /// <param name="tangent">Vertex tangent.</param>
        /// <param name="color">Vertex color.</param>
        /// <param name="uv1">UV0.</param>
        /// <param name="uv2">UV1.</param>
        /// <param name="uv3">UV2.</param>
        /// <param name="uv4">UV3.</param>
        /// <param name="boneWeight">Bone weight (reserved/legacy; bone weights are collected through <see cref="AddBoneWeight"/>).</param>
        /// <returns>The vertex data index used by geometries (triangle indices reference this index).</returns>
        public int AddVertex(
            AssetLoaderContext assetLoaderContext,
            int vertexIndex,
            Vector3 position,
            Vector3 normal,
            Vector4 tangent,
            Color color,
            Vector2 uv1,
            Vector2 uv2,
            Vector2 uv3,
            Vector2 uv4,
            BoneWeight boneWeight
        )
        {
            var vertexDataIndex = -1;
            if (assetLoaderContext.Options.SwapUVs)
            {
                (uv1, uv2) = (uv2, uv1);
            }
            TempOriginalVertexIndex = vertexIndex;
            TempPosition = position;
            TempNormal = normal;
            TempTangent = tangent;
            TempColor = color;
            TempUV1 = uv1;
            TempUV2 = uv2;
            TempUV3 = uv3;
            TempUV4 = uv4;
            if (!assetLoaderContext.Options.MergeVertices)
            {
                vertexDataIndex = VerticesDataCount;
                AddVertexData();
            }
            else
            {
                var pointerVertexData = new PointerVertexData(this, -1);
                if (_mergedVertices.TryGetValue(pointerVertexData, out var existingIndex))
                {
                    _mergedVertexCount++;
                    vertexDataIndex = existingIndex;
                }
                else
                {
                    vertexDataIndex = VerticesDataCount;
                    AddVertexData();
                    pointerVertexData.VertexDataIndex = vertexDataIndex;
                    _mergedVertices.Add(pointerVertexData, vertexDataIndex);
                }
            }

            return vertexDataIndex;
        }

        /// <summary>
        /// Generates and populates the Unity <see cref="Mesh"/> instance for this geometry group.
        /// </summary>
        /// <remarks>
        /// This method is implemented as an iterator to allow yielding back to the main thread between heavy steps
        /// (via <see cref="AssetLoaderContext.ReleaseMainThread"/>), preventing long frame stalls during import.
        /// </remarks>
        /// <param name="assetLoaderContext">The asset loader context and options.</param>
        /// <param name="meshGameObject">The target mesh game object.</param>
        /// <param name="meshModel">The model holding bind poses, transforms, and hierarchy information.</param>
        /// <returns>An enumerator used by the importer pipeline to schedule work across frames.</returns>
        public IEnumerable GenerateMesh(AssetLoaderContext assetLoaderContext, GameObject meshGameObject, IModel meshModel)
        {
            //todo: reimplement
            //assetLoaderContext.AppliedDoubleSidedMaterials = false;
            if (Mesh != null)
            {
                yield break;
            }
            Mesh = new Mesh
            {
                name = Name
            };
            if (VerticesDataCount > 0)
            {
                if (!assetLoaderContext.Options.LoadPointClouds)
                {
                    if ((!HasNormals || HasInvalidNormals) && assetLoaderContext.Options.GenerateNormals && !assetLoaderContext.Options.UseUnityNativeNormalCalculator)
                    {
                        if (assetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.Log($"Mesh [{Name}] has no normals. TriLib will calculate them.");
                        }
                        Normals = NormalSolver.CalculateNormalsAsList(Positions, GeometriesData, assetLoaderContext);
                        HasNormals = true;
                        foreach (var item in assetLoaderContext.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                    //todo: reimplement
                    //GeometryGroupUtils.TryCreateDuplicatedFaces(assetLoaderContext, this, materialIndices);
                }
                if (Pivot != Vector3.zero)
                {
                    if (meshModel.BindPoses != null)
                    {
                        if (assetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("Could not set skinned mesh [" + Name + "] pivot.");
                        }
                    }
                    else
                    {
                        for (var i = 0; i < Positions.Count; i++)
                        {
                            Positions[i] += Pivot;
                        }
                        foreach (var item in assetLoaderContext.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                }
                if (!assetLoaderContext.Options.MeshWorldTransform.isIdentity)
                {
                    if (meshModel.BindPoses != null)
                    {
                        if (assetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning("Could not set skinned mesh [" + Name + "] offset.");
                        }
                    }
                    else
                    {
                        var localOffset = meshModel.GetGlobalParentMatrix().inverse * assetLoaderContext.Options.MeshWorldTransform;
                        for (var i = 0; i < Positions.Count; i++)
                        {
                            Positions[i] = localOffset.MultiplyPoint(Positions[i]);
                        }
                        foreach (var item in assetLoaderContext.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }
                }
                Mesh.SetVertices(Positions);
                if (HasNormals)
                {
                    Mesh.SetNormals(Normals);
                }
                if (HasTangents)
                {
                    Mesh.SetTangents(Tangents);
                }
                if (HasColors)
                {
                    Mesh.SetColors(Colors);
                }
                if (HasUv1)
                {
                    Mesh.SetUVs(0, UVs1);
                }
                if (HasUv2)
                {
                    Mesh.SetUVs(1, UVs2);
                }
                if (HasUv3)
                {
                    Mesh.SetUVs(2, UVs3);
                }
                if (HasUv4)
                {
                    Mesh.SetUVs(3, UVs4);
                }
                foreach (var item in assetLoaderContext.ReleaseMainThread())
                {
                    yield return item;
                }
                if (!assetLoaderContext.Options.LoadPointClouds)
                {
                    if (RawBoneWeights != null && RawBoneWeights.Count > 0)
                    {
                        if (assetLoaderContext.Options.LimitBoneWeights)
                        {
                            var boneWeights = new BoneWeight[VerticesDataCount];
                            for (var i = 0; i < boneWeights.Length; i++)
                            {
                                var vertexIndex = OriginalVertexIndices[i];
                                if (!RawBoneWeights.TryGetValue(vertexIndex, out var rawBoneWeights))
                                {
                                    continue;
                                }
                                boneWeights[i] = GeometryGroupUtils.LimitBoneWeights(rawBoneWeights);
                            }
                            RawBoneWeights = null;
                            Mesh.boneWeights = boneWeights;
                        }
                        else
                        {
                            var bonesPerVertex = new NativeArray<byte>(VerticesDataCount, Allocator.Temp);
                            var allWeightsList = new List<BoneWeight1>(BoneWeightVerticesCapacity);
                            for (var i = 0; i < VerticesDataCount; i++)
                            {
                                var vertexIndex = OriginalVertexIndices[i];
                                var rawBones = RawBoneWeights[vertexIndex];
                                var count = GeometryGroupUtils.LimitMaxBoneWeights(allWeightsList, rawBones);
                                bonesPerVertex[i] = count;
                            }
                            var allWeights = new NativeArray<BoneWeight1>(allWeightsList.Count, Allocator.Temp);
                            for (var i = 0; i < allWeightsList.Count; i++)
                            {
                                allWeights[i] = allWeightsList[i];
                            }
                            Mesh.SetBoneWeights(bonesPerVertex, allWeights);
                        }
                        foreach (var item in assetLoaderContext.ReleaseMainThread())
                        {
                            yield return item;
                        }
                    }

                    Mesh.indexFormat = VerticesDataCount >= ushort.MaxValue || assetLoaderContext.Options.IndexFormat == IndexFormat.UInt32 ? IndexFormat.UInt32 : IndexFormat.UInt16;
                    Mesh.subMeshCount = GeometriesData.Count;
                    var subMeshIndex = 0;
                    foreach (var geometry in GeometriesData.Values)
                    {
                        if (geometry != null)
                        {
                            Mesh.SetTriangles(geometry.VertexDataIndices, subMeshIndex++, true);
                            foreach (var item in assetLoaderContext.ReleaseMainThread())
                            {
                                yield return item;
                            }
                        }
                    }
                    if ((!HasNormals || HasInvalidNormals) && assetLoaderContext.Options.GenerateNormals && assetLoaderContext.Options.UseUnityNativeNormalCalculator)
                    {
                        if (assetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.Log($"Mesh [{Name}] has no normals. TriLib will calculate them using Unity builtin normal calculator.");
                        }
                        Mesh.RecalculateNormals();
                        HasNormals = true;
                    }
                    if (!HasTangents || assetLoaderContext.Options.GenerateTangents && !assetLoaderContext.Options.ImportTangents)
                    {
                        if (assetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.Log($"Mesh [{Name}] has no tangents. TriLib will calculate them using Unity builtin tangent calculator.");
                        }
                        Mesh.RecalculateTangents();
                        HasTangents = true;
                    }
                    if (BlendShapeKeys != null)
                    {
                        if (assetLoaderContext.Options.BlendShapeMapper != null)
                        {
                            assetLoaderContext.Options.BlendShapeMapper.Setup(assetLoaderContext, this, meshGameObject, BlendShapeKeys);
                        }
                        else
                        {
                            foreach (var item in GeometryGroupUtils.ProcessBlendShapeKeys(assetLoaderContext, this, BlendShapeKeys))
                            {
                                yield return item;
                            }
                        }
                    }
                    Mesh.bindposes = meshModel.BindPoses;
                }
                Mesh.RecalculateBounds();
                if (!assetLoaderContext.Options.LoadPointClouds)
                {
                    if (assetLoaderContext.Options.OptimizeMeshes && assetLoaderContext.Options.BlendShapeMapper == null)
                    {
                        Mesh.Optimize();
                    }
                    if (!assetLoaderContext.Options.ReadEnabled && assetLoaderContext.Options.BlendShapeMapper == null &&/*&& !assetLoaderContext.AppliedDoubleSidedMaterials */ assetLoaderContext.Options.AlphaMaterialMode != AlphaMaterialMode.CutoutAndTransparent)
                    {
                        Mesh.UploadMeshData(true);
                    }
                }
                if (assetLoaderContext.Options.MarkMeshesAsDynamic || assetLoaderContext.Options.BlendShapeMapper != null)
                {
                    Mesh.MarkDynamic();
                }
                if (assetLoaderContext.Options.MergeVertices && assetLoaderContext.Options.ShowLoadingWarnings)
                {
                    Debug.Log($"Merged {_mergedVertexCount} vertices from mesh [{Name}]");
                }
            }
        }

        /// <summary>
        /// Computes the center point of the geometry group based on the vertex positions.
        /// </summary>
        /// <returns>The computed bounds center.</returns>
        public Vector3 GetCenter()
        {
            var bounds = default(Bounds);
            for (var i = 0; i < Positions.Count; i++)
            {
                if (i == 0)
                {
                    bounds = new Bounds(Positions[0], Vector3.zero);
                }
                else
                {
                    bounds.Encapsulate(Positions[0]);
                }
            }
            return bounds.center;
        }

        /// <summary>
        /// Gets an existing geometry entry for the given material index and topology,
        /// or creates and registers a new one if needed.
        /// </summary>
        /// <typeparam name="TGeometry">The geometry implementation type.</typeparam>
        /// <param name="assetLoaderContext">The asset loader context and options.</param>
        /// <param name="materialIndex">The material index associated with the geometry.</param>
        /// <param name="isQuad">Whether the geometry represents quad topology.</param>
        /// <param name="hasBlendShapes">Whether blend shapes are present for this geometry.</param>
        /// <returns>The geometry instance registered in <see cref="GeometriesData"/>.</returns>
        public virtual TGeometry GetGeometry<TGeometry>(AssetLoaderContext assetLoaderContext, int materialIndex, bool isQuad, bool hasBlendShapes)
            where TGeometry : class, IGeometry, new()
        {
            isQuad &= assetLoaderContext.Options.KeepQuads;
            var finalIndex = isQuad ? int.MaxValue / 2 + materialIndex : materialIndex;
            if (!GeometriesData.TryGetValue(finalIndex, out var geometry))
            {
                geometry = new TGeometry();
                geometry.Setup(this, materialIndex, isQuad, hasBlendShapes, assetLoaderContext.Options.LoadPointClouds);
                GeometriesData.Add(finalIndex, geometry);
            }
            return (TGeometry)geometry;
        }

        /// <summary>
        /// Initializes internal lists and caches using the provided capacity hints and loader options.
        /// </summary>
        /// <param name="assetLoaderContext">The asset loader context and options.</param>
        /// <param name="verticesCapacity">Initial vertex list capacity.</param>
        /// <param name="geometriesCapacity">Initial geometry/sub-mesh dictionary capacity.</param>
        public virtual void Setup(AssetLoaderContext assetLoaderContext, int verticesCapacity, int geometriesCapacity)
        {
            if (!_initialized)
            {
                if (assetLoaderContext.Options.MaxVertexDataInitialCapacity > 0 && verticesCapacity > assetLoaderContext.Options.MaxVertexDataInitialCapacity)
                {
                    if (assetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        Debug.LogWarning($"Mesh [{Name}] vertex data capacity [{verticesCapacity}] exceeded the custom maximum capacity [{assetLoaderContext.Options.MaxVertexDataInitialCapacity}].");
                    }
                    verticesCapacity = 3;
                }
                OriginalVertexIndices = new List<int>(verticesCapacity);
                VerticesCapacity = verticesCapacity;
                GeometryCapacity = geometriesCapacity;
                Positions = new List<Vector3>(verticesCapacity);
                Normals = HasNormals ? new List<Vector3>(verticesCapacity) : null;
                Tangents = HasTangents ? new List<Vector4>(verticesCapacity) : null;
                Colors = HasColors ? new List<Color>(verticesCapacity) : null;
                UVs1 = HasUv1 ? new List<Vector2>(verticesCapacity) : null;
                UVs2 = HasUv2 ? new List<Vector2>(verticesCapacity) : null;
                UVs3 = HasUv3 ? new List<Vector2>(verticesCapacity) : null;
                UVs4 = HasUv4 ? new List<Vector2>(verticesCapacity) : null;
                GeometriesData = new Dictionary<int, IGeometry>(geometriesCapacity); //todo: another structure?
                if (assetLoaderContext.Options.MergeVertices)
                {
                    _mergedVertices = new Dictionary<PointerVertexData, int>(verticesCapacity);
                }
                _initialized = true;
            }
        }

        /// <summary>
        /// Transfers this geometry group vertex data into a blend shape target.
        /// </summary>
        /// <param name="baseGeometryGroup">The base geometry group receiving the blend shape data.</param>
        /// <param name="blendShapeKey">The blend shape key/target definition.</param>
        /// <param name="originalVertexIndices">The original vertex index mapping to use during the transfer.</param>
        public void TransferToBlendShape(IGeometryGroup baseGeometryGroup, IBlendShapeKey blendShapeKey, IList<int> originalVertexIndices)
        {
            GeometryGroupUtils.TransferToBlendShape(baseGeometryGroup, this, blendShapeKey, originalVertexIndices);
        }

        /// <summary>
        /// Commits the staged temporary vertex data fields into the internal vertex attribute lists.
        /// </summary>
        private void AddVertexData()
        {
            OriginalVertexIndices.Add(TempOriginalVertexIndex);
            HasInvalidNormals |= Mathf.Abs(TempNormal.x) > 1f || Mathf.Abs(TempNormal.y) > 1f || Mathf.Abs(TempNormal.z) > 1f;
            Positions.Add(TempPosition);
            if (HasNormals)
            {
                Normals.Add(TempNormal);
            }
            if (HasTangents)
            {
                Tangents.Add(TempTangent);
            }
            if (HasColors)
            {
                Colors.Add(TempColor);
            }
            if (HasUv1)
            {
                UVs1.Add(TempUV1);
            }
            if (HasUv2)
            {
                UVs2.Add(TempUV2);
            }
            if (HasUv3)
            {
                UVs3.Add(TempUV3);
            }
            if (HasUv4)
            {
                UVs4.Add(TempUV4);
            }
        }
    }
}
