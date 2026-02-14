using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Represents a concrete implementation of <see cref="IGeometry"/> that holds vertex and face data
    /// within a <see cref="CommonGeometryGroup"/>. Multiple <see cref="CommonGeometry"/> instances can
    /// exist within a single group, each referencing a shared vertex pool.
    /// </summary>
    public class CommonGeometry : IGeometry
    {
        /// <summary>
        /// Gets or sets the parent geometry group that contains this geometry.
        /// </summary>
        public CommonGeometryGroup GeometryGroup { get; set; }

        /// <summary>
        /// Gets or sets the index of this geometry within its parent geometry group.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this geometry should be processed as quads.
        /// If false, the geometry is typically handled as triangles.
        /// </summary>
        public bool IsQuad { get; set; }

        /// <summary>
        /// Gets or sets the index of the material assigned to this geometry.
        /// </summary>
        public int MaterialIndex { get; set; }

        /// <summary>
        /// Gets or sets the original geometry index within the parent geometry group.
        /// If this geometry was duplicated, this field preserves the initial index.
        /// </summary>
        public int OriginalIndex { get; set; }

        /// <summary>
        /// Gets or sets the list of indices into the shared vertex data
        /// managed by <see cref="CommonGeometryGroup"/>. These indices map 
        /// this geometry's faces to the group's vertex pool.
        /// </summary>
        public List<int> VertexDataIndices { get; set; }

        /// <summary>
        /// Adds a new vertex (with position, normal, tangent, color, UVs, and bone weighting)
        /// to the parent geometry group's vertex pool and records the resulting index 
        /// in <see cref="VertexDataIndices"/> if this geometry is not a point cloud.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The context containing model loading data and settings.
        /// </param>
        /// <param name="originalVertexIndex">The source vertex index from the loaded model.</param>
        /// <param name="position">The vertex position.</param>
        /// <param name="normal">The vertex normal.</param>
        /// <param name="tangent">The vertex tangent.</param>
        /// <param name="color">The vertex color.</param>
        /// <param name="uv0">The first set of UV coordinates.</param>
        /// <param name="uv1">The second set of UV coordinates.</param>
        /// <param name="uv2">The third set of UV coordinates.</param>
        /// <param name="uv3">The fourth set of UV coordinates.</param>
        /// <param name="boneWeight">The bone weight data, if applicable.</param>
        public void AddVertex(
            AssetLoaderContext assetLoaderContext,
            int originalVertexIndex,
            Vector3 position,
            Vector3 normal= default,
            Vector4 tangent = default,
            Color color = default,
            Vector2 uv0= default,
            Vector2 uv1= default,
            Vector2 uv2 = default,
            Vector2 uv3 = default,
            BoneWeight boneWeight = default
        )
        {
            var vertexDataIndex = GeometryGroup.AddVertex(
                assetLoaderContext,
                originalVertexIndex,
                position,
                normal,
                tangent,
                color,
                uv0,
                uv1,
                uv2,
                uv3,
                boneWeight);

            // For non-point cloud geometries, store the index referencing the shared vertex pool.
            if (!assetLoaderContext.Options.LoadPointClouds)
            {
                VertexDataIndices.Add(vertexDataIndex);
            }
        }

        /// <summary>
        /// Configures this geometry with the specified parent group, material index, face mode (quads or triangles),
        /// blend shape usage, and point cloud status. If the geometry is not a point cloud, 
        /// this method also pre-allocates a list for storing vertex indices.
        /// </summary>
        /// <param name="geometryGroup">The parent geometry group.</param>
        /// <param name="materialIndex">The material index for this geometry.</param>
        /// <param name="isQuad">Indicates if this geometry uses quadrilateral faces.</param>
        /// <param name="hasBlendShapes">Indicates if this geometry uses blend shapes.</param>
        /// <param name="isPointCloud">Indicates if this geometry is a point cloud.</param>
        public void Setup(
            CommonGeometryGroup geometryGroup,
            int materialIndex,
            bool isQuad,
            bool hasBlendShapes,
            bool isPointCloud)
        {
            GeometryGroup = geometryGroup;
            IsQuad = isQuad;
            Index = geometryGroup.GeometriesData.Count;
            OriginalIndex = Index;
            MaterialIndex = materialIndex;

            // Allocate vertex index storage if this geometry is not a point cloud.
            if (!isPointCloud)
            {
                var capacity = geometryGroup.VerticesCapacity * 3;
                VertexDataIndices = new List<int>(capacity);
            }
        }
    }
}
