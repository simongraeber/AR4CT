using System;
using System.Collections.Generic;
using System.Globalization;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Obj
{
    public class ObjMaterial : IMaterial
    {

        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();
        public bool DoubleSided { get; set; }
        public int Index { get; set; }
        public MaterialShadingSetup MaterialShadingSetup
        {
            get
            {
                if (_properties.ContainsKey("map_Pr") || _properties.ContainsKey("Pr"))
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                return MaterialShadingSetup.PhongLambert;
            }
        }

        public bool MixAlbedoColorWithTexture => true;
        public string Name { get; set; }
        public bool Processed { get; set; }
        public bool Processing { get; set; }
        public bool Used { get; set; }
        public bool UsesAlpha => false;
        public bool UsesRoughnessSetup => false;

        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
            if (propertyValue is ITexture texture)
            {
                if (propertyName == "norm" || propertyName == "map_bump")
                {
                    texture.TextureFormat = TextureFormat.UNorm;
                }
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
                return (Color)propertyValue;
            }
            return Color.white;
        }

        public float GetFloatValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return Convert.ToSingle(propertyValue, CultureInfo.InvariantCulture);
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
                case GenericMaterialProperty.OcclusionStrength:
                case GenericMaterialProperty.NormalStrength:
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
                    return 1f - value;
                case GenericMaterialProperty.AlphaValue:
                    return HasProperty("Tr") ? 1f - value : value;
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
                    return "Kd";
                case GenericMaterialProperty.DiffuseMap:
                    return "map_Kd";
                case GenericMaterialProperty.SpecularColor:
                    return "Ks";
                case GenericMaterialProperty.SpecularMap:
                    return "map_Ks";
                case GenericMaterialProperty.NormalMap:
                    return HasProperty("norm") ? "norm" : "map_bump";
                case GenericMaterialProperty.AlphaValue:
                    return HasProperty("d") ? "d" : "Tr";
                case GenericMaterialProperty.OcclusionMap:
                    return "map_ao";
                case GenericMaterialProperty.EmissionColor:
                    return "Ke";
                case GenericMaterialProperty.EmissionMap:
                    return "map_Ke";
                case GenericMaterialProperty.Metallic:
                    return "Pm";
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return "Pr";
                case GenericMaterialProperty.GlossinessOrRoughnessMap:
                    return "map_Pr";
                case GenericMaterialProperty.MetallicMap:
                    return HasProperty("map_Pm") ? "map_Pm" : "map_Ns";
                case GenericMaterialProperty.TransparencyMap:
                    return HasProperty("map_d") ? "map_d" : "map_Tr";
                case GenericMaterialProperty.DisplacementMap:
                    return "disp";
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
                return Convert.ToInt32(propertyValue);
            }
            return 0;
        }

        public string GetStringValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return Convert.ToString(propertyValue, CultureInfo.InvariantCulture);
            }
            return null;
        }

        public ITexture GetTextureValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return (ITexture)propertyValue;
            }
            return null;
        }

        public Vector3 GetVector3Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return (Vector3)propertyValue;
            }
            return Vector3.zero;
        }

        public Vector4 GetVector4Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return (Vector4)propertyValue;
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

        public bool PostProcessTexture(TextureLoadingContext textureLoadingContext)
        {
            if (!textureLoadingContext.Context.Options.ConvertMaterialTextures || !textureLoadingContext.MaterialMapperContext.MaterialMapper.ConvertMaterialTextures)
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
                        const float shininessExponent = 0f; 
                        var glossinessName = GetGenericPropertyName(GenericMaterialProperty.GlossinessOrRoughness);
                        var roughness = HasProperty(glossinessName) ? GetFloatValue(glossinessName) : (float?)null;
                        var metallicName = GetGenericPropertyName(GenericMaterialProperty.Metallic);
                        var metallic = HasProperty(metallicName) ? GetFloatValue(metallicName) : (float?)null;
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
                default:
                    return false;
            }
        }
    }
}
