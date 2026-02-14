using System.IO;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Fbx
{
    public enum FBXTextureBlendMode
    {
        eTranslucent, eAdditive, eModulate, eModulate2,
        eOver, eNormal, eDissolve, eDarken,
        eColorBurn, eLinearBurn, eDarkerColor, eLighten,
        eScreen, eColorDodge, eLinearDodge, eLighterColor,
        eSoftLight, eHardLight, eVividLight, eLinearLight,
        ePinLight, eHardMix, eDifference, eExclusion,
        eSubtract, eDivide, eHue, eSaturation,
        eColor, eLuminosity, eOverlay, eBlendModeCount
    }

    public class FBXTexture : FBXObject, ITexture
    {
        public const int TextureCapacity = 128;

        public FBXTextureUse6 TextureTypeUse; 
        public float TextureAlpha; 
        public FBXUnifiedMappingType CurrentMappingType; 
        public FBXWrapMode TextureWrapModeU; 
        public FBXWrapMode TextureWrapModeV; 
        public bool UVSwap; 
        public bool PremultiplyAlpha; 
        public Vector3? Translation; 
        public Vector3? Rotation; 
        public Vector3? Scaling; 
        public Vector3 TextureRotationPivot; 
        public Vector3 TextureScalingPivot; 
        public FBXBlendMode CurrentTextureBlendMode; 
        public string UVSet; 
        public bool UseMaterial; 
        public bool UseMipMap; 
        public FBXAlphaSource AlphaSource; 
        public Vector4 Cropping; 

        public FBXVideo Video;
        public string TextureName;
        public string Media;
        public string FullFilename;
        public string RelativeFilename;
        public Vector2? ModelUVTranslation;
        public Vector2? ModelUVScaling;
        public string Texture_Alpha_Source;
        public string Type;
        private string _resolvedFilename;

        public virtual int GetSubTextureCount()
        {
            return 0;
        }

        public virtual float GetWeight(int index)
        {
            return 1f;
        }

        public virtual ITexture GetSubTexture(int index)
        {
            return this;
        }

        public virtual void AddTexture(ITexture texture)
        {

        }

        public byte[] Data
        {
            get => Video?.Content;
            set
            {

            }
        }

        public Stream DataStream
        {
            get;
            set;
        }

        public string Filename
        {
            get => RelativeFilename ?? FullFilename;
            set
            {

            }
        }

        public TextureWrapMode WrapModeU
        {
            get => TextureWrapModeU == FBXWrapMode.eClamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            set
            {

            }
        }

        public TextureWrapMode WrapModeV
        {
            get => TextureWrapModeV == FBXWrapMode.eClamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            set
            {

            }
        }

        public Vector2 Tiling
        {
            get => Scaling??ModelUVScaling??Vector2.one;
            set
            {

            }
        }

        public Vector2 Offset
        {
            get => Translation??ModelUVTranslation??Vector2.zero;
            set
            {

            }
        }

        public int TextureId
        {
            get;
            set;
        }

        public string ResolvedFilename
        {
            get => Video?.ResolvedFilename ?? _resolvedFilename;
            set => _resolvedFilename = value;
        }

        public bool IsValid => !string.IsNullOrEmpty(Filename) || Video?.ContentStream != null && Video.ContentStream.Length > 0;
        public bool HasAlpha { get; set; }
        public TextureFormat TextureFormat { get; set; }


        public FBXTexture(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Texture;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.TextureDefinition != null)
            {
                TextureTypeUse = Document.TextureDefinition.TextureTypeUse;
                TextureAlpha = Document.TextureDefinition.TextureAlpha;
                CurrentMappingType = Document.TextureDefinition.CurrentMappingType;
                TextureWrapModeU = Document.TextureDefinition.TextureWrapModeU;
                TextureWrapModeV = Document.TextureDefinition.TextureWrapModeV;
                UVSwap = Document.TextureDefinition.UVSwap;
                PremultiplyAlpha = Document.TextureDefinition.PremultiplyAlpha;
                Translation = Document.TextureDefinition.Translation;
                Rotation = Document.TextureDefinition.Rotation;
                Scaling = Document.TextureDefinition.Scaling;
                TextureRotationPivot = Document.TextureDefinition.TextureRotationPivot;
                TextureScalingPivot = Document.TextureDefinition.TextureScalingPivot;
                CurrentTextureBlendMode = Document.TextureDefinition.CurrentTextureBlendMode;
                UVSet = Document.TextureDefinition.UVSet;
                UseMaterial = Document.TextureDefinition.UseMaterial;
                UseMipMap = Document.TextureDefinition.UseMipMap;
                AlphaSource = Document.TextureDefinition.AlphaSource;
                Cropping = Document.TextureDefinition.Cropping;
            }
        }

        public bool Equals(ITexture other)
        {
            return TextureComparators.TextureEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            return TextureComparators.Equals(this, obj);
        }

        public override int GetHashCode()
        {
            return TextureComparators.GetHashCode(this);
        }
    }
}