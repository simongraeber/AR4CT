using TriLibCore.Textures;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _BlendModes_token = -3838216541220309212;
        private const long _TextureTypeUse_token = 7637847229842731415;
        private const long _Texture_alpha_token = 2626607208962590260;
        private const long _CurrentMappingType_token = 241407700457973428;
        private const long _WrapModeU_token = -4289189187634787709;
        private const long _WrapModeV_token = -4289189187634787708;
        private const long _UVSwap_token = -1367968408260896775;
        private const long _PremultiplyAlpha_token = -5973428029444777508;
        private const long _Translation_token = -8289339786050197108;
        private const long _Rotation_token = -4898811164768878973;
        private const long _Scaling_token = -5513532510058915870;
        private const long _TextureRotationPivot_token = -7366345155921651314;
        private const long _TextureScalingPivot_token = -3197825671523237487;
        private const long _CurrentTextureBlendMode_token = 8900201661626535085;
        private const long _UVSet_token = 7096547112137216316;
        private const long _UseMaterial_token = -8288491230235570519;
        private const long _UseMipMap_token = -4289190863330387862;
        private const long _AlphaSource_token = -8305058749623747084;
        private const long _Cropping_token = -4898811574924991737;
        private const long _TextureName_token = -8289663717619403487;
        private const long _Media_token = 7096547112130291455;
        private const long _Filename_token = -4898811500470796020;
        private const long _RelativeFilename_token = 1746164594034540280;
        private const long _ModelUVTranslation_token = -6528792456810533492;
        private const long _ModelUVScaling_token = -8355123491603178526;
        private const long _Texture_Alpha_Source_token = 1698310194394011269;

        private FBXLayeredTexture ProcessLayeredTexture(FBXNode node, long objectId, string name, string objectClass)
        {
            var texture = new FBXLayeredTexture(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AllTextures.Add(texture);
            }
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties!= null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _BlendModes_token:
                                {
                                    texture.BlendModes = (FBXTextureBlendMode)property.Properties.GetIntValue(4);
                                    break;
                                }
                            case _LayeredTexture_token:
                                {
                                    texture.Weights = property.Properties.GetFloatValues();
                                    break;
                                }
                        }
                    }
                }
            }
            return (FBXLayeredTexture)ProcessTextureSubType(node, objectId, texture);
        }

        private FBXTexture ProcessTexture(FBXNode node, long objectId, string name, string objectClass)
        {
            var texture = new FBXTexture(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AllTextures.Add(texture);
            }
            return ProcessTextureSubType(node, objectId, texture);
        }

        private FBXTexture ProcessTextureSubType(FBXNode node, long objectId, FBXTexture texture)
        {
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties!= null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _TextureTypeUse_token:
                                texture.TextureTypeUse = (FBXTextureUse6)property.Properties.GetIntValue(4);
                                break;
                            case _Texture_alpha_token:
                                texture.TextureAlpha = property.Properties.GetFloatValue(4);
                                break;
                            case _CurrentMappingType_token:
                                texture.CurrentMappingType = (FBXUnifiedMappingType)property.Properties.GetIntValue(4);
                                break;
                            case _WrapModeU_token:
                                texture.TextureWrapModeU = (FBXWrapMode)property.Properties.GetIntValue(4);
                                break;
                            case _WrapModeV_token:
                                texture.TextureWrapModeV = (FBXWrapMode)property.Properties.GetIntValue(4);
                                break;
                            case _UVSwap_token:
                                texture.UVSwap = property.Properties.GetBoolValue(4);
                                break;
                            case _PremultiplyAlpha_token:
                                texture.PremultiplyAlpha = property.Properties.GetBoolValue(4);
                                break;
                            case _Translation_token:
                                texture.Translation = property.Properties.GetVector3Value(4);
                                break;
                            case _Rotation_token:
                                texture.Rotation = property.Properties.GetVector3Value(4);
                                break;
                            case _Scaling_token:
                                texture.Scaling = property.Properties.GetVector3Value(4);
                                break;
                            case _TextureRotationPivot_token:
                                texture.TextureRotationPivot = property.Properties.GetVector3Value(4);
                                break;
                            case _TextureScalingPivot_token:
                                texture.TextureScalingPivot = property.Properties.GetVector3Value(4);
                                break;
                            case _CurrentTextureBlendMode_token:
                                texture.CurrentTextureBlendMode = (FBXBlendMode)property.Properties.GetIntValue(4);
                                break;
                            case _UVSet_token:
                                texture.UVSet = property.Properties.GetStringValue(4, false);
                                break;
                            case _UseMaterial_token:
                                texture.UseMaterial = property.Properties.GetBoolValue(4);
                                break;
                            case _AlphaSource_token:
                                texture.AlphaSource = (FBXAlphaSource)property.Properties.GetIntValue(4);
                                break;
                            case _Cropping_token:
                                texture.Cropping = property.Properties.GetVector4Value(4); //todo: check this
                                break;
                        }
                    }
                }
            }

            var textureName = node.GetNodeByName(_TextureName_token);
            if (textureName!= null)
            {
                texture.TextureName = textureName.Properties.GetStringValue(0, false);
            }

            var media = node.GetNodeByName(_Media_token);
            if (media!= null)
            {
                texture.Media = media.Properties.GetStringValue(0, false);
            }

            var filename = node.GetNodeByName(_Filename_token);
            if (filename!= null)
            {
                texture.FullFilename = filename.Properties.GetStringValue(0, false);
            }

            var relativeFilename = node.GetNodeByName(_RelativeFilename_token);
            if (relativeFilename!= null)
            {
                texture.RelativeFilename = relativeFilename.Properties.GetStringValue(0, false);
            }

            var modelUVTranslation = node.GetNodeByName(_ModelUVTranslation_token);
            if (modelUVTranslation!= null)
            {
                var r = modelUVTranslation.Properties.GetFloatValue(0);
                var g = modelUVTranslation.Properties.GetFloatValue(1);
                texture.ModelUVTranslation = new Vector2(r, g);
            }

            var modelUVScaling = node.GetNodeByName(_ModelUVScaling_token);
            if (modelUVScaling!= null)
            {
                var r = modelUVScaling.Properties.GetFloatValue(0);
                var g = modelUVScaling.Properties.GetFloatValue(1);
                texture.ModelUVScaling = new Vector2(r, g);
            }

            var textureAlphaSource = node.GetNodeByName(_Texture_Alpha_Source_token);
            if (textureAlphaSource!= null)
            {
                texture.Texture_Alpha_Source = textureAlphaSource.Properties.GetStringValue(0, false);
            }

            var type = node.GetNodeByName(_Type_token);
            if (type!= null)
            {
                texture.Type = type.Properties.GetStringValue(0, false);
            }

            if (texture.Filename == null && texture.RelativeFilename == null)
            {
                texture.Id = objectId.GetHashCode(); //todo: assigning the texture id from the object id hash. the texture needs at least the id to be comparable
            }

            return texture;
        }

        private void PostProcessTextures()
        {
            foreach (FBXTexture texture in Document.AllTextures)
            {
                if (texture.Video != null)
                {
                    texture.DataStream = texture.Video.ContentStream;
                }
                texture.ResolveFilename(Reader.AssetLoaderContext);
            }
        }
    }
}
