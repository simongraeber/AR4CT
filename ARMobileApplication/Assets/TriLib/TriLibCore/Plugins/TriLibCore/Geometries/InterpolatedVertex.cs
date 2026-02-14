using System;
using System.Collections.Generic;
using TriLibCore.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Represents an <see cref="IVertexData"/> implementation whose attributes are stored locally,
    /// typically used to build interpolated vertex data (for example, during tessellation or
    /// geometry processing stages).
    /// </summary>
    public class InterpolatedVertex : IVertexData
    {
        private Vector3 _position;
        private Vector3 _normal;
        private Vector4 _tangent;
        private Color _color;
        private Vector2 _uv0;
        private Vector2 _uv1;
        private Vector2 _uv2;
        private Vector2 _uv3;
        private BoneWeight _boneWeight;
        private int _vertexIndex;

        /// <summary>
        /// Initializes a new instance of the <see cref="InterpolatedVertex"/> class using the given position.
        /// </summary>
        /// <param name="position">The initial vertex position.</param>
        public InterpolatedVertex(Vector3 position)
        {
            _position = position;
        }

        /// <summary>
        /// Sets the vertex index associated with this vertex data instance.
        /// </summary>
        /// <param name="value">The vertex index.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetVertexIndex(int value, IGeometryGroup geometryGroup)
        {
            _vertexIndex = value;
        }

        /// <summary>
        /// Gets the vertex index associated with this vertex data instance.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex index.</returns>
        public int GetVertexIndex(IGeometryGroup geometryGroup)
        {
            return _vertexIndex;
        }

        /// <summary>
        /// Sets the vertex position.
        /// </summary>
        /// <param name="value">The position value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetPosition(Vector3 value, IGeometryGroup geometryGroup)
        {
            _position = value;
        }

        /// <summary>
        /// Gets the vertex position.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex position.</returns>
        public Vector3 GetPosition(IGeometryGroup geometryGroup)
        {
            return _position;
        }

        /// <summary>
        /// Sets the vertex normal.
        /// </summary>
        /// <param name="value">The normal value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetNormal(Vector3 value, IGeometryGroup geometryGroup)
        {
            _normal = value;
        }

        /// <summary>
        /// Gets the vertex normal.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex normal.</returns>
        public Vector3 GetNormal(IGeometryGroup geometryGroup)
        {
            return _normal;
        }

        /// <summary>
        /// Sets the vertex tangent.
        /// </summary>
        /// <param name="value">The tangent value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetTangent(Vector4 value, IGeometryGroup geometryGroup)
        {
            _tangent = value;
        }

        /// <summary>
        /// Gets the vertex tangent.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex tangent.</returns>
        public Vector4 GetTangent(IGeometryGroup geometryGroup)
        {
            return _tangent;
        }

        /// <summary>
        /// Sets the vertex color.
        /// </summary>
        /// <param name="value">The color value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetColor(Color value, IGeometryGroup geometryGroup)
        {
            _color = value;
        }

        /// <summary>
        /// Gets the vertex color.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex color.</returns>
        public Color GetColor(IGeometryGroup geometryGroup)
        {
            return _color;
        }

        /// <summary>
        /// Sets the UV0 (first UV channel) value.
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV1(Vector2 value, IGeometryGroup geometryGroup)
        {
            _uv0 = value;
        }

        /// <summary>
        /// Gets the UV0 (first UV channel) value.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value.</returns>
        public Vector2 GetUV1(IGeometryGroup geometryGroup)
        {
            return _uv0;
        }

        /// <summary>
        /// Sets the UV1 (second UV channel) value.
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV2(Vector2 value, IGeometryGroup geometryGroup)
        {
            _uv1 = value;
        }

        /// <summary>
        /// Gets the UV1 (second UV channel) value.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value.</returns>
        public Vector2 GetUV2(IGeometryGroup geometryGroup)
        {
            return _uv1;
        }

        /// <summary>
        /// Sets the UV2 (third UV channel) value.
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV3(Vector2 value, IGeometryGroup geometryGroup)
        {
            _uv2 = value;
        }

        /// <summary>
        /// Gets the UV2 (third UV channel) value.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value.</returns>
        public Vector2 GetUV3(IGeometryGroup geometryGroup)
        {
            return _uv2;
        }

        /// <summary>
        /// Sets the UV3 (fourth UV channel) value.
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV4(Vector2 value, IGeometryGroup geometryGroup)
        {
            _uv3 = value;
        }

        /// <summary>
        /// Gets the UV3 (fourth UV channel) value.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value.</returns>
        public Vector2 GetUV4(IGeometryGroup geometryGroup)
        {
            return _uv3;
        }

        /// <summary>
        /// Sets the vertex bone weights.
        /// </summary>
        /// <param name="value">The bone weight value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetBoneWeight(BoneWeight value, IGeometryGroup geometryGroup)
        {
            _boneWeight = value;
        }

        /// <summary>
        /// Gets the vertex bone weights.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The bone weight value.</returns>
        public BoneWeight GetBoneWeight(IGeometryGroup geometryGroup)
        {
            return _boneWeight;
        }

        /// <summary>
        /// Gets a value indicating whether this vertex data instance uses bone weights.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns><c>true</c> if bone weights are used; otherwise, <c>false</c>.</returns>
        public bool GetUsesBoneWeight(IGeometryGroup geometryGroup)
        {
            return true;
        }

        /// <summary>
        /// Determines whether the specified geometry group contains skinning data relevant to this vertex.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>
        /// A value indicating whether skinning data is present.
        /// </returns>
        /// <exception cref="NotImplementedException">
        /// Thrown because this method is not implemented in this type.
        /// </exception>
        public bool HasSkin(IGeometryGroup geometryGroup)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Fills the provided list with byte spans describing the vertex data layout in a stream.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <param name="spans">The list to be filled with (start, end) span tuples.</param>
        /// <exception cref="NotImplementedException">
        /// Thrown because this method is not implemented in this type.
        /// </exception>
        public void GetStreamSpans(IGeometryGroup geometryGroup, IList<Tuple<int, int>> spans)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Copies a span of the vertex byte stream into the provided native array.
        /// </summary>
        /// <param name="data">The destination byte array.</param>
        /// <param name="spanStart">The inclusive start offset of the span.</param>
        /// <param name="spanEnd">The exclusive end offset of the span.</param>
        /// <exception cref="NotImplementedException">
        /// Thrown because this method is not implemented in this type.
        /// </exception>
        public void CopySpanToNativeArray(ref NativeArray<byte> data, int spanStart, int spanEnd)
        {
            throw new NotImplementedException();
        }
    }
}
