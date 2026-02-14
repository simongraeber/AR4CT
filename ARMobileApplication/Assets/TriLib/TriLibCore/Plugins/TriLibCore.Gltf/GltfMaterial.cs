using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Gltf
{
    public class GltfMaterial : IMaterial
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        public bool DoubleSided { get; set; }
        public int Index { get; set; }

        public MaterialShadingSetup MaterialShadingSetup
        {
            get
            {
                if (UsingSpecularGlossinness)
                {
                    return MaterialShadingSetup.SpecGlossiness;
                }
                return MaterialShadingSetup.MetallicRoughness;
            }
        }

        public bool MixAlbedoColorWithTexture => true;
        public string Name { get; set; }
        public bool Processed { get; set; }
        public bool Processing { get; set; }
        public bool Used { get; set; }
        public bool UsesAlpha => false;
        public bool UsesRoughnessSetup => UsingSpecularGlossinness;

        public bool UsingSpecularGlossinness =>
            HasProperty("PbrSpecularGlossiness.DiffuseTexture") ||
            HasProperty("PbrSpecularGlossiness.SpecularGlossinessTexture") ||
            HasProperty("PbrSpecularGlossiness.GlossinessFactor");

        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
            if (propertyValue is ITexture texture && propertyName == "NormalTexture")
            {
                texture.TextureFormat = TextureFormat.UNorm;
            }
            _properties[propertyName] = propertyValue;
        }

        public bool ApplyOffsetAndScale(TextureLoadingContext textureLoadingContext)
        {
            return false;
        }
        public Color GetColorValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is Color colorValue)
                {
                    return colorValue;
                }
                if (propertyValue is Vector3 vector3Value)
                {
                    return new Color(vector3Value.x, vector3Value.y, vector3Value.z);
                }
                if (propertyValue is Vector4 vector4Value)
                {
                    return vector4Value;
                }
            }
            return Color.white;
        }

        public float GetFloatValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is float floatValue)
                {
                    return floatValue;
                }
            }
            return 0f;
        }

        public Color GetGenericColorValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            if (HasProperty(propertyName))
            {
                return GetColorValue(propertyName);
            }
            return materialProperty == GenericMaterialProperty.EmissionColor ? Color.black : Color.white;
        }

        public Color GetGenericColorValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            var value = GetGenericColorValue(genericMaterialProperty);
            if (genericMaterialProperty == GenericMaterialProperty.EmissionColor)
            {
                if (_properties.ContainsKey("EmissiveStrength"))
                {
                    var emissiveStrength = GetFloatValue("EmissiveStrength");
                    value *= emissiveStrength;
                }
            }
            return value;
        }

        public float GetGenericFloatValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            if (HasProperty(propertyName))
            {
                return GetFloatValue(propertyName);
            }
            switch (materialProperty)
            {
                case GenericMaterialProperty.AlphaValue:
                case GenericMaterialProperty.OcclusionStrength:
                case GenericMaterialProperty.NormalStrength:
                    return 1f;
                case GenericMaterialProperty.Metallic:
                    return 1f;
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return 1f;
                default:
                    return 0f;
            }
        }

        public float GetGenericFloatValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            var value = GetGenericFloatValue(genericMaterialProperty);
            switch (genericMaterialProperty)
            {
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return UsingSpecularGlossinness ? value : !(materialMapperContext != null && materialMapperContext.Context.Options.DoPBRConversion) ? value : 1.0f - value;
            }
            return value;
        }

        public int GetGenericIntValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetIntValue(propertyName);
        }

        public string GetGenericPropertyName(GenericMaterialProperty genericMaterialProperty)
        {
            switch (genericMaterialProperty)
            {
                case GenericMaterialProperty.DiffuseColor:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.DiffuseFactor" : "PbrMetallicRoughness.BaseColorFactor";
                case GenericMaterialProperty.DiffuseMap:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.DiffuseTexture" : "PbrMetallicRoughness.BaseColorTexture";
                case GenericMaterialProperty.SpecularColor:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.SpecularFactor" : null;
                case GenericMaterialProperty.SpecularMap:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.SpecularGlossinessTexture" : null;
                case GenericMaterialProperty.NormalMap:
                    return "NormalTexture";
                case GenericMaterialProperty.AlphaValue:
                    return "Alpha";
                case GenericMaterialProperty.OcclusionMap:
                    return "OcclusionTexture";
                case GenericMaterialProperty.EmissionColor:
                    return "EmissiveFactor";
                case GenericMaterialProperty.EmissionMap:
                    return "EmissiveTexture";
                case GenericMaterialProperty.Metallic:
                    return UsingSpecularGlossinness ? null : "PbrMetallicRoughness.MetallicFactor";
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.GlossinessFactor" : "PbrMetallicRoughness.RoughnessFactor";
                case GenericMaterialProperty.GlossinessOrRoughnessMap:
                    return UsingSpecularGlossinness ? "PbrSpecularGlossiness.SpecularGlossinessTexture" : "PbrMetallicRoughness.MetallicRoughnessTexture";
                case GenericMaterialProperty.MetallicMap:
                    return "PbrMetallicRoughness.MetallicRoughnessTexture";
            }
            return null;
        }
        public string GetGenericStringValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetStringValue(propertyName);
        }

        public ITexture GetGenericTextureValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetTextureValue(propertyName);
        }

        public Vector3 GetGenericVector3Value(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetVector3Value(propertyName);
        }
        public Vector4 GetGenericVector4Value(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetVector4Value(propertyName);
        }
        public int GetIntValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is int intValue)
                {
                    return intValue;
                }
            }
            return 0;
        }

        public string GetStringValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is string stringValue)
                {
                    return stringValue;
                }
            }
            return null;
        }

        public ITexture GetTextureValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is ITexture textureValue)
                {
                    return textureValue;
                }
            }
            return null;
        }

        public Vector3 GetVector3Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is Vector3 vector3Value)
                {
                    return vector3Value;
                }
            }
            return Vector3.zero;
        }

        public Vector4 GetVector4Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is Vector4 vector4Value)
                {
                    return vector4Value;
                }
            }
            return Vector4.zero;
        }
        public bool HasProperty(string propertyName)
        {
            if (propertyName == null)
            {
                return false;
            }
            return _properties.ContainsKey(propertyName);
        }

        /// <summary>
        /// Optional method used to post-process a texture.
        /// </summary>
        /// <param name="textureLoadingContext">Context containing Data from the Original and the Unity Texture.</param>
        /// <returns><c>true</c> if the texture has been processed, otherwise <c>false</c>.</returns>
        public bool PostProcessTexture(TextureLoadingContext textureLoadingContext)
        {
            if (!textureLoadingContext.Context.Options.ConvertMaterialTextures || !textureLoadingContext.MaterialMapperContext.MaterialMapper.ConvertMaterialTextures)
            {
                return false;
            }
            if (textureLoadingContext.TextureType == TextureType.Diffuse && UsingSpecularGlossinness)
            {
                textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Specular, out var specularTexture);
                textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Diffuse, out var diffuseTexture);
                var diffuse = GetGenericColorValueMultiplied(GenericMaterialProperty.DiffuseColor);
                var specular = GetGenericColorValueMultiplied(GenericMaterialProperty.SpecularColor);
                var glossinessName = GetGenericPropertyName(GenericMaterialProperty.GlossinessOrRoughness);
                var glossiness = HasProperty(glossinessName) ? GetFloatValue(glossinessName) : (float?)null;
                if (specularTexture == null)
                {
                    return false;
                }
                TextureUtils.SpecularDiffuseToAlbedo(textureLoadingContext, diffuseTexture, specularTexture, diffuse, specular, glossiness.GetValueOrDefault(), true);
                TextureUtils.ApplyTexture2D(textureLoadingContext, true);
            }
            if (textureLoadingContext.TextureType == TextureType.Metalness)
            {
                textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Specular, out var specularTexture);
                textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Diffuse, out var diffuseTexture);
                var diffuse = GetGenericColorValueMultiplied(GenericMaterialProperty.DiffuseColor);
                var specular = GetGenericColorValueMultiplied(GenericMaterialProperty.SpecularColor);
                var glossinessName = GetGenericPropertyName(GenericMaterialProperty.GlossinessOrRoughness);
                var glossiness = HasProperty(glossinessName) ? GetFloatValue(glossinessName) : (float?)null;
                if (UsingSpecularGlossinness)
                {
                    if (specularTexture == null)
                    {
                        return false;
                    }
                    TextureUtils.SpecularDiffuseToAlbedo(textureLoadingContext, diffuseTexture, specularTexture, diffuse, specular, glossiness.GetValueOrDefault(), false);
                    TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                    return true;
                }
                textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.GlossinessOrRoughness, out var glossinessTexture);
                if (glossinessTexture == null)
                {
                    return false;
                }
                const float shininessExponent = 1f;
                var metallicName = GetGenericPropertyName(GenericMaterialProperty.Metallic);
                var metallic = HasProperty(metallicName) ? GetFloatValue(metallicName) : (float?)null;
                Texture metallicTexture;
                int metallicTextureComponentIndex;
                int glossinessComponentIndex;
                if (textureLoadingContext.MaterialMapperContext.MaterialMapper.ExtractMetallicAndSmoothness)
                {
                    TextureUtils.ExtractChannelData(2, textureLoadingContext, "metallic");
                    metallicTexture = textureLoadingContext.UnityTexture;
                    metallicTextureComponentIndex = 0;
                    glossinessComponentIndex = 0;
                }
                else
                {
                    metallicTexture = glossinessTexture;
                    metallicTextureComponentIndex = 2;
                    glossinessComponentIndex = 1;
                }

                TextureUtils.BuildMetallicTexture(
                    textureLoadingContext,
                    diffuseTexture,
                    metallicTexture,
                    specularTexture,
                    glossinessTexture,
                    diffuse,
                    specular,
                    shininessExponent,
                    glossiness,
                    metallic,
                    !UsingSpecularGlossinness,
                    true,
                    metallicTextureComponentIndex,
                    glossinessComponentIndex
                );
                TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                return true;
            }
            if (textureLoadingContext.OriginalUnityTexture != null)
            {
                switch (textureLoadingContext.TextureType)
                {
                    case TextureType.GlossinessOrRoughness when textureLoadingContext.MaterialMapperContext.MaterialMapper.ExtractMetallicAndSmoothness:
                        {
                            if (UsingSpecularGlossinness)
                            {
                                TextureUtils.ExtractChannelData(3, textureLoadingContext, "glossiness");
                            }
                            else
                            {
                                TextureUtils.ExtractChannelData(1, textureLoadingContext, "roughness");
                            }
                            TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                            return true;
                        }
                    case TextureType.Occlusion:
                        {
                            TextureUtils.ExtractChannelData(0, textureLoadingContext, "occlusion");
                            TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                            return true;
                        }
                }
            }
            return false;
        }
    }
}