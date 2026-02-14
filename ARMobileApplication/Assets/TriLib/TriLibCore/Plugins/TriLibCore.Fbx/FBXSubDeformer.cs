using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXSubDeformer : FBXObject
    {
        public IList<int> Indexes ;
        
        public Matrix4x4 Transform ;

        public Matrix4x4 TransformLink;

        public IList<float> Weights;

        public FBXModel Model;

        public FBXDeformer BaseDeformer;

        public int WeightsCount;

        //this is a workarond
        public bool transformParsed;

        public FBXSubDeformer(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.SubDeformer;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }
    }
}