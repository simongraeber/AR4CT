using System;
using System.Collections;
using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Provides helper methods used by geometry groups during mesh construction,
    /// including bone-weight normalization/limiting and blend-shape processing.
    /// </summary>
    public static class GeometryGroupUtils
    {
        /// <summary>
        /// Comparer used to sort <see cref="BoneWeight1"/> entries by weight (descending).
        /// </summary>
        private static readonly IComparer<BoneWeight1> BoneWeightComparer = new BoneWeightComparer();

        /// <summary>
        /// Limits the given bone weight list to at most 4 influences and normalizes the weights,
        /// returning a Unity <see cref="BoneWeight"/> structure.
        /// </summary>
        /// <remarks>
        /// The input list is sorted in-place (descending by weight). The top four influences are used.
        /// The selected weights are normalized so that their sum equals 1.
        /// </remarks>
        /// <param name="boneWeights1">The list of raw bone weight entries for a vertex.</param>
        /// <returns>A normalized <see cref="BoneWeight"/> with up to 4 influences.</returns>
        /// <exception cref="Exception">Thrown when the sum of weights is zero.</exception>
        public static BoneWeight LimitBoneWeights(List<BoneWeight1> boneWeights1)
        {
            boneWeights1.Sort(BoneWeightComparer);
            var boneWeight = new BoneWeight();
            var sum = 0f;
            var bonesCount = Mathf.Min(4, boneWeights1.Count);
            for (var i = 0; i < bonesCount; i++)
            {
                sum += boneWeights1[i].weight;
            }
            if (Mathf.Abs(sum) > 0f)
            {
                var invSum = 1f / sum;
                for (var k = 0; k < bonesCount; k++)
                {
                    var boneWeight1 = boneWeights1[k];
                    boneWeight1.weight *= invSum;
                    boneWeights1[k] = boneWeight1;
                }
            }
            else
            {
                throw new Exception("No weights");
            }
            if (bonesCount >= 1)
            {
                boneWeight.boneIndex0 = boneWeights1[0].boneIndex;
                boneWeight.weight0 = boneWeights1[0].weight;
            }
            if (bonesCount >= 2)
            {
                boneWeight.boneIndex1 = boneWeights1[1].boneIndex;
                boneWeight.weight1 = boneWeights1[1].weight;
            }
            if (bonesCount >= 3)
            {
                boneWeight.boneIndex2 = boneWeights1[2].boneIndex;
                boneWeight.weight2 = boneWeights1[2].weight;
            }
            if (bonesCount >= 4)
            {
                boneWeight.boneIndex3 = boneWeights1[3].boneIndex;
                boneWeight.weight3 = boneWeights1[3].weight;
            }
            return boneWeight;
        }

        /// <summary>
        /// Transfers vertex deltas from <paramref name="geometryGroup"/> into a blend shape key,
        /// using <paramref name="baseGeometryGroup"/> as the reference.
        /// </summary>
        /// <remarks>
        /// This method populates <see cref="IBlendShapeKey.Vertices"/> with delta positions
        /// (blend shape position - base position) and creates an index map from original vertex indices
        /// to blend shape vertex indices.
        /// Existing blend shape buffers are disposed when supported.
        /// </remarks>
        /// <param name="baseGeometryGroup">The reference geometry group used as the base shape.</param>
        /// <param name="geometryGroup">The geometry group containing the target (morphed) positions.</param>
        /// <param name="blendShapeKey">The blend shape key to populate.</param>
        /// <param name="originalVertexIndices">The original vertex index mapping for the base geometry.</param>
        public static void TransferToBlendShape(IGeometryGroup baseGeometryGroup, IGeometryGroup geometryGroup, IBlendShapeKey blendShapeKey, IList<int> originalVertexIndices)
        {
            blendShapeKey.Vertices?.TryToDispose();
            blendShapeKey.IndexMap?.TryToDispose<IDictionary<int, int>>();

            var vertices = new List<Vector3>(baseGeometryGroup.VerticesDataCount);
            var indexMap = new Dictionary<int, int>(baseGeometryGroup.VerticesDataCount);
            var indexMapCount = 0;

            for (var i = 0; i < baseGeometryGroup.VerticesDataCount; i++)
            {
                var originalVertexIndex = originalVertexIndices[i];
                if (!indexMap.ContainsKey(originalVertexIndex))
                {
                    var baseVertex = baseGeometryGroup.Positions[i];
                    vertices.Add(geometryGroup.Positions[i] - baseVertex);
                    indexMap.Add(originalVertexIndex, indexMapCount++);
                }
            }

            blendShapeKey.Vertices = vertices;
            blendShapeKey.IndexMap = indexMap;
        }

        /// <summary>
        /// Normalizes and appends all weights from <paramref name="weightsList"/> into <paramref name="allWeightsList"/>,
        /// returning the number of weights appended as a <see cref="byte"/>.
        /// </summary>
        /// <remarks>
        /// The input list is sorted in-place (descending by weight). All influences in <paramref name="weightsList"/>
        /// are normalized to sum to 1 and appended to <paramref name="allWeightsList"/>.
        /// </remarks>
        /// <param name="allWeightsList">The output list receiving normalized weights.</param>
        /// <param name="weightsList">The source list of weights for a vertex.</param>
        /// <returns>The number of weights appended; returns 0 if the sum of weights is zero.</returns>
        public static byte LimitMaxBoneWeights(List<BoneWeight1> allWeightsList, List<BoneWeight1> weightsList)
        {
            weightsList.Sort(BoneWeightComparer);
            var bonesCount = (byte)weightsList.Count;
            var sum = 0f;
            for (var j = 0; j < bonesCount; j++)
            {
                sum += weightsList[j].weight;
            }
            if (Mathf.Abs(sum) > 0f)
            {
                var invSum = 1f / sum;
                for (var k = 0; k < bonesCount; k++)
                {
                    var boneWeight1 = weightsList[k];
                    boneWeight1.weight *= invSum;
                    allWeightsList.Add(boneWeight1);
                }
                return bonesCount;
            }
            return 0;
        }

        /// <summary>
        /// Adds blend shape frames to the target mesh using the provided blend shape keys.
        /// </summary>
        /// <remarks>
        /// For each key, this method builds delta arrays for vertices (and optionally normals/tangents)
        /// and calls <see cref="Mesh.AddBlendShapeFrame(string,float,Vector3[],Vector3[],Vector3[])"/>.
        /// This method is implemented as an iterator to allow yielding back to the main thread between frames.
        /// </remarks>
        /// <param name="assetLoaderContext">The asset loader context and options.</param>
        /// <param name="geometryGroup">The geometry group containing the base mesh data and the target mesh.</param>
        /// <param name="blendShapeKeys">The blend shape keys to process.</param>
        /// <returns>An enumerator used by the importer pipeline to schedule work across frames.</returns>
        public static IEnumerable ProcessBlendShapeKeys(AssetLoaderContext assetLoaderContext, CommonGeometryGroup geometryGroup, List<IBlendShapeKey> blendShapeKeys)
        {
            var hasNormals = false;
            var hasTangents = false;
            foreach (var blendShapeKey in blendShapeKeys)
            {
                hasNormals |= blendShapeKey.Normals != null;
                hasTangents |= blendShapeKey.Tangents != null;
            }

            foreach (var blendShapeKey in blendShapeKeys)
            {
                var deltaVertices = new Vector3[geometryGroup.VerticesDataCount];
                var deltaNormals = assetLoaderContext.Options.CalculateBlendShapeNormals || assetLoaderContext.Options.ImportBlendShapeNormals && hasNormals
                    ? new Vector3[geometryGroup.VerticesDataCount]
                    : null;
                var deltaTangents = assetLoaderContext.Options.CalculateBlendShapeNormals || assetLoaderContext.Options.ImportBlendShapeNormals && hasTangents
                    ? new Vector3[geometryGroup.VerticesDataCount]
                    : null;

                for (var j = 0; j < geometryGroup.VerticesDataCount; j++)
                {
                    var originalVertexIndex = geometryGroup.OriginalVertexIndices[j];
                    if (blendShapeKey.IndexMap.TryGetValue(originalVertexIndex, out var blendShapeVertexIndex))
                    {
                        deltaVertices[j] = blendShapeKey.Vertices[blendShapeVertexIndex];
                        if (assetLoaderContext.Options.ImportBlendShapeNormals)
                        {
                            if (deltaNormals != null)
                            {
                                deltaNormals[j] = blendShapeKey.Normals[blendShapeVertexIndex];
                            }
                            if (deltaTangents != null)
                            {
                                deltaTangents[j] = blendShapeKey.Tangents[blendShapeVertexIndex];
                            }
                        }
                    }
                }

                if (!assetLoaderContext.Options.ImportBlendShapeNormals && assetLoaderContext.Options.CalculateBlendShapeNormals)
                {
                    var tempVertices = new Vector3[geometryGroup.VerticesDataCount];
                    for (var i = 0; i < tempVertices.Length; i++)
                    {
                        tempVertices[i] = geometryGroup.Positions[i] + deltaVertices[i];
                    }

                    deltaNormals = NormalSolver.CalculateNormals(tempVertices, geometryGroup.GeometriesData, assetLoaderContext);
                    if (geometryGroup.HasUv1 || geometryGroup.HasUv2 || geometryGroup.HasUv3 || geometryGroup.HasUv4)
                    {
                        deltaTangents = TangentSolver.CalculateTangents(geometryGroup, tempVertices, deltaNormals, assetLoaderContext);
                        for (var i = 0; i < deltaTangents.Length; i++)
                        {
                            deltaTangents[i] -= (Vector3)geometryGroup.Tangents[i];
                        }
                    }

                    for (var i = 0; i < deltaNormals.Length; i++)
                    {
                        deltaNormals[i] -= geometryGroup.Normals[i];
                    }

                    tempVertices.TryToDispose();
                }

                var blendShapeName = blendShapeKey.Name;
                if (geometryGroup.Mesh.GetBlendShapeIndex(blendShapeName) >= 0)
                {
                    blendShapeName += "(0)";
                }

                geometryGroup.Mesh.AddBlendShapeFrame(blendShapeName, blendShapeKey.FrameWeight, deltaVertices, deltaNormals, deltaTangents);

                foreach (var item in assetLoaderContext.ReleaseMainThread())
                {
                    yield return item;
                }
            }
        }
    }
}
