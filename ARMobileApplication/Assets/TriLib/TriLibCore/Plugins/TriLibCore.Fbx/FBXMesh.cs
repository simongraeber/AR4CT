using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXMesh : IFBXMesh
    {
        private string _name;
        public FBXMesh()
        {

        }

        public FBXMesh(FBXDocument document, string name, long objectId, string objectClass, bool cloned = false)
        {
            Document = document;
            Name = name;
            Id = objectId;
            Class = objectClass;
            ObjectType = FBXObjectType.Geometry;
            if (!cloned && objectId > -1)
            {
                LoadDefinition();
            }
        }

        public IFBXMesh BaseMesh { get; private set; }
        public Vector3 BBoxMax { get; set; }
        public Vector3 BBoxMin { get; set; }
        public int BlendShapeGeometryBindingsCount { get; set; }
        public bool CastShadows { get; set; }
        public string Class { get; set; }
        public Color Color { get; set; }
        public FBXGeometryProcessor ColorProcessor { get; set; }
        public FBXDocument Document { get; set; }
        public bool HasBlendShapes
        {
            get;
            set;
        }

        public long Id { get; set; }
        public IGeometryGroup InnerGeometryGroup { get; set; }
        public FBXGeometryProcessor MaterialProcessor { get; set; }
        public FBXModel Model { get; set; }
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                if (InnerGeometryGroup != null)
                {
                    InnerGeometryGroup.Name = _name;
                }
            }
        }

        public FBXNode Node { get; set; }
        public FBXGeometryProcessor NormalProcessor { get; set; }
        public FBXObjectType ObjectType { get; set; }

        public bool PrimaryVisibility { get; set; } 

        public bool ReceiveShadows { get; set; } 
        public FBXGeometryProcessor TangentProcessor { get; set; }

        public bool Used { get; set; }

        public FBXGeometryProcessor UVProcessor0 { get; set; }

        public FBXGeometryProcessor UVProcessor1 { get; set; }

        public FBXGeometryProcessor UVProcessor2 { get; set; }

        public FBXGeometryProcessor UVProcessor3 { get; set; }

        public FBXGeometryProcessor VertexProcessor { get; set; }
        public static IFBXMesh CopyFrom(AssetLoaderContext assetLoaderContext, IFBXMesh other)
        {
            var mesh = new FBXMesh();
            mesh.BaseMesh = other;
            mesh.Document = other.Document;
            mesh.ObjectType = other.ObjectType;
            mesh.Class = other.Class;
            mesh.Node = other.Node;
            mesh.Color = other.Color;
            mesh.BBoxMin = other.BBoxMin;
            mesh.BBoxMax = other.BBoxMax;
            mesh.PrimaryVisibility = other.PrimaryVisibility;
            mesh.CastShadows = other.CastShadows;
            mesh.ReceiveShadows = other.ReceiveShadows;
            mesh.BlendShapeGeometryBindingsCount = other.BlendShapeGeometryBindingsCount;
            mesh.VertexProcessor = other.VertexProcessor;
            mesh.NormalProcessor = other.NormalProcessor;
            mesh.ColorProcessor = other.ColorProcessor;
            mesh.UVProcessor0 = other.UVProcessor0;
            mesh.UVProcessor1 = other.UVProcessor1;
            mesh.UVProcessor2 = other.UVProcessor2;
            mesh.UVProcessor3 = other.UVProcessor3;
            mesh.MaterialProcessor = other.MaterialProcessor;
            mesh.HasBlendShapes = other.HasBlendShapes;
            return mesh;
        }

        public void LoadDefinition()
        {
            if (Document.GeometryGroupDefinition != null)
            {
                Color = Document.GeometryGroupDefinition.Color;
                BBoxMin = Document.GeometryGroupDefinition.BBoxMin;
                BBoxMax = Document.GeometryGroupDefinition.BBoxMax;
                PrimaryVisibility = Document.GeometryGroupDefinition.PrimaryVisibility;
                CastShadows = Document.GeometryGroupDefinition.CastShadows;
                ReceiveShadows = Document.GeometryGroupDefinition.ReceiveShadows;
            }
        }
    }
}