using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public interface IFBXMeshBase : IFBXObject
    {
        FBXNode Node { get; set; }
    }

    public interface IFBXMesh : IFBXMeshBase
    {
        IFBXMesh BaseMesh { get; }

        IGeometryGroup InnerGeometryGroup { get; set; }

        Color Color { get; set; } 

        Vector3 BBoxMin { get; set; }  

        Vector3 BBoxMax { get; set; }  

        bool PrimaryVisibility { get; set; } 

        bool CastShadows { get; set; } 

        bool ReceiveShadows { get; set; } 

        int BlendShapeGeometryBindingsCount { get; set; }

        FBXModel Model { get; set; }

        FBXGeometryProcessor VertexProcessor { get; set; }
        FBXGeometryProcessor NormalProcessor { get; set; }
        FBXGeometryProcessor TangentProcessor { get; set; }
        FBXGeometryProcessor ColorProcessor { get; set; }
        FBXGeometryProcessor UVProcessor0 { get; set; }
        FBXGeometryProcessor UVProcessor1 { get; set; }
        FBXGeometryProcessor UVProcessor2 { get; set; }
        FBXGeometryProcessor UVProcessor3 { get; set; }
        FBXGeometryProcessor MaterialProcessor { get; set; }
        bool HasBlendShapes { get; set; }
    }
}
