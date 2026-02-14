using System.Collections.Generic;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public class FBXBlendShapeSubDeformer : FBXSubDeformer
    {
        public float DeformPercent;

        public IList<float> FullWeights;

        public IBlendShapeKey Geometry;
        
        public FBXBlendShapeSubDeformer(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.BlendShapeSubDeformer;
        }
    }
}