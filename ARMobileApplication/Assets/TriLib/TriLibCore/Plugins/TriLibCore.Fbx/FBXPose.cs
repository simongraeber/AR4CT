using System.Collections.Generic;

namespace TriLibCore.Fbx
{
    public class FBXPose : FBXObject
    {
        public int NbPoseNodes ;

        public Dictionary<long, FBXPoseNode> PoseNodes ;

        public FBXPose(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Pose;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.PoseDefinition != null)
            {
                NbPoseNodes = Document.PoseDefinition.NbPoseNodes;
            }
        }
    }
}