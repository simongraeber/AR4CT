using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Dae
{
    public class DaeMaterial : IMaterial
    {
        public const string AmbientName = "ambient";
        public const string BumpName = "bump";
        public const string DiffuseName = "diffuse";
        public const string EmissionName = "emission";
        public const string IndexOfRefractionName = "index_of_refraction";
        public const string ReflectiveName = "reflective";
        public const string ReflectivityName = "reflectivity";
        public const string ShininessName = "shininess";
        public const string ShininessExponentName = "shininessExponent";
        public const string SpecularName = "specular";
        public const string TransparencyName = "transparency";
        public const string TransparentName = "transparent";
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        public bool DoubleSided { get; set; }
        public int Index { get; set; }
        public bool UsesRoughnessSetup => false;
        public MaterialShadingSetup MaterialShadingSetup => MaterialShadingSetup.PhongLambert;
        public bool MixAlbedoColorWithTexture => false;
        public string Name { get; set; }
        public bool Processed { get; set; }
        public bool Processing { get; set; }
        public bool Used { get; set; }
        public bool UsesAlpha => false;
        public bool UsingSpecularGlossinness => false;

        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
            if (propertyValue is ITexture texture && propertyName == BumpName)
            {
                texture.TextureFormat = TextureFormat.UNorm;
            }
            if (isTexture)
            {
                propertyName += "Tex";
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
            return GetGenericColorValue(genericMaterialProperty);
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
                    return 0f;
                case GenericMaterialProperty.OcclusionStrength:
                case GenericMaterialProperty.NormalStrength:
                case GenericMaterialProperty.Metallic:
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
                case GenericMaterialProperty.AlphaValue:
                    {
                        return 1f - value;
                    }
                case GenericMaterialProperty.Metallic:
                    {
                        if (GetGenericTextureValue(GenericMaterialProperty.Metallic) == null)
                        {
                            if (materialMapperContext != null && materialMapperContext.Context.Options.DoPBRConversion)
                            {
                                var specular = GetColorValue(SpecularName);
                                var diffuse = GetColorValue(DiffuseName);
                                return ColorUtils.SpecularToMetallic(specular, diffuse);
                            }
                            return value;
                        }
                        return 0f;
                    }
                case GenericMaterialProperty.GlossinessOrRoughness:
                    if (GetGenericTextureValue(GenericMaterialProperty.GlossinessOrRoughnessMap) == null)
                    {
                        if (materialMapperContext != null && materialMapperContext.Context.Options.DoPBRConversion)
                        {
                            var specular = GetColorValue(SpecularName);
                            var shininessExponent = GetFloatValue(ShininessExponentName);
                            return ColorUtils.SpecularToGlossiness(specular, shininessExponent);
                        }
                        return value;
                    }
                    return 1f;
                default:
                    return value;
            }
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
                    return DiffuseName;
                case GenericMaterialProperty.DiffuseMap:
                    return DiffuseName + "Tex";
                case GenericMaterialProperty.SpecularColor:
                    return SpecularName;
                case GenericMaterialProperty.SpecularMap:
                    return SpecularName + "Tex";
                case GenericMaterialProperty.NormalMap:
                    return BumpName + "Tex";
                case GenericMaterialProperty.AlphaValue:
                    return TransparencyName;
                case GenericMaterialProperty.OcclusionMap:
                    return null;
                case GenericMaterialProperty.EmissionColor:
                    return EmissionName;
                case GenericMaterialProperty.EmissionMap:
                    return EmissionName + "Tex";
                case GenericMaterialProperty.Metallic:
                    return null;
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return null;
                case GenericMaterialProperty.GlossinessOrRoughnessMap:
                    return null;
                case GenericMaterialProperty.MetallicMap:
                    return null;
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
            if (!textureLoadingContext.Context.Options.ConvertMaterialTextures)
            {
                return false;
            }
            switch (textureLoadingContext.TextureType)
            {
                case TextureType.Metalness:
                    {
                        var metallicTexture = textureLoadingContext.UnityTexture;
                        textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Specular, out var specularTexture);
                        textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.GlossinessOrRoughness, out var glossinessTexture);
                        textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Diffuse, out var diffuseTexture);
                        if (specularTexture == null && glossinessTexture == null)
                        {
                            return false;
                        }
                        var diffuse = GetGenericColorValueMultiplied(GenericMaterialProperty.DiffuseColor);
                        var specular = GetGenericColorValueMultiplied(GenericMaterialProperty.SpecularColor);
                        var shininessExponent = GetFloatValue(ShininessName);
                        var glossinessName = GetGenericPropertyName(GenericMaterialProperty.GlossinessOrRoughness);
                        var roughness = glossinessName != null ? GetFloatValue(glossinessName) : (float?)null;
                        var metallicName = GetGenericPropertyName(GenericMaterialProperty.Metallic);
                        var metallic = metallicName != null ? GetFloatValue(metallicName) : (float?)null;
                        {
                            TextureUtils.BuildMetallicTexture(
                                textureLoadingContext,
                                diffuseTexture,
                                metallicTexture,
                                specularTexture,
                                glossinessTexture,
                                diffuse,
                                specular,
                                shininessExponent,
                                roughness,
                                metallic,
                                true
                            );
                            TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                            return true;
                        }
                    }
            }
            return false;
        }
    }
}