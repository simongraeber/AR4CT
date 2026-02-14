using System.Collections.Generic;

namespace TriLibCore.Fbx
{
    public class FBXBindingTable : FBXObject
    {
        public string TargetName; 
        public string TargetType; 

        public Dictionary<string, string> Entries = new Dictionary<string, string>();

        public FBXBindingTable(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.BindingTable;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }
    }
}