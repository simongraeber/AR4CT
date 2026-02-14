using System;
using System.Collections.Generic;
using System.Globalization;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.ThreeMf
{
    public class ThreeMfMaterial : IMaterial
    {
        private readonly IDictionary<string, object> _properties = new Dictionary<string, object>();

        public string Name { get; set; }
        public bool Used { get; set; }
        public bool UsesAlpha => false;
        public bool ApplyOffsetAndScale(TextureLoadingContext textureLoadingContext)
        {
            return false;
        }

        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
            _properties[propertyName] = propertyValue;
        }

        public string GetGenericPropertyName(GenericMaterialProperty genericMaterialProperty)
        {
            switch (genericMaterialProperty)
            {
                case GenericMaterialProperty.DiffuseColor:
                    return "displayColor";
                case GenericMaterialProperty.DiffuseMap:
                    return "diffuseTex";
                case GenericMaterialProperty.SpecularColor:
                    break;
                case GenericMaterialProperty.SpecularMap:
                    break;
                case GenericMaterialProperty.NormalMap:
                    break;
                case GenericMaterialProperty.AlphaValue:
                    break;
                case GenericMaterialProperty.OcclusionMap:
                    break;
                case GenericMaterialProperty.DisplacementMap:
                    break;
                case GenericMaterialProperty.EmissionColor:
                    break;
                case GenericMaterialProperty.EmissionMap:
                    break;
                case GenericMaterialProperty.Metallic:
                    break;
                case GenericMaterialProperty.GlossinessOrRoughness:
                    break;
                case GenericMaterialProperty.MetallicMap:
                    break;
            }
            return null;
        }

        public Color GetGenericColorValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            return GetGenericColorValue(genericMaterialProperty);
        }

        public float GetGenericFloatValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            return GetGenericFloatValue(genericMaterialProperty);
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
                default:
                    return 0f;
            }
        }

        public int GetGenericIntValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetIntValue(propertyName);
        }

        public string GetGenericStringValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetStringValue(propertyName);
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

        public Color GetGenericColorValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            if (HasProperty(propertyName))
            {
                return GetColorValue(propertyName);
            }
            return materialProperty == GenericMaterialProperty.EmissionColor ? Color.black : Color.white;
        }
        public ITexture GetGenericTextureValue(GenericMaterialProperty materialProperty)
        {
            var propertyName = GetGenericPropertyName(materialProperty);
            return GetTextureValue(propertyName);
        }

        public bool DoubleSided { get; set; }
        public bool MixAlbedoColorWithTexture => false;

        public float GetFloatValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return Convert.ToSingle(propertyValue, CultureInfo.InvariantCulture);
            }
            return 0f;
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

        public Color GetColorValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return (Color)propertyValue;
            }
            return Color.white;
        }

        public ITexture GetTextureValue(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                return (ITexture)propertyValue;
            }
            return null;
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
            return false;
        }

        public MaterialShadingSetup MaterialShadingSetup => MaterialShadingSetup.PhongLambert;
        public int Index { get; set; }
        public bool UsesRoughnessSetup => false;
        public bool Processing { get; set; }
        public bool Processed { get; set; }
    }
}