using System;
using System.Collections.Generic;
using System.Linq;
using TriLibCore.Extensions;
using TriLibCore.Fbx.Reader;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Fbx
{
    public class FBXMaterial : FBXObject, IMaterial
    {
        public FBXImplementation Implementation;
        public int MultiLayer;

        public string ShadingModel;
        private const string AmbientColor = "AmbientColor";
        private const string AmbientFactor = "AmbientFactor";
        private const string AOMap = "ao_map";
        private const string BaseColor = "base_color";
        private const string BaseColorMap = "base_color_map";
        private const string Bump = "bump";
        private const string BumpMap = "bump_map";
        private const string BumpMapAmt = "bump_map_amt";
        private const string ColorMap = "color_map";
        private const string DiffuseColor = "DiffuseColor";
        private const string DiffuseFactor = "DiffuseFactor";
        private const string DisplacementAmt = "displacement_amt";
        private const string DisplacementMap = "displacement_map";
        private const string Emissive = "emissive";
        private const string EmissiveColor = "EmissiveColor";
        private const string EmissiveFacotr = "EmissiveFactor";
        private const string EmissiveMap = "emissive_map";
        private const string EmitColor = "emit_color";
        private const string EmitColorMap = "emit_color_map";
        private const string Glossiness = "glossiness";
        private const string Metallic = "metallic";
        private const string MetallicMap = "metallic_map";
        private const string Metalness = "metalness";
        private const string MetalnessMap = "metalness_map";
        private const string NormalMap = "NormalMap";
        private const string NormMap = "norm_map";
        private const string Opacity = "Opacity";
        private const string OpacityMap = "opacity_map";
        private const string ReflColor = "refl_color";
        private const string ReflColorMap = "refl_color_map";
        private const string ReflectionFactor = "ReflectionFactor";
        private const string Roughness = "roughness";
        private const string SpecularRoughness = "specularRoughness";
        private const string DiffuseRoughness = "diffuseRoughness";
        private const string RoughnessMap = "roughness_map";
        private const string Shininess = "Shininess";
        private const string ShininessExponent = "ShininessExponent";
        private const string SpecularColor = "SpecularColor";
        private const string SpecularFactor = "SpecularFactor";
        private const string TexMapDiffuse = "texmapDiffuse";
        private const string TransColor = "trans_color";
        private const string TransColorMap = "trans_color_map";
        private const string Transparency = "transparency";
        private const string TransparencyFactor = "TransparencyFactor";
        private const string TransparentColor = "TransparentColor";
        private static readonly string[] NormalMapPropertyNames =
        {
            NormalMap,
            Bump,
            BumpMap,
            NormMap
        };

        private Dictionary<string, object> _properties;
        public FBXMaterial(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Material;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public bool DoubleSided { get; set; }
        public int Index { get; set; }
        public bool IsBlenderMaterial => Document.OriginalApplicationName?.Contains("Blender") ?? false;
        public bool IsPhongMaterial => ShadingModel.Equals("Phong", StringComparison.InvariantCultureIgnoreCase) || ShadingModel.Equals("Lambert", StringComparison.InvariantCultureIgnoreCase);
        public bool IsUnknownOrPhongMaterial => IsPhongMaterial;
        public MaterialShadingSetup MaterialShadingSetup
        {
            get
            {
                if (IsBlenderMaterial)
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                if (Is3DsMaxPhysicalMaterial)
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                if (Is3DsMaxMetalRough)
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                if (Is3DsMaxSpecGloss)
                {
                    return MaterialShadingSetup.SpecGlossiness;
                }
                if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                if (Is3DsMaxArnoldStandardSurfaceMaterial)
                {
                    return MaterialShadingSetup.MetallicRoughness;
                }
                return MaterialShadingSetup.PhongLambert;
            }
        }

        public bool MixAlbedoColorWithTexture => false;

        public bool Processed { get; set; }
        public bool Processing { get; set; }
        public bool UsesAlpha => GetGenericPropertyName(GenericMaterialProperty.TransparencyMap) != null;
        public bool UsesRoughnessSetup => UsingRoughness;
        public bool UsingRoughness => IsBlenderMaterial || Is3DsMaxArnoldStandardSurfaceMaterial || IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial || Is3DsMaxPhysicalMaterial || IsStingRayMaterial || Is3DsMaxMetalRough && GetIntValue("useGlossiness") == 2;
        private bool Is3DsMaxArnoldStandardSurfaceMaterial
        {
            get
            {
                var classIdA = GetIntValue("ClassIDa");
                var classIdB = GetIntValue("ClassIDb");
                return classIdA == 2121471519 && classIdB == 1660373836;
            }
        }

        private bool Is3DsMaxMetalRough
        {
            get
            {
                var classIdA = GetIntValue("ClassIDa");
                var classIdB = GetIntValue("ClassIDb");
                return classIdA == -804315648 && classIdB == -1099438848;
            }
        }

        private bool Is3DsMaxPhysicalMaterial
        {
            get
            {
                var classIdA = GetIntValue("ClassIDa");
                var classIdB = GetIntValue("ClassIDb");
                return classIdA == 1030429932 && classIdB == -559038463;
            }
        }

        private bool Is3DsMaxSpecGloss
        {
            get
            {
                var classIdA = GetIntValue("ClassIDa");
                var classIdB = GetIntValue("ClassIDb");
                return classIdA == -804315648 && classIdB == 31173939;
            }
        }

        private bool IsMayaArnoldStandardSurfaceMaterial
        {
            get
            {
                var typeId = GetIntValue("TypeId");
                return typeId == 1138001 || Implementation != null && Implementation.RenderAPI == "ARNOLD_SHADER_ID";
            }
        }

        private bool IsMayaMentalRayMaterial
        {
            get
            {
                var typeId = GetIntValue("TypeId");
                return typeId == 1398031443 || Implementation != null && Implementation.RenderAPI == "MentalRay";
            }
        }

        private bool IsStingRayMaterial => Implementation != null && Implementation.RenderAPI == "SFX_PBS_SHADER";
        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
            if (propertyValue is ITexture texture && NormalMapPropertyNames.Contains(propertyName))
            {
                texture.TextureFormat = TextureFormat.UNorm;
            }
            _properties[propertyName] = propertyValue;
        }

        public bool ApplyOffsetAndScale(TextureLoadingContext textureLoadingContext)
        {
            var applied = false;
            if (HasProperty("uv_offset"))
            {
                var offset = GetVector2Value("uv_offset");
                offset.y = -offset.y;
                textureLoadingContext.MaterialMapperContext.VirtualMaterial.Offset = offset;
                applied = true;
            }
            if (HasProperty("uv_scale"))
            {
                textureLoadingContext.MaterialMapperContext.VirtualMaterial.Tiling = GetVector2Value("uv_scale");
                applied = true;
            }
            return applied;
        }

        public void CreateLists()
        {
            _properties = new Dictionary<string, object>(System.StringComparer.InvariantCultureIgnoreCase);
            if (Document.MaterialDefinition != null)
            {
                foreach (var kvp in Document.MaterialDefinition._properties)
                {
                    _properties.Add(kvp.Key, kvp.Value);
                }
            }
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
                switch (propertyValue)
                {
                    case float floatValue:
                        return floatValue;
                    case Vector3 vector3Value:
                        return vector3Value.x;
                    case Vector4 vector4Value:
                        return vector4Value.x;
                }
            }
            return 0f;
        }

        public Color GetGenericColorValue(GenericMaterialProperty materialProperty)
        {
            Color value;
            var propertyName = GetGenericPropertyName(materialProperty);
            if (HasProperty(propertyName))
            {
                value = GetColorValue(propertyName);
            }
            else
            {
                switch (materialProperty)
                {
                    case GenericMaterialProperty.EmissionColor:
                        value = Color.black;
                        break;
                    default:
                        value = Color.white;
                        break;
                }
            }
            return value;
        }

        public Color GetGenericColorValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            var value = GetGenericColorValue(genericMaterialProperty);
            if (genericMaterialProperty == GenericMaterialProperty.DiffuseColor)
            {
                if (IsStingRayMaterial && FindStingRayProperty(ColorMap, "use_color_map") != null)
                {
                    return Color.white;
                }
                if (IsPhongMaterial)
                {
                    value = FindBestMultiplier(DiffuseColor, DiffuseFactor) * value;
                    if (FbxReader.ApplyAmbientColor && HasProperty(AmbientColor))
                    {
                        var ambientColor = GetColorValue(AmbientColor);
                        ambientColor = FindBestMultiplier(AmbientColor, AmbientFactor) * ambientColor;
                        value += ambientColor;
                    }
                    value.a = 1f;
                    return value;
                }
            }
            if (genericMaterialProperty == GenericMaterialProperty.EmissionColor)
            {
                if (IsStingRayMaterial && FindStingRayProperty(EmissiveMap, "use_emissive_map") != null)
                {
                    return Color.white;
                }
                if (IsPhongMaterial)
                {
                    var strength = GetFloatValue(EmissiveFacotr);
                    if (strength > 1f && value.r == 0f && value.g == 0f && value.b == 0f)
                    {
                        value.r = 1f;
                        value.g = 1f;
                        value.b = 1f;
                    }
                    return strength * value;
                }
            }
            if (genericMaterialProperty == GenericMaterialProperty.SpecularColor)
            {
                if (IsPhongMaterial)
                {
                    return FindBestMultiplier(SpecularColor, SpecularFactor) * value;
                }
            }
            return value;
        }

        public float GetGenericFloatValue(GenericMaterialProperty materialProperty)
        {
            float value;
            var propertyName = GetGenericPropertyName(materialProperty);
            if (HasProperty(propertyName))
            {
                value = GetFloatValue(propertyName);
            }
            else
            {
                switch (materialProperty)
                {
                    case GenericMaterialProperty.AlphaValue:
                    case GenericMaterialProperty.OcclusionStrength:
                    case GenericMaterialProperty.NormalStrength:
                    case GenericMaterialProperty.GlossinessOrRoughness when UsingRoughness:
                        value = 1f;
                        break;
                    default:
                        value = 0f;
                        break;
                }
            }
            return value;
        }

        public float GetGenericFloatValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            var value = GetGenericFloatValue(genericMaterialProperty);
            if (IsBlenderMaterial || IsUnknownOrPhongMaterial)
            {
                switch (genericMaterialProperty)
                {
                    case GenericMaterialProperty.AlphaValue:
                        {
                            var genericPropertyName = GetGenericPropertyName(genericMaterialProperty);
                            switch (genericPropertyName)
                            {
                                case Opacity:
                                    return Mathf.Clamp01(value);
                                case TransparencyFactor:
                                    return 1.0f - Mathf.Clamp01(value);
                                case TransparentColor:
                                    var transparentColor = GetColorValue(TransparentColor);
                                    return 1.0f - (transparentColor.r + transparentColor.g + transparentColor.b) / 3.0f;
                            }
                            break;
                        }
                    case GenericMaterialProperty.Metallic:
                        {
                            if (GetGenericTextureValue(GenericMaterialProperty.Metallic) == null)
                            {
                                if (!IsBlenderMaterial && IsPhongMaterial)
                                {
                                    if (materialMapperContext != null && materialMapperContext.Context.Options.DoPBRConversion)
                                    {
                                        var specular = GetColorValue(SpecularColor);
                                        var diffuse = GetColorValue(DiffuseColor) * GetFloatValue(DiffuseFactor);
                                        return ColorUtils.SpecularToMetallic(specular, diffuse);
                                    }
                                    else
                                    {
                                        return 0f;
                                    }
                                }
                                return value;
                            }
                            break;
                        }
                    case GenericMaterialProperty.GlossinessOrRoughness:
                        if (GetGenericTextureValue(GenericMaterialProperty.GlossinessOrRoughnessMap) == null)
                        {
                            if (IsBlenderMaterial)
                            {
                                value = Mathf.Sqrt(value) / 10.0f;
                                return value;
                            }
                            if (IsPhongMaterial)
                            {
                                if (materialMapperContext != null && materialMapperContext.Context.Options.DoPBRConversion)
                                {
                                    var specular = GetColorValue(SpecularColor);
                                    var shininessExponent = GetFloatValue(ShininessExponent);
                                    return ColorUtils.SpecularToGlossiness(specular, shininessExponent);
                                    //return Mathf.Sqrt(value / (value + 1f));
                                }
                                else
                                {
                                    return 0.5f;
                                }
                            }
                        }
                        break;
                }
            }
            if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial) && genericMaterialProperty == GenericMaterialProperty.AlphaValue)
            {
                var genericPropertyName = GetGenericPropertyName(genericMaterialProperty);
                switch (genericPropertyName)
                {
                    case Transparency:
                        return 1.0f - Mathf.Clamp01(value);
                    case TransColor:
                        var transparentColor = GetColorValue(TransColor);
                        return 1.0f - (transparentColor.r + transparentColor.g + transparentColor.b) / 3.0f;
                }
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
                    {
                        if (IsStingRayMaterial)
                        {
                            return BaseColor;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return BaseColor;
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return "baseColor";
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return "base_color";
                        }
                        return DiffuseColor;
                    }
                case GenericMaterialProperty.DiffuseMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(ColorMap, "use_color_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, BaseColorMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "baseColor");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "base_color_map");
                        }
                        return FindBestProperty(true, DiffuseColor, TexMapDiffuse);
                    }
                case GenericMaterialProperty.SpecularColor:
                    {
                        if (IsStingRayMaterial)
                        {
                            return null;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, ReflColor);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "specularColor");
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "Specular");
                        }
                        return SpecularColor;
                    }
                case GenericMaterialProperty.SpecularMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return null;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, ReflColorMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "specularColor");
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "specular_map");
                        }
                        return FindBestProperty(true, SpecularColor);
                    }
                case GenericMaterialProperty.NormalMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(NormalMap, "use_normal_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, BumpMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "normalCamera");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, NormMap);
                        }
                        return FindBestProperty(true, NormalMap, Bump);
                    }
                case GenericMaterialProperty.AlphaValue:
                    {
                        if (IsStingRayMaterial)
                        {
                            return null;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(false, Transparency, TransColor);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "opacity");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return null;
                        }
                        return FindBestProperty(false, Opacity, TransparencyFactor);
                    }
                case GenericMaterialProperty.OcclusionMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(AOMap, "use_ao_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return "diff_rough_mapTex";
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "ao_map");
                        }
                        return null;
                    }
                case GenericMaterialProperty.EmissionColor:
                    {
                        if (IsStingRayMaterial)
                        {
                            return Emissive;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return EmitColor;
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "emissionColor");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return EmitColor;
                        }
                        return FindBestProperty(false, EmissiveColor);
                    }
                case GenericMaterialProperty.EmissionMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(EmissiveMap, "use_emissive_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, EmitColorMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "emissionColor");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "emit_color_map");
                        }
                        return FindBestProperty(true, EmissiveColor);
                    }
                case GenericMaterialProperty.Metallic:
                    {
                        if (IsStingRayMaterial)
                        {
                            return Metallic;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return Metalness;
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "metalness");
                        }
                        if (Is3DsMaxMetalRough)
                        {
                            return Metalness;
                        }
                        return IsBlenderMaterial ? FindBestProperty(false, ReflectionFactor) : null;
                    }
                case GenericMaterialProperty.MetallicMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(MetallicMap, "use_metallic_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, MetalnessMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "metalness");
                        }
                        if (Is3DsMaxMetalRough)
                        {
                            return FindBestProperty(true, MetalnessMap);
                        }
                        return FindBestProperty(true, ReflectionFactor);
                    }
                case GenericMaterialProperty.GlossinessOrRoughness:
                    {
                        if (IsStingRayMaterial)
                        {
                            return Roughness;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return Roughness;
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, SpecularRoughness, DiffuseRoughness);
                        }
                        if (Is3DsMaxMetalRough)
                        {
                            return Roughness;
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return Glossiness;
                        }
                        return FindBestProperty(false, Shininess, ShininessExponent);
                    }

                case GenericMaterialProperty.GlossinessOrRoughnessMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return FindStingRayProperty(RoughnessMap, "use_roughness_map");
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, RoughnessMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true,  SpecularRoughness, DiffuseRoughness);
                        }
                        if (Is3DsMaxMetalRough)
                        {
                            return FindBestProperty(true, "roughness_map");
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, "glossiness_map");
                        }
                        return FindBestProperty(true, Shininess, ShininessExponent);
                    }
                case GenericMaterialProperty.TransparencyMap:
                    {
                        if (IsStingRayMaterial)
                        {
                            return null;
                        }
                        if ((Is3DsMaxArnoldStandardSurfaceMaterial || Is3DsMaxPhysicalMaterial))
                        {
                            return FindBestProperty(true, TransColorMap);
                        }
                        if (IsMayaArnoldStandardSurfaceMaterial || IsMayaMentalRayMaterial)
                        {
                            return FindBestProperty(true, "opacity");
                        }
                        if (Is3DsMaxMetalRough || Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, OpacityMap);
                        }
                        return FindBestProperty(true, TransparentColor);
                    }
                case GenericMaterialProperty.DisplacementMap:
                    {
                        if (Is3DsMaxMetalRough)
                        {
                            return FindBestProperty(true, DisplacementMap);
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(true, DisplacementMap);
                        }
                        if (Is3DsMaxPhysicalMaterial)
                        {
                            return FindBestProperty(true, DisplacementMap);
                        }
                        break;
                    }
                case GenericMaterialProperty.DisplacementStrength:
                    {
                        if (Is3DsMaxMetalRough)
                        {
                            return FindBestProperty(false, DisplacementAmt);
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(false, DisplacementAmt);
                        }
                        if (Is3DsMaxPhysicalMaterial)
                        {
                            return FindBestProperty(false, DisplacementAmt);
                        }
                        break;
                    }
                case GenericMaterialProperty.NormalStrength:
                    {
                        if (Is3DsMaxMetalRough)
                        {
                            return FindBestProperty(false, BumpMapAmt);
                        }
                        if (Is3DsMaxSpecGloss)
                        {
                            return FindBestProperty(false, BumpMapAmt);
                        }
                        if (Is3DsMaxPhysicalMaterial)
                        {
                            return FindBestProperty(false, BumpMapAmt);
                        }
                        break;
                    }
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

                if (propertyValue is float floatValue)
                {
                    return (int)floatValue;
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

        public Vector2 GetVector2Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is Vector2 vector2Value)
                {
                    return vector2Value;
                }
                else if (propertyValue is Vector3 vector3Value)
                {
                    return vector3Value;
                }
                else if (propertyValue is Vector4 vector4Value)
                {
                    return (Vector3)vector4Value;
                }
            }
            return Vector2.zero;
        }

        public Vector3 GetVector3Value(string propertyName)
        {
            if (_properties.TryGetValueSafe(propertyName, out var propertyValue))
            {
                if (propertyValue is Vector3 vector3Value)
                {
                    return vector3Value;
                }
                else if (propertyValue is Vector2 vector2Value)
                {
                    return vector2Value;
                }
                else if (propertyValue is Vector4 vector4Value)
                {
                    return vector4Value;
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
                else if (propertyValue is Vector3 vector3Value)
                {
                    return vector3Value;
                }
                else if (propertyValue is Vector2 vector2Value)
                {
                    return vector2Value;
                }
            }
            return Vector4.zero;
        }

        public bool HasProperty(string propertyName)
        {
            if (_properties == null || propertyName == null)
            {
                return false;
            }
            return _properties.ContainsKey(propertyName);
        }

        public sealed override void LoadDefinition()
        {
            if (Document.MaterialDefinition != null)
            {
                MultiLayer = Document.MaterialDefinition.MultiLayer;
                ShadingModel = Document.MaterialDefinition.ShadingModel;
            }
        }

        public bool PostProcessTexture(TextureLoadingContext textureLoadingContext)
        {
            if (!textureLoadingContext.Context.Options.ConvertMaterialTextures || !textureLoadingContext.MaterialMapperContext.MaterialMapper.ConvertMaterialTextures)
            {
                return false;
            }
            switch (textureLoadingContext.TextureType)
            {
                case TextureType.Metalness when !textureLoadingContext.MaterialMapperContext.MaterialMapper.ExtractMetallicAndSmoothness:
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
                        var shininessExponent = GetFloatValue(ShininessExponent);
                        var glossinessName = GetGenericPropertyName(GenericMaterialProperty.GlossinessOrRoughness);
                        var roughness = HasProperty(glossinessName) ? GetGenericFloatValueMultiplied(GenericMaterialProperty.GlossinessOrRoughness, textureLoadingContext.MaterialMapperContext)/* : GetFloatValue(glossinessName)) : (float?)null*/: (float?)null;
                        var metallicName = GetGenericPropertyName(GenericMaterialProperty.Metallic);
                        var metallic = HasProperty(metallicName) ? GetFloatValue(metallicName) : (float?)null;
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
                            UsingRoughness
                        );
                        TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                        return true;
                    }
                case TextureType.Diffuse:
                    {
                        if (!textureLoadingContext.Context.Options.ApplyTransparencyTexture)
                        {
                            return false;
                        }
                        var diffuseTexture = textureLoadingContext.UnityTexture;
                        textureLoadingContext.Context.TryGetMaterialTexture(textureLoadingContext.MaterialMapperContext.Material, TextureType.Transparency, out var transparencyTexture);
                        if (transparencyTexture != null && diffuseTexture != transparencyTexture)
                        {
                            TextureUtils.ApplyTransparency(
                                textureLoadingContext,
                                diffuseTexture,
                                transparencyTexture
                            );
                            TextureUtils.ApplyTexture2D(textureLoadingContext, true);
                            return true;
                        }
                        return false;
                    }
            }
            return false;
        }

        private float FindBestMultiplier(string baseProperty, params string[] properties)
        {
            if (HasProperty(baseProperty))
            {
                for (var index = 0; index < properties.Length; index++)
                {
                    var property = properties[index];
                    if (HasProperty(property))
                    {
                        return GetFloatValue(property);
                    }
                }
            }
            return 1f;
        }

        private string FindBestProperty(bool isTexture, params string[] properties)
        {
            for (var index = 0; index < properties.Length; index++)
            {
                var property = properties[index];
                if (isTexture)
                {
                    property += "Tex";
                }
                if (HasProperty(property))
                {
                    return property;
                }
            }
            return null;
        }

        private string FindStingRayProperty(string mapName, params string[] useName)
        {
            var use = false;
            for (var i = 0; i < useName.Length; i++)
            {
                var name = useName[i];
                if (GetIntValue(name) > 0)
                {
                    use = true;
                    break;
                }
            }

            if (!use)
            {
                return null;
            }
            var textureMapName = $"TEX_{mapName}Tex";
            if (_properties.ContainsKey(textureMapName))
            {
                return textureMapName;
            }
            return null;
        }
    }
}