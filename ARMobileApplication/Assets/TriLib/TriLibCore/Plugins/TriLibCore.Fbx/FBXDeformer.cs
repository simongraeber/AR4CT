namespace TriLibCore.Fbx
{
    public class FBXDeformer : FBXObject
    {
        public float Link_DeformAcuracy ;

        public IFBXMesh Geometry;

        public FBXDeformer(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Deformer;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.DeformerDefinition != null)
            {
                Link_DeformAcuracy = Document.DeformerDefinition.Link_DeformAcuracy;
            }
        }
    }
}