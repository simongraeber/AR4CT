using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public interface IFBXObject : IObject
    {
        FBXDocument Document {get; set; }

        long Id { get; set; }

        FBXObjectType ObjectType { get; set; }

        string Class { get; set; }

        void LoadDefinition();
    }

    public class FBXObject : IFBXObject
    {
        public string Name { get; set; }

        public bool Used { get; set; }

        public FBXDocument Document { get; set; }

        public long Id { get; set; }

        public FBXObjectType ObjectType { get; set; }

        public string Class { get; set; }

        public FBXProperties Properties { get; set; }


        protected FBXObject()
        {

        }
        
        protected FBXObject(FBXDocument document, string name, long objectId, string objectClass)
        {
            Document = document;
            Name = name;
            Id = objectId;
            Class = objectClass;
        }
        
        public virtual void LoadDefinition()
        {
            
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
