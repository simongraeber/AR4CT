using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides functionality to calculate tangent vectors for a set of vertices.
    /// </summary>
    internal static class TangentSolver
    {
        /// <summary>
        /// Calculates the tangent vectors for the given geometry data. Each returned tangent corresponds to the 
        /// associated vertex index from <paramref name="vertices"/>.
        /// </summary>
        /// <param name="geometryGroup">
        /// The <see cref="IGeometryGroup"/> containing geometry and UV data necessary for computing tangents.
        /// </param>
        /// <param name="vertices">
        /// A list of vertex positions used in the geometry.
        /// </param>
        /// <param name="normals">
        /// A list of vertex normals used to orthonormalize the resulting tangents.
        /// </param>
        /// <param name="assetLoaderContext">
        /// Contextual information for asset loading, including a cancellation token for cooperative cancellation.
        /// </param>
        /// <returns>
        /// An array of <see cref="Vector3"/> representing the computed tangent vectors for each vertex.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Tangent vectors are computed using the vertices, their associated normals, and the primary UV set 
        /// (<c>geometryGroup.UVs1</c>).
        /// </para>
        /// <para>
        /// The algorithm computes S- and T-directions for each triangle (or quad) face, sums them across shared vertices, 
        /// and then orthonormalizes the resulting vectors against each vertex's normal.
        /// </para>
        /// </remarks>
        internal static Vector3[] CalculateTangents(
            IGeometryGroup geometryGroup,
            IList<Vector3> vertices,
            IList<Vector3> normals,
            AssetLoaderContext assetLoaderContext
        )
        {
            var vertexCount = vertices.Count;
            var tangents = new Vector3[vertexCount];

            // Temporary arrays to accumulate S and T directions per vertex.
            var tan1 = new Vector3[vertexCount];
            var tan2 = new Vector3[vertexCount];

            var geometriesData = geometryGroup.GeometriesData;

            // For each geometry, sum the S and T vectors across the faces (triangles or quads).
            foreach (var kvp in geometriesData)
            {
                var geometry = kvp.Value;
                var elements = geometry.VertexDataIndices;

                // Process triangles or quads.
                for (var elementIndex = 0; elementIndex < elements.Count; elementIndex += geometry.IsQuad ? 4 : 3)
                {
                    var i1 = elements[elementIndex + 0];
                    var i2 = elements[elementIndex + 1];
                    var i3 = elements[elementIndex + 2];

                    // Retrieve vertex positions.
                    var v1 = vertices[i1];
                    var v2 = vertices[i2];
                    var v3 = vertices[i3];

                    // Retrieve UV coordinates.
                    var w1 = geometryGroup.UVs1[i1];
                    var w2 = geometryGroup.UVs1[i2];
                    var w3 = geometryGroup.UVs1[i3];

                    // Calculate positional deltas.
                    var x1 = v2.x - v1.x;
                    var x2 = v3.x - v1.x;
                    var y1 = v2.y - v1.y;
                    var y2 = v3.y - v1.y;
                    var z1 = v2.z - v1.z;
                    var z2 = v3.z - v1.z;

                    // Calculate UV deltas.
                    var s1 = w2.x - w1.x;
                    var s2 = w3.x - w1.x;
                    var t1 = w2.y - w1.y;
                    var t2 = w3.y - w1.y;

                    // Compute the reciprocal to avoid repeated division.
                    var r = 1.0f / (s1 * t2 - s2 * t1);

                    // S and T directional vectors.
                    var sdir = new Vector3(
                        (t2 * x1 - t1 * x2) * r,
                        (t2 * y1 - t1 * y2) * r,
                        (t2 * z1 - t1 * z2) * r
                    );
                    var tdir = new Vector3(
                        (s1 * x2 - s2 * x1) * r,
                        (s1 * y2 - s2 * y1) * r,
                        (s1 * z2 - s2 * z1) * r
                    );

                    // Accumulate the S and T vectors.
                    tan1[i1] += sdir;
                    tan1[i2] += sdir;
                    tan1[i3] += sdir;

                    tan2[i1] += tdir;
                    tan2[i2] += tdir;
                    tan2[i3] += tdir;

                    // If processing quads, handle the fourth vertex.
                    if (geometry.IsQuad)
                    {
                        var i4 = elements[elementIndex + 3];
                        tan1[i4] += sdir;
                        tan2[i4] += tdir;
                    }
                }
            }

            // Orthonormalize each vertex's tangent against its normal.
            for (var a = 0; a < vertexCount; ++a)
            {
                var n = normals[a];
                var t = tan1[a];

                // OrthoNormalize modifies the second vector to be orthogonal to the first.
                Vector3.OrthoNormalize(ref n, ref t);

                var tangent = tangents[a];
                tangent.x = t.x;
                tangent.y = t.y;
                tangent.z = t.z;

                tangents[a] = tangent;

                // Check for cancellation at this point.
                assetLoaderContext.CancellationToken.ThrowIfCancellationRequested();
            }

            return tangents;
        }
    }
}
