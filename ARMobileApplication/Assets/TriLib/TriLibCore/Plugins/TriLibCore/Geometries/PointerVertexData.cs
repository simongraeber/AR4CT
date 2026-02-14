using System;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Represents a lightweight <see cref="IVertexData"/> implementation that points into a
    /// <see cref="CommonGeometryGroup"/> vertex attribute storage.
    /// </summary>
    /// <remarks>
    /// This type is primarily used during vertex merging. When <see cref="Added"/> is <c>false</c>,
    /// getters read from the geometry group's temporary staging fields (Temp*). After a vertex is
    /// committed and <see cref="VertexDataIndex"/> becomes valid, getters/setters operate directly
    /// on the underlying lists stored in the <see cref="CommonGeometryGroup"/>.
    /// </remarks>
    public struct PointerVertexData : IVertexData, IEquatable<PointerVertexData>
    {
        private readonly CommonGeometryGroup _commonGeometryGroup;

        /// <summary>
        /// Gets a value indicating whether this instance points to a committed vertex entry.
        /// </summary>
        public bool Added => VertexDataIndex > -1;

        /// <summary>
        /// Gets or sets the vertex data index into the owning <see cref="CommonGeometryGroup"/> lists.
        /// A value of <c>-1</c> indicates that this instance references the temporary staging data.
        /// </summary>
        public int VertexDataIndex;

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="PointerVertexData"/> instance
        /// by comparing only the attributes that are enabled in the owning geometry group.
        /// </summary>
        /// <param name="other">The other instance to compare.</param>
        /// <returns>
        /// <c>true</c> if the enabled attributes match; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(PointerVertexData other)
        {
            var position = GetPosition(_commonGeometryGroup);
            var result = position.Equals(other.GetPosition(_commonGeometryGroup));

            if (_commonGeometryGroup.HasNormals)
            {
                var normal = GetNormal(_commonGeometryGroup);
                result &= normal.Equals(other.GetNormal(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasTangents)
            {
                var tangent = GetTangent(_commonGeometryGroup);
                result &= tangent.Equals(other.GetTangent(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasColors)
            {
                var color = GetColor(_commonGeometryGroup);
                result &= color.Equals(other.GetColor(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasUv1)
            {
                var uv1 = GetUV1(_commonGeometryGroup);
                result &= uv1.Equals(other.GetUV1(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasUv2)
            {
                var uv2 = GetUV2(_commonGeometryGroup);
                result &= uv2.Equals(other.GetUV2(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasUv3)
            {
                var uv3 = GetUV3(_commonGeometryGroup);
                result &= uv3.Equals(other.GetUV3(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasUv4)
            {
                var uv4 = GetUV4(_commonGeometryGroup);
                result &= uv4.Equals(other.GetUV4(_commonGeometryGroup));
            }

            if (_commonGeometryGroup.HasSkin)
            {
                var thisIndex = GetVertexIndex(_commonGeometryGroup);
                var otherIndex = other.GetVertexIndex(_commonGeometryGroup);
                result &= thisIndex == otherIndex;
            }

            return result;
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>
        /// <c>true</c> if <paramref name="obj"/> is a <see cref="PointerVertexData"/> and is equal;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            return obj is PointerVertexData pointerVertexData && Equals(pointerVertexData);
        }

        /// <summary>
        /// Returns a hash code based on the enabled attributes in the owning geometry group.
        /// </summary>
        /// <returns>The computed hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GetPosition(_commonGeometryGroup).GetHashCode();

                if (_commonGeometryGroup.HasNormals)
                {
                    hashCode = (hashCode * 397) ^ GetNormal(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasTangents)
                {
                    hashCode = (hashCode * 397) ^ GetTangent(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasColors)
                {
                    hashCode = (hashCode * 397) ^ GetColor(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasUv1)
                {
                    hashCode = (hashCode * 397) ^ GetUV1(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasUv2)
                {
                    hashCode = (hashCode * 397) ^ GetUV2(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasUv3)
                {
                    hashCode = (hashCode * 397) ^ GetUV3(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasUv4)
                {
                    hashCode = (hashCode * 397) ^ GetUV4(_commonGeometryGroup).GetHashCode();
                }

                if (_commonGeometryGroup.HasSkin)
                {
                    hashCode = (hashCode * 397) ^ GetVertexIndex(_commonGeometryGroup).GetHashCode();
                }

                return hashCode;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PointerVertexData"/> struct.
        /// </summary>
        /// <param name="commonGeometryGroup">The owning geometry group that stores the vertex attribute lists.</param>
        /// <param name="vertexDataIndex">
        /// The vertex data index into the group lists. Use <c>-1</c> to indicate staged (temporary) data.
        /// </param>
        public PointerVertexData(CommonGeometryGroup commonGeometryGroup, int vertexDataIndex)
        {
            _commonGeometryGroup = commonGeometryGroup;
            VertexDataIndex = vertexDataIndex;
        }

        /// <summary>
        /// Sets the original vertex index.
        /// </summary>
        /// <param name="value">The original vertex index.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetVertexIndex(int value, IGeometryGroup geometryGroup)
        {
            if (Added)
            {
                _commonGeometryGroup.OriginalVertexIndices[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the original vertex index.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The original vertex index.</returns>
        public int GetVertexIndex(IGeometryGroup geometryGroup)
        {
            return Added ? _commonGeometryGroup.OriginalVertexIndices[VertexDataIndex] : _commonGeometryGroup.TempOriginalVertexIndex;
        }

        /// <summary>
        /// Sets the vertex position.
        /// </summary>
        /// <param name="value">The position value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetPosition(Vector3 value, IGeometryGroup geometryGroup)
        {
            if (Added)
            {
                _commonGeometryGroup.Positions[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the vertex position.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex position.</returns>
        public Vector3 GetPosition(IGeometryGroup geometryGroup)
        {
            return Added ? _commonGeometryGroup.Positions[VertexDataIndex] : _commonGeometryGroup.TempPosition;
        }

        /// <summary>
        /// Sets the vertex normal.
        /// </summary>
        /// <param name="value">The normal value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetNormal(Vector3 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasNormals)
            {
                _commonGeometryGroup.Normals[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the vertex normal.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex normal, or <c>default</c> when normals are not enabled.</returns>
        public Vector3 GetNormal(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasNormals ? _commonGeometryGroup.Normals[VertexDataIndex] : default)
                : _commonGeometryGroup.TempNormal;
        }

        /// <summary>
        /// Sets the vertex tangent.
        /// </summary>
        /// <param name="value">The tangent value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetTangent(Vector4 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasTangents)
            {
                _commonGeometryGroup.Tangents[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the vertex tangent.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex tangent, or <c>default</c> when tangents are not enabled.</returns>
        public Vector4 GetTangent(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasTangents ? _commonGeometryGroup.Tangents[VertexDataIndex] : default)
                : _commonGeometryGroup.TempTangent;
        }

        /// <summary>
        /// Sets the vertex color.
        /// </summary>
        /// <param name="value">The color value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetColor(Color value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasColors)
            {
                _commonGeometryGroup.Colors[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the vertex color.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The vertex color, or <c>default</c> when colors are not enabled.</returns>
        public Color GetColor(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasColors ? _commonGeometryGroup.Colors[VertexDataIndex] : default)
                : _commonGeometryGroup.TempColor;
        }

        /// <summary>
        /// Sets the UV0 value (first UV channel).
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV1(Vector2 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasUv1)
            {
                _commonGeometryGroup.UVs1[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the UV0 value (first UV channel).
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value, or <c>default</c> when UV0 is not enabled.</returns>
        public Vector2 GetUV1(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasUv1 ? _commonGeometryGroup.UVs1[VertexDataIndex] : default)
                : _commonGeometryGroup.TempUV1;
        }

        /// <summary>
        /// Sets the UV1 value (second UV channel).
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV2(Vector2 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasUv2)
            {
                _commonGeometryGroup.UVs2[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the UV1 value (second UV channel).
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value, or <c>default</c> when UV1 is not enabled.</returns>
        public Vector2 GetUV2(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasUv2 ? _commonGeometryGroup.UVs2[VertexDataIndex] : default)
                : _commonGeometryGroup.TempUV2;
        }

        /// <summary>
        /// Sets the UV2 value (third UV channel).
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV3(Vector2 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasUv3)
            {
                _commonGeometryGroup.UVs3[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the UV2 value (third UV channel).
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value, or <c>default</c> when UV2 is not enabled.</returns>
        public Vector2 GetUV3(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasUv3 ? _commonGeometryGroup.UVs3[VertexDataIndex] : default)
                : _commonGeometryGroup.TempUV3;
        }

        /// <summary>
        /// Sets the UV3 value (fourth UV channel).
        /// </summary>
        /// <param name="value">The UV value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetUV4(Vector2 value, IGeometryGroup geometryGroup)
        {
            if (Added && _commonGeometryGroup.HasUv4)
            {
                _commonGeometryGroup.UVs4[VertexDataIndex] = value;
            }
        }

        /// <summary>
        /// Gets the UV3 value (fourth UV channel).
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The UV value, or <c>default</c> when UV3 is not enabled.</returns>
        public Vector2 GetUV4(IGeometryGroup geometryGroup)
        {
            return Added
                ? (_commonGeometryGroup.HasUv4 ? _commonGeometryGroup.UVs4[VertexDataIndex] : default)
                : _commonGeometryGroup.TempUV4;
        }

        /// <summary>
        /// Sets the vertex bone weight.
        /// </summary>
        /// <remarks>
        /// This method is currently not implemented for <see cref="PointerVertexData"/>.
        /// </remarks>
        /// <param name="value">The bone weight value.</param>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        public void SetBoneWeight(BoneWeight value, IGeometryGroup geometryGroup)
        {
            //todo: not needed to implement now, but maybe on a future usage
        }

        /// <summary>
        /// Gets the vertex bone weight.
        /// </summary>
        /// <remarks>
        /// This method is currently not implemented for <see cref="PointerVertexData"/> and returns <c>default</c>.
        /// </remarks>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>The bone weight value, or <c>default</c> when not implemented.</returns>
        public BoneWeight GetBoneWeight(IGeometryGroup geometryGroup)
        {
            //todo: not needed to implement now, but maybe on a future usage
            return default;
        }

        /// <summary>
        /// Gets a value indicating whether this vertex data instance uses bone weights.
        /// </summary>
        /// <param name="geometryGroup">The geometry group requesting the operation.</param>
        /// <returns>
        /// <c>true</c> if the owning geometry group has skinning enabled; otherwise, <c>false</c>.
        /// </returns>
        public bool GetUsesBoneWeight(IGeometryGroup geometryGroup)
        {
            return _commonGeometryGroup.HasSkin;
        }
    }
}
