using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Dae
{
    public struct DaeVector : IVertexData
    {
        public int VertexIndex;
        public int NormalIndex;
        public int TexCoordIndex;
        public int OriginalVertexIndex;
        public DaeGeometryGroup GeometryGroup;

        public void SetVertexIndex(int value, IGeometryGroup geometryGroup)
        {

        }

        public int GetVertexIndex(IGeometryGroup geometryGroup)
        {
            return OriginalVertexIndex;
        }

        public void SetPosition(Vector3 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector3 GetPosition(IGeometryGroup geometryGroup)
        {
            var finalIndex = GeometryGroup.Primitives[VertexIndex];
            return ListUtils.FixIndex(finalIndex, GeometryGroup.VerticesList);
        }

        public void SetNormal(Vector3 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector3 GetNormal(IGeometryGroup geometryGroup)
        {
            var finalIndex = GeometryGroup.Primitives[NormalIndex];
            return ListUtils.FixIndex(finalIndex, GeometryGroup.NormalsList);
        }

        public void SetTangent(Vector4 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector4 GetTangent(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public void SetColor(Color value, IGeometryGroup geometryGroup)
        {

        }

        public Color GetColor(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public void SetUV1(Vector2 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector2 GetUV1(IGeometryGroup geometryGroup)
        {
            var finalIndex = GeometryGroup.Primitives[TexCoordIndex];
            return ListUtils.FixIndex(finalIndex, GeometryGroup.TexCoordsList);
        }

        public void SetUV2(Vector2 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector2 GetUV2(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public void SetUV3(Vector2 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector2 GetUV3(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public void SetUV4(Vector2 value, IGeometryGroup geometryGroup)
        {

        }

        public Vector2 GetUV4(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public void SetBoneWeight(BoneWeight value, IGeometryGroup geometryGroup)
        {

        }

        public BoneWeight GetBoneWeight(IGeometryGroup geometryGroup)
        {
            return default;
        }

        public bool GetUsesBoneWeight(IGeometryGroup geometryGroup)
        {
            return default;
        }
    }
}