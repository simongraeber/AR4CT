using System.Collections.Generic;

namespace TriLibCore.Fbx
{
    public class FBXAnimationCurveNode : FBXObject
    {
        public float DX;

        public float DY;

        public float DZ;

        public float DeformPercent;

        public float Visibility;

        public FBXAnimationCurveModelBinding AnimationCurveModelBinding;

        public FBXAnimationCurveGeometryBinding AnimationCurveGeometryBinding;

        public FBXAnimationCurveBlendShapeBinding AnimationCurveBlendShapeBinding;

        public FBXAnimationCurveMaterialBinding AnimationCurveMaterialBinding;

        public FBXAnimationLayer AnimationLayer;

        public IList<FBXAnimationCurve> AnimationCurves;
        public int AnimationCurvesCount;

        public FBXAnimationCurveNode(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.AnimationCurveNode;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.AnimationCurveNodeDefinition != null)
            {
                DX = Document.AnimationCurveNodeDefinition.DX;
                DY = Document.AnimationCurveNodeDefinition.DY;
                DZ = Document.AnimationCurveNodeDefinition.DZ;
                DeformPercent = Document.AnimationCurveNodeDefinition.DeformPercent;
                Visibility = Document.AnimationCurveNodeDefinition.Visibility;
            }
        }
    }
}
