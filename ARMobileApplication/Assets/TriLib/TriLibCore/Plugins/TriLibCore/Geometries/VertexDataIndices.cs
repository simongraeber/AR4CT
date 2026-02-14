using System.Collections.Generic;

namespace TriLibCore.Geometries
{
    
    /// <summary>Represents the mapping between Geometry elements and the Geometry Group elements.</summary>
    public struct VertexDataIndices : IEqualityComparer<VertexDataIndices>
    {
        /// <summary>
        /// Geometry group vertex index.
        /// </summary>
        public readonly int VertexIndex;

        /// <summary>
        /// Geometry group normal index, or -1 when there is no normal for this vertex.
        /// </summary>
        public readonly int NormalIndex;

        ///// <summary>
        ///// Geometry group tangent index, or -1 when there is no tangent for this vertex.
        ///// </summary>
        public readonly int TangentIndex;

        /// <summary>
        /// Geometry group uv channel 1 index, or -1 when there is no uv for this vertex.
        /// </summary>
        public readonly int UvIndex;

        /// <summary>
        /// Geometry group uv channel 2 index, or -1 when there is no uv for this vertex.
        /// </summary>
        public readonly int UvIndex2;

        /// <summary>
        /// Geometry group uv channel 3 index, or -1 when there is no uv for this vertex.
        /// </summary>
        public readonly int UvIndex3;

        /// <summary>
        /// Geometry group uv channel 4 index, or -1 when there is no uv for this vertex.
        /// </summary>
        public readonly int UvIndex4;

        /// <summary>
        /// Geometry group color index, or -1 when there is no color for this vertex.
        /// </summary>
        public readonly int ColorIndex;

        private readonly int _hashCode;

        //private byte[] _hashCode2;

        /// <summary>Represents the mapping between Geometry Group elements and its Geometries. There is one Vertex Data per vertex in every Geometry.</summary>
        /// <param name="vertexIndex">The vertex element index.</param>
        /// <param name="normalIndex">The normal element index.</param>
        /// <param name="tangentIndex">The tangent element index.</param>
        /// <param name="uvIndex">The uv1 element index.</param>
        /// <param name="uvIndex2">The uv2 element index.</param>
        /// <param name="uvIndex3">The uv3 element index.</param>
        /// <param name="uvIndex4">The uv4 element index.</param>
        /// <param name="colorIndex">The color element index.</param>
        public VertexDataIndices(int vertexIndex, int normalIndex, int tangentIndex, int uvIndex, int uvIndex2, int uvIndex3, int uvIndex4, int colorIndex)
        {
            VertexIndex = vertexIndex;
            NormalIndex = normalIndex;
            TangentIndex = tangentIndex;
            UvIndex = uvIndex;
            UvIndex2 = uvIndex2;
            UvIndex3 = uvIndex3;
            UvIndex4 = uvIndex4;
            ColorIndex = colorIndex;
            unchecked
            {
                _hashCode = VertexIndex;
                _hashCode = (_hashCode * 397) ^ NormalIndex;
                _hashCode = (_hashCode * 397) ^ TangentIndex;
                _hashCode = (_hashCode * 397) ^ UvIndex;
                _hashCode = (_hashCode * 397) ^ UvIndex2;
                _hashCode = (_hashCode * 397) ^ UvIndex3;
                _hashCode = (_hashCode * 397) ^ UvIndex4;
                _hashCode = (_hashCode * 397) ^ ColorIndex;
            }
        }


        /// <summary>Determines whether the specified Vertex Data is equal to this instance.</summary>
        /// <param name="other">The Other Vertex Data.</param>
        /// <returns>
        /// <c>true</c> if vertex data are equals, <c>false</c> otherwise.</returns>
        public bool Equals(VertexDataIndices other)
        {
            return
                VertexIndex == other.VertexIndex &&
                NormalIndex == other.NormalIndex &&
                TangentIndex == other.TangentIndex &&
                UvIndex == other.UvIndex &&
                UvIndex2 == other.UvIndex2 &&
                UvIndex3 == other.UvIndex3 &&
                UvIndex4 == other.UvIndex4 &&
                ColorIndex == other.ColorIndex;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The Object to compare with the current instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object obj)
        {
            return obj is VertexDataIndices other && Equals(other);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>Determines whether the specified Vertex Data are equals.</summary>
        /// <param name="x">The first Vertex Data to compare.</param>
        /// <param name="y">The second Vertex Data to compare.</param>
        /// <returns>true if the specified objects are equal; otherwise, false.</returns>
        public bool Equals(VertexDataIndices x, VertexDataIndices y)
        {
            return x.VertexIndex == y.VertexIndex &&
                   x.NormalIndex == y.NormalIndex &&
                   x.TangentIndex == y.TangentIndex &&
                   x.UvIndex == y.UvIndex &&
                   x.UvIndex2 == y.UvIndex2 &&
                   x.UvIndex3 == y.UvIndex3 &&
                   x.UvIndex4 == y.UvIndex4 &&
                   x.ColorIndex == y.ColorIndex;
        }

        /// <summary>Returns a hash code for this instance.</summary>
        /// <param name="obj">The Vertex Data used to get the hashcode.</param>
        /// <returns>A hash code for the vertex data, suitable for use in hashing algorithms and data structures like a hash table.</returns>
        public int GetHashCode(VertexDataIndices obj)
        {
            return obj._hashCode;
        }
    }
    
}