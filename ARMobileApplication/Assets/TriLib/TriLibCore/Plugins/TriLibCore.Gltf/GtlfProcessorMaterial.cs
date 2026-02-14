using TriLibCore.Utils;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private GltfMaterial ConvertMaterial(int i)
        {
            var material = materials.GetArrayValueAtIndex(i);

            var gltfMaterial = new GltfMaterial
            {
                Name = material.GetChildValueAsString(_name_token , _temporaryString)
            };

            if (material.TryGetChildValueAsBool(_doubleSided_token, out var doubleSided, _temporaryString, false))
            {
                gltfMaterial.DoubleSided = doubleSided;
            }

            if (material.TryGetChildWithKey(_emissiveFactor_token , out var emissiveFactor))
            {
                gltfMaterial.AddProperty("EmissiveFactor", ConvertColor(emissiveFactor), false);
            }

            if (material.TryGetChildWithKey(_emissiveTexture_token , out var emissiveTexture))
            {
                AddTextureProperty(emissiveTexture, gltfMaterial, "EmissiveTexture");
            }

            if (material.TryGetChildWithKey(_normalTexture_token , out var normalTexture))
            {
                AddTextureProperty(normalTexture, gltfMaterial, "NormalTexture");
            }

            if (material.TryGetChildWithKey(_occlusionTexture_token , out var occlusionTexture))
            {
                AddTextureProperty(occlusionTexture, gltfMaterial, "OcclusionTexture");
            }

            if (material.TryGetChildWithKey(_pbrMetallicRoughness_token , out var pbrMetallicRoughness))
            {
                if (pbrMetallicRoughness.TryGetChildWithKey(_baseColorFactor_token , out var baseColorFactor))
                {
                    gltfMaterial.AddProperty("PbrMetallicRoughness.BaseColorFactor", ConvertColor(baseColorFactor), false);
                }

                if (pbrMetallicRoughness.TryGetChildValueAsFloat(_metallicFactor_token , out var metallicFactor, _temporaryString))
                {
                    gltfMaterial.AddProperty("PbrMetallicRoughness.MetallicFactor", metallicFactor, false);
                }

                if (pbrMetallicRoughness.TryGetChildValueAsFloat(_roughnessFactor_token , out var roughnessFactor, _temporaryString))
                {
                    gltfMaterial.AddProperty("PbrMetallicRoughness.RoughnessFactor", roughnessFactor, false);
                }

                if (pbrMetallicRoughness.TryGetChildWithKey(_baseColorTexture_token , out var baseColorTexture))
                {
                    AddTextureProperty(baseColorTexture, gltfMaterial, "PbrMetallicRoughness.BaseColorTexture");
                }

                if (pbrMetallicRoughness.TryGetChildWithKey(_metallicRoughnessTexture_token , out var metallicRoughnessTexture))
                {
                    AddTextureProperty(metallicRoughnessTexture, gltfMaterial, "PbrMetallicRoughness.MetallicRoughnessTexture");
                }
            }

            if (material.TryGetChildWithKey(_extensions_token , out var extensions))
            {
                if (extensions.TryGetChildWithKey(_KHR_materials_emissive_strength_token, out var materialsEmissiveStrength))
                {
                    if (materialsEmissiveStrength.TryGetChildWithKey(_emissiveStrength_token , out var emissiveStrength))
                    {
                        gltfMaterial.AddProperty("EmissiveStrength", emissiveStrength.GetValueAsFloat(_temporaryString, 1f), false);
                    }
                }
                if (extensions.TryGetChildWithKey(_KHR_materials_pbrSpecularGlossiness_token , out var pbrSpecularGlossiness))
                {
                    if (pbrSpecularGlossiness.TryGetChildWithKey(_diffuseFactor_token , out var diffuseFactor))
                    {
                        gltfMaterial.AddProperty("PbrSpecularGlossiness.DiffuseFactor", ConvertColor(diffuseFactor), false);
                    }

                    if (pbrSpecularGlossiness.TryGetChildValueAsFloat(_glossinessFactor_token , out var glossinessFactor, _temporaryString))
                    {
                        gltfMaterial.AddProperty("PbrSpecularGlossiness.GlossinessFactor", glossinessFactor, false);
                    }

                    if (pbrSpecularGlossiness.TryGetChildWithKey(_specularFactor_token , out var specularFactor))
                    {
                        gltfMaterial.AddProperty("PbrSpecularGlossiness.SpecularFactor", ConvertColor(specularFactor), false);
                    }

                    if (pbrSpecularGlossiness.TryGetChildWithKey(_diffuseTexture_token , out var diffuseTexture))
                    {
                        AddTextureProperty(diffuseTexture, gltfMaterial, "PbrSpecularGlossiness.DiffuseTexture");
                    }

                    if (pbrSpecularGlossiness.TryGetChildWithKey(_specularGlossinessTexture_token , out var specularGlossinessTexture))
                    {
                        AddTextureProperty(specularGlossinessTexture, gltfMaterial, "PbrSpecularGlossiness.SpecularGlossinessTexture");
                    }
                }
            }

            var alphaMode = material.GetChildValueAsString(_alphaMode_token , _temporaryString);
            if (alphaMode == "BLEND" || alphaMode == "MASK")
            {
                gltfMaterial.AddProperty("Alpha", 0.9999f, false);
            }

            return gltfMaterial;
        }
    }
}