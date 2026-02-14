using System.Collections.Generic;
using TriLibCore.Geometries;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Geometry. There may be multiple Geometries within a single 
    /// <see cref="CommonGeometryGroup"/> (or any other group implementing geometry collections).
    /// </summary>
    public interface IGeometry
    {
        /// <summary>
        /// Gets or sets the index of this Geometry within its parent Geometry Group.
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// Gets or sets the original Geometry index within the parent Geometry Group. 
        /// Sometimes a Geometry can be duplicated, and this field is used to track 
        /// its original index.
        /// </summary>
        int OriginalIndex { get; set; }

        /// <summary>
        /// Gets or sets the material index used by this Geometry.
        /// </summary>
        int MaterialIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this Geometry contains quadrilateral faces 
        /// (quads) instead of triangular faces.
        /// </summary>
        bool IsQuad { get; set; }

        /// <summary>
        /// Gets or sets the list of indices into the Geometry Group's vertex data. 
        /// Each element of this list corresponds to a vertex position entry 
        /// (and optionally normal, tangent, UV, etc.).
        /// </summary>
        List<int> VertexDataIndices { get; set; }

        /// <summary>
        /// Gets or sets the parent <see cref="CommonGeometryGroup"/> that contains 
        /// this Geometry.
        /// </summary>
        CommonGeometryGroup GeometryGroup { get; set; }

        /// <summary>
        /// Adds a new vertex to this Geometry, defining its position, normal, tangent, color, 
        /// UV coordinates, and bone weight data.
        /// </summary>
        /// <param name="assetLoaderContext">The <see cref="AssetLoaderContext"/> that holds 
        /// information about the model-loading process.</param>
        /// <param name="originalVertexIndex">The original vertex index.</param>
        /// <param name="position">The position vector for the new vertex.</param>
        /// <param name="normal">The normal vector for the new vertex.</param>
        /// <param name="tangent">The tangent vector for the new vertex.</param>
        /// <param name="color">The color of the new vertex.</param>
        /// <param name="uv0">The first UV set coordinate.</param>
        /// <param name="uv1">The second UV set coordinate.</param>
        /// <param name="uv2">The third UV set coordinate.</param>
        /// <param name="uv3">The fourth UV set coordinate.</param>
        /// <param name="boneWeight">The bone weight data for skinning.</param>
        void AddVertex(
            AssetLoaderContext assetLoaderContext,
            int originalVertexIndex,
            Vector3 position,
            Vector3 normal = default,
            Vector4 tangent = default,
            Color color = default,
            Vector2 uv0 = default,
            Vector2 uv1 = default,
            Vector2 uv2 = default,
            Vector2 uv3 = default,
            BoneWeight boneWeight = default
        );

        /// <summary>
        /// Configures this Geometry with its parent group, material index, and other properties 
        /// that affect how its vertices and faces are handled.
        /// </summary>
        /// <param name="geometryGroup">The parent geometry group to which this Geometry belongs.</param>
        /// <param name="materialIndex">The index of the material associated with this Geometry.</param>
        /// <param name="isQuad">Whether this Geometry uses quadrilateral faces.</param>
        /// <param name="hasBlendShapes">Whether this Geometry includes blend shape data.</param>
        /// <param name="isPointCloud">Whether this Geometry is a point cloud (vertices only, no faces).</param>
        void Setup(
            CommonGeometryGroup geometryGroup,
            int materialIndex,
            bool isQuad,
            bool hasBlendShapes,
            bool isPointCloud);
    }
}
