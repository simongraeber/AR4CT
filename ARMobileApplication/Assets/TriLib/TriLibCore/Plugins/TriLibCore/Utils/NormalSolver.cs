using System.Collections.Generic;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for calculating normals on vertex data, including support for both <see cref="Vector3"/> and generic <see cref="IVertexData"/>.
    /// </summary>
    /// based on: https://gist.github.com/runevision/6fd7cc8d841245a53df5d09ccf6b47ff
    internal static class NormalSolver
    {
        /// <summary>
        /// Calculates per-vertex normals in-place for the specified <paramref name="normals"/> array 
        /// based on the provided <paramref name="verticesData"/> and <paramref name="geometriesData"/>.
        /// </summary>
        /// <param name="normals">
        /// An <see cref="IList{Vector3}"/> that will store the computed normals. 
        /// It should already be sized to match the length of <paramref name="verticesData"/>.
        /// </param>
        /// <param name="verticesData">The collection of vertex positions used to calculate normals.</param>
        /// <param name="geometriesData">A mapping of geometry indices to <see cref="IGeometry"/> instances.</param>
        /// <param name="assetLoaderContext">Contains data and settings (such as smoothing angle) for the normal calculation.</param>
        private static void CalculateNormalsInternal(
            IList<Vector3> normals,
            IList<Vector3> verticesData,
            IDictionary<int, IGeometry> geometriesData,
            AssetLoaderContext assetLoaderContext)
        {
            var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(verticesData.Count);

            // Local function to group vertices by their approximate position.
            void AddToDictionary(int vertexIndex, int geometryIndex, int elementIndex)
            {
                var vertexKey = new VertexKey(verticesData[vertexIndex]);
                if (!dictionary.TryGetValue(vertexKey, out var entry))
                {
                    entry = new List<VertexEntry>(4);
                    dictionary.Add(vertexKey, entry);
                }
                var vertexEntry = new VertexEntry(geometryIndex, elementIndex, vertexIndex);
                entry.Add(vertexEntry);
            }

            var cosineThreshold = Mathf.Cos(assetLoaderContext.Options.SmoothingAngle * Mathf.Deg2Rad);
            var elementNormals = new Vector3[geometriesData.Count][];

            // Compute face normals for each geometry.
            foreach (var geometry in geometriesData.Values)
            {
                var geometryIndex = geometry.Index;
                var elements = geometry.VertexDataIndices;
                elementNormals[geometryIndex] = new Vector3[geometry.IsQuad ? elements.Count / 4 : elements.Count / 3];

                for (var elementIndex = 0; elementIndex < elements.Count; elementIndex += geometry.IsQuad ? 4 : 3)
                {
                    var i1 = elements[elementIndex + 0];
                    var i2 = elements[elementIndex + 1];
                    var i3 = elements[elementIndex + 2];

                    var p1 = (verticesData[i2] - verticesData[i1]);
                    var p2 = (verticesData[i3] - verticesData[i1]);
                    var elementNormalIndex = elementIndex / (geometry.IsQuad ? 4 : 3);

                    // Calculate the normal for this face (triangle or quad).
                    elementNormals[geometryIndex][elementNormalIndex] = Vector3.Cross(p1, p2).normalized;

                    // Group the vertex indices by their approximate position, to handle smoothing.
                    AddToDictionary(i1, geometryIndex, elementNormalIndex);
                    AddToDictionary(i2, geometryIndex, elementNormalIndex);
                    AddToDictionary(i3, geometryIndex, elementNormalIndex);

                    // If it's a quad, process the fourth vertex.
                    if (geometry.IsQuad)
                    {
                        var i4 = elements[elementIndex + 3];
                        AddToDictionary(i4, geometryIndex, elementNormalIndex);
                    }
                }
            }

            // Aggregate face normals for each vertex, based on the smoothing angle.
            foreach (var vertexList in dictionary.Values)
            {
                for (var j = 0; j < vertexList.Count; j++)
                {
                    // LHS = left-hand side, the "current" vertex.
                    var lhsEntry = vertexList[j];
                    var sum = new Vector3();

                    for (var i = 0; i < vertexList.Count; i++)
                    {
                        assetLoaderContext.CancellationToken.ThrowIfCancellationRequested();
                        var rhsEntry = vertexList[i];

                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                        {
                            // Same vertex, always add its face normal.
                            sum += elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex];
                        }
                        else
                        {
                            // Different vertex. Check if the angle between face normals is within the threshold.
                            var dot = Vector3.Dot(
                                elementNormals[lhsEntry.MeshIndex][lhsEntry.ElementIndex],
                                elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex]
                            );
                            if (dot >= cosineThreshold)
                            {
                                sum += elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex];
                            }
                        }
                    }

                    normals[lhsEntry.VertexIndex] = sum.normalized;
                }
            }
        }

        /// <summary>
        /// Calculates per-vertex normals for the given <paramref name="verticesData"/> and <paramref name="geometriesData"/>,
        /// and returns them as an array of <see cref="Vector3"/>.
        /// </summary>
        /// <param name="verticesData">The array of vertex positions.</param>
        /// <param name="geometriesData">A dictionary of geometry data, keyed by geometry index.</param>
        /// <param name="assetLoaderContext">Contains data and settings (such as smoothing angle) for the normal calculation.</param>
        /// <returns>An array of <see cref="Vector3"/> representing the computed normals.</returns>
        public static Vector3[] CalculateNormals(
            IList<Vector3> verticesData,
            IDictionary<int, IGeometry> geometriesData,
            AssetLoaderContext assetLoaderContext)
        {
            var normals = new Vector3[verticesData.Count];
            CalculateNormalsInternal(normals, verticesData, geometriesData, assetLoaderContext);
            return normals;
        }

        /// <summary>
        /// Calculates per-vertex normals for the given <paramref name="verticesData"/> and <paramref name="geometriesData"/>,
        /// and returns them as a <see cref="List{Vector3}"/>.
        /// </summary>
        /// <param name="verticesData">The list of vertex positions.</param>
        /// <param name="geometriesData">A dictionary of geometry data, keyed by geometry index.</param>
        /// <param name="assetLoaderContext">Contains data and settings (such as smoothing angle) for the normal calculation.</param>
        /// <returns>A <see cref="List{Vector3}"/> containing the computed normals.</returns>
        public static List<Vector3> CalculateNormalsAsList(
            IList<Vector3> verticesData,
            IDictionary<int, IGeometry> geometriesData,
            AssetLoaderContext assetLoaderContext)
        {
            var normals = new List<Vector3>(verticesData.Count);
            for (var i = 0; i < verticesData.Count; i++)
            {
                normals.Add(default);
            }
            CalculateNormalsInternal(normals, verticesData, geometriesData, assetLoaderContext);
            return normals;
        }

        /// <summary>
        /// Calculates per-vertex normals in-place for the given collection of <see cref="IVertexData"/> objects,
        /// using the specified <paramref name="geometryGroup"/> and <paramref name="geometriesData"/>.
        /// </summary>
        /// <typeparam name="T">
        /// The type of vertex data, which must implement <see cref="IVertexData"/> to allow 
        /// position and normal manipulation.
        /// </typeparam>
        /// <param name="geometryGroup">
        /// The geometry group context used to interpret and set vertex positions and normals.
        /// </param>
        /// <param name="verticesData">The list of vertex data objects to process.</param>
        /// <param name="geometriesData">A dictionary of geometry data, keyed by geometry index.</param>
        /// <param name="assetLoaderContext">Contains data and settings (such as smoothing angle) for the normal calculation.</param>
        public static void CalculateNormals<T>(
            IGeometryGroup geometryGroup,
            IList<T> verticesData,
            IDictionary<int, IGeometry> geometriesData,
            AssetLoaderContext assetLoaderContext)
            where T : IVertexData
        {
            var dictionary = new Dictionary<VertexKey, List<VertexEntry>>(verticesData.Count);

            // Local function to group vertices by approximate position.
            void AddToDictionary(int vertexIndex, int geometryIndex, int elementIndex)
            {
                var vertexKey = new VertexKey(verticesData[vertexIndex].GetPosition(geometryGroup));
                if (!dictionary.TryGetValue(vertexKey, out var entry))
                {
                    entry = new List<VertexEntry>(4);
                    dictionary.Add(vertexKey, entry);
                }
                var vertexEntry = new VertexEntry(geometryIndex, elementIndex, vertexIndex);
                entry.Add(vertexEntry);
            }

            var cosineThreshold = Mathf.Cos(assetLoaderContext.Options.SmoothingAngle * Mathf.Deg2Rad);
            var elementNormals = new Vector3[geometriesData.Count][];

            // Compute face normals for each geometry.
            foreach (var geometry in geometriesData.Values)
            {
                var geometryIndex = geometry.Index;
                var elements = geometry.VertexDataIndices;
                elementNormals[geometryIndex] = new Vector3[geometry.IsQuad ? elements.Count / 4 : elements.Count / 3];

                for (var elementIndex = 0; elementIndex < elements.Count; elementIndex += geometry.IsQuad ? 4 : 3)
                {
                    var i1 = elements[elementIndex + 0];
                    var i2 = elements[elementIndex + 1];
                    var i3 = elements[elementIndex + 2];

                    var p1 = (verticesData[i2].GetPosition(geometryGroup) - verticesData[i1].GetPosition(geometryGroup)).normalized;
                    var p2 = (verticesData[i3].GetPosition(geometryGroup) - verticesData[i1].GetPosition(geometryGroup)).normalized;
                    var elementNormalIndex = elementIndex / (geometry.IsQuad ? 4 : 3);

                    // Calculate the normal for this face (triangle or quad).
                    elementNormals[geometryIndex][elementNormalIndex] = Vector3.Cross(p1, p2).normalized;

                    // Group the vertex indices by approximate position, to handle smoothing.
                    AddToDictionary(i1, geometryIndex, elementNormalIndex);
                    AddToDictionary(i2, geometryIndex, elementNormalIndex);
                    AddToDictionary(i3, geometryIndex, elementNormalIndex);

                    // If it's a quad, process the fourth vertex.
                    if (geometry.IsQuad)
                    {
                        var i4 = elements[elementIndex + 3];
                        AddToDictionary(i4, geometryIndex, elementNormalIndex);
                    }
                }
            }

            // Aggregate face normals for each vertex, based on the smoothing angle.
            foreach (var vertexList in dictionary.Values)
            {
                for (var j = 0; j < vertexList.Count; j++)
                {
                    var lhsEntry = vertexList[j];
                    var sum = new Vector3();

                    for (var i = 0; i < vertexList.Count; i++)
                    {
                        assetLoaderContext.CancellationToken.ThrowIfCancellationRequested();
                        var rhsEntry = vertexList[i];

                        if (lhsEntry.VertexIndex == rhsEntry.VertexIndex)
                        {
                            // Same vertex, always add its face normal.
                            sum += elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex];
                        }
                        else
                        {
                            // Different vertex. Check if the angle between face normals is within the threshold.
                            var dot = Vector3.Dot(
                                elementNormals[lhsEntry.MeshIndex][lhsEntry.ElementIndex],
                                elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex]
                            );
                            if (dot >= cosineThreshold)
                            {
                                sum += elementNormals[rhsEntry.MeshIndex][rhsEntry.ElementIndex];
                            }
                        }
                    }

                    // Set the computed normal directly on the vertex data.
                    var vertexData = verticesData[lhsEntry.VertexIndex];
                    vertexData.SetNormal(sum.normalized, geometryGroup);
                    verticesData[lhsEntry.VertexIndex] = vertexData;
                }
            }
        }

        /// <summary>
        /// Represents a unique key for a vertex based on its approximate position, 
        /// used for grouping vertices with similar or identical coordinates.
        /// </summary>
        private struct VertexKey
        {
            private readonly long _x;
            private readonly long _y;
            private readonly long _z;

            // Change this if you require a different precision.
            private const int Tolerance = 100000;

            /// <summary>
            /// Initializes a new instance of the <see cref="VertexKey"/> struct, 
            /// rounding the specified <see cref="Vector3"/> position to a fixed precision.
            /// </summary>
            /// <param name="position">The vertex position.</param>
            public VertexKey(Vector3 position)
            {
                _x = (long)(Mathf.Round(position.x * Tolerance));
                _y = (long)(Mathf.Round(position.y * Tolerance));
                _z = (long)(Mathf.Round(position.z * Tolerance));
            }

            /// <inheritdoc />
            public override bool Equals(object obj)
            {
                var key = (VertexKey)obj;
                return _x == key._x && _y == key._y && _z == key._z;
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                // Combine the rounded coordinates to produce a hash.
                return (_x * 7 ^ _y * 13 ^ _z * 27).GetHashCode();
            }
        }

        /// <summary>
        /// Stores an index to a particular geometry (mesh), an element (face) within that geometry,
        /// and the vertex index within the overall vertex collection.
        /// </summary>
        private struct VertexEntry
        {
            /// <summary>
            /// The index of the mesh in the GeometriesData.
            /// </summary>
            public int MeshIndex;

            /// <summary>
            /// The index of the face (triangle or quad) within the mesh.
            /// </summary>
            public int ElementIndex;

            /// <summary>
            /// The index of the vertex within the overall vertex collection.
            /// </summary>
            public int VertexIndex;

            /// <summary>
            /// Initializes a new instance of the <see cref="VertexEntry"/> struct with the specified indices.
            /// </summary>
            /// <param name="meshIndex">The index of the mesh.</param>
            /// <param name="elementIndex">The index of the face (triangle or quad).</param>
            /// <param name="vertexIndex">The index of the vertex in the main vertex list.</param>
            public VertexEntry(int meshIndex, int elementIndex, int vertexIndex)
            {
                MeshIndex = meshIndex;
                ElementIndex = elementIndex;
                VertexIndex = vertexIndex;
            }
        }
    }
}
