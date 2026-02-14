namespace TriLibCore.Fbx
{
    public class FBXImplementation : FBXObject
    {
        public string ShaderLanguage; 
        public int ShaderLanguageVersion; 
        public string RenderAPI; 
        public string RootBindingName; 

        public FBXBindingTable BindingTable;

        public FBXImplementation(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Implementation;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }
    }
}
