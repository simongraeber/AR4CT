using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public interface IFBXGeometry : IGeometry
    {
        void AddVertex(AssetLoaderContext assetLoaderContext, int originalVertexIndex, Vector3 position, Vector3 normal, Vector4 tangent, Color color, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3);
        int IndicesCount { get; set; }
    }


    public class FBXGeometry : CommonGeometry, IFBXObject, IFBXGeometry
    {
        public string Name { get; set; }

        public bool Used { get; set; }

        public FBXDocument Document { get; set; }

        public long Id { get; set; }

        public FBXObjectType ObjectType { get; set; }

        public string Class { get; set; }

        public void LoadDefinition()
        {

        }

        public void ReleaseTemporary()
        {

        }

        public void AddVertex(AssetLoaderContext assetLoaderContext, int originalVertexIndex, Vector3 position, Vector3 normal, Vector4 tangent, Color color, Vector2 uv0, Vector2 uv1, Vector2 uv2, Vector2 uv3)
        {
            base.AddVertex(assetLoaderContext: assetLoaderContext,
                originalVertexIndex: originalVertexIndex,
                position: position,
                normal: normal,
                tangent: tangent,
                color: color,
                uv0: uv0,
                uv1: uv1,
                uv2: uv2, 
                uv3: uv3);
        }

        public int IndicesCount { get; set; }
    }
}
