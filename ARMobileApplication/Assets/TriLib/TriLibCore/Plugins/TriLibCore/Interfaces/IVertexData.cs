using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Geometries
{
    /// <summary>
    /// Represents a Vertex Data.
    /// A Vertex Data contains all attributes a Unity Vertex can have.
    /// </summary>
    public interface IVertexData
    {
        /// <summary>
        /// Gets/Sets the original Vertex Index.
        /// </summary>
        void SetVertexIndex(int value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the original Vertex Index.
        /// </summary>
        int GetVertexIndex(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Position.
        /// </summary>
        void SetPosition(Vector3 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Position.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector3 GetPosition(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Normal.
        /// </summary>
        void SetNormal(Vector3 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Normal.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector3 GetNormal(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Tangent.
        /// </summary>
        void SetTangent(Vector4 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Tangent.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector4 GetTangent(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Color.
        /// </summary>
        void SetColor(Color value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Color.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Color GetColor(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 1.
        /// </summary>
        void SetUV1(Vector2 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 1.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector2 GetUV1(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 2.
        /// </summary>
        void SetUV2(Vector2 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 2.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector2 GetUV2(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 3.
        /// </summary>
        void SetUV3(Vector2 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 3.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector2 GetUV3(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 4.
        /// </summary>
        void SetUV4(Vector2 value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex Texture Coordinate 4.
        /// </summary>
        /// <param name="geometryGroup"></param>
        Vector2 GetUV4(IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex processed Bone Weight.
        /// </summary>
        void SetBoneWeight(BoneWeight value, IGeometryGroup geometryGroup);

        /// <summary>
        /// Gets/Sets the Vertex processed Bone Weight.
        /// </summary>
        /// <param name="geometryGroup"></param>
        BoneWeight GetBoneWeight(IGeometryGroup geometryGroup);

        /// <summary>
        /// Indicates whether this Vertex Data uses Bone Weights.
        /// </summary>
        /// <param name="geometryGroup"></param>
        bool GetUsesBoneWeight(IGeometryGroup geometryGroup);
    }
    
}