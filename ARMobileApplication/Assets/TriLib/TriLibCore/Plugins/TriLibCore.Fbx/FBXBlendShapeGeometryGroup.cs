using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXBlendShapeGeometryGroup : IFBXMeshBase, IBlendShapeKey
    {
        public Dictionary<int, int> IndexMap { get; set; }
        public List<Vector3> Vertices { get; set; }
        public List<Vector3> Normals { get; set; }
        public List<Vector3> Tangents { get; set; }

        public float FrameWeight { get; set; }
        public bool FullGeometryShape { get; set; }

        public bool Processed;
        
        public FBXBlendShapeGeometryGroup(FBXDocument document, string name, long objectId, string objectClass)
        {
            Document = document;
            Name = name;
            Id = objectId;
            Class = objectClass;
            ObjectType = FBXObjectType.BlendShapeGeometry;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public string Name { get; set; }
        public bool Used { get; set; }
        public FBXDocument Document { get; set; }
        public long Id { get; set; }
        public FBXObjectType ObjectType { get; set; }
        public string Class { get; set; }
        public FBXNode Node { get; set; }

        public void LoadDefinition()
        {
            
        }

        public void ReleaseTemporary()
        {
            
        }
    }
}