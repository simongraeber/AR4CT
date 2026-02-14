using System.Collections.Generic;
using TriLibCore.Collections;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public class FBXLayeredTexture : FBXTexture
    {
        public FBXTextureBlendMode BlendModes;

        public IList<float> Weights;

        public IList<FBXTexture> Textures;

        public FBXLayeredTexture(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.LayeredTexture;
            Textures = new List<FBXTexture>(TextureCapacity);//todo: connections
        }

        public override void AddTexture(ITexture sourceTexture)
        {
            Textures.Add((FBXTexture)sourceTexture);
        }

        public override float GetWeight(int index)
        {
            return Weights[index];
        }

        public override ITexture GetSubTexture(int index)
        {
            return Textures[index];
        }

        public override int GetSubTextureCount()
        {
            return Textures.Count;
        }
    }
}