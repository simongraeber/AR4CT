using System;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Materials
{
    /// <summary>
    /// Represents a minimal fallback implementation of <see cref="IMaterial"/>.
    /// </summary>
    /// <remarks>
    /// This material is used as a safe placeholder when no concrete material
    /// definition is available. It provides sensible default values for
    /// generic material properties while leaving most lookups unimplemented.
    /// </remarks>
    public class DummyMaterial : IMaterial
    {
        /// <summary>
        /// Gets or sets the material name.
        /// </summary>
        public string Name { get; set; } = "DummyMaterial";

        /// <summary>
        /// Gets or sets a value indicating whether this material was used during import.
        /// </summary>
        public bool Used { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this material should be treated as double-sided.
        /// </summary>
        public bool DoubleSided { get; set; }

        /// <summary>
        /// Gets a value indicating whether albedo color should be mixed with the albedo texture.
        /// </summary>
        public bool MixAlbedoColorWithTexture { get; }

        /// <summary>
        /// Gets a float property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value, or <c>default</c> if not defined.</returns>
        public float GetFloatValue(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic float material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>A default float value appropriate for the property.</returns>
        public float GetGenericFloatValue(GenericMaterialProperty materialProperty)
        {
            switch (materialProperty)
            {
                case GenericMaterialProperty.DiffuseColor:
                    return 1f;
                case GenericMaterialProperty.AlphaValue:
                    return 1f;
                case GenericMaterialProperty.Metallic:
                    return 0f;
                case GenericMaterialProperty.GlossinessOrRoughness:
                    return 0.5f;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Gets an integer property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value, or <c>default</c>.</returns>
        public int GetIntValue(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic integer material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>A default integer value appropriate for the property.</returns>
        public int GetGenericIntValue(GenericMaterialProperty materialProperty)
        {
            switch (materialProperty)
            {
                case GenericMaterialProperty.AlphaValue:
                    return 1;
                case GenericMaterialProperty.Metallic:
                    return 0;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Gets a string property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The property value, or <c>default</c>.</returns>
        public string GetStringValue(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic string material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>The string value, or <c>default</c>.</returns>
        public string GetGenericStringValue(GenericMaterialProperty materialProperty)
        {
            return default;
        }

        /// <summary>
        /// Gets a Vector3 property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The vector value, or <c>default</c>.</returns>
        public Vector3 GetVector3Value(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic Vector3 material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>The vector value, or <c>default</c>.</returns>
        public Vector3 GetGenericVector3Value(GenericMaterialProperty materialProperty)
        {
            return default;
        }

        /// <summary>
        /// Gets a Vector4 property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The vector value, or <c>default</c>.</returns>
        public Vector4 GetVector4Value(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic Vector4 material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>The vector value, or <c>default</c>.</returns>
        public Vector4 GetGenericVector4Value(GenericMaterialProperty materialProperty)
        {
            return default;
        }

        /// <summary>
        /// Gets a color property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The color value, or <c>default</c>.</returns>
        public Color GetColorValue(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic color material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>A color constructed from the corresponding generic float value.</returns>
        public Color GetGenericColorValue(GenericMaterialProperty materialProperty)
        {
            return GetGenericFloatValue(materialProperty) * Vector4.one;
        }

        /// <summary>
        /// Gets a texture property value by name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The texture, or <c>null</c>.</returns>
        public ITexture GetTextureValue(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic texture material value.
        /// </summary>
        /// <param name="materialProperty">The generic material property.</param>
        /// <returns>The texture, or <c>null</c>.</returns>
        public ITexture GetGenericTextureValue(GenericMaterialProperty materialProperty)
        {
            return default;
        }

        /// <summary>
        /// Adds a material property.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <param name="propertyValue">The property value.</param>
        /// <param name="isTexture">Whether the property represents a texture.</param>
        public void AddProperty(string propertyName, object propertyValue, bool isTexture)
        {
        }

        /// <summary>
        /// Gets the shader property name associated with a generic material property.
        /// </summary>
        /// <param name="genericMaterialProperty">The generic material property.</param>
        /// <returns>The property name, or <c>default</c>.</returns>
        public string GetGenericPropertyName(GenericMaterialProperty genericMaterialProperty)
        {
            return default;
        }

        /// <summary>
        /// Gets a generic color value multiplied by any applicable mapper context.
        /// </summary>
        /// <param name="genericMaterialProperty">The generic material property.</param>
        /// <param name="materialMapperContext">Optional material mapper context.</param>
        /// <returns>The resulting color value.</returns>
        public Color GetGenericColorValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            return GetGenericColorValue(genericMaterialProperty);
        }

        /// <summary>
        /// Gets a generic float value multiplied by any applicable mapper context.
        /// </summary>
        /// <param name="genericMaterialProperty">The generic material property.</param>
        /// <param name="materialMapperContext">Optional material mapper context.</param>
        /// <returns>The resulting float value.</returns>
        public float GetGenericFloatValueMultiplied(GenericMaterialProperty genericMaterialProperty, MaterialMapperContext materialMapperContext = null)
        {
            return GetGenericFloatValue(genericMaterialProperty);
        }

        /// <summary>
        /// Determines whether this material defines a property with the given name.
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns><c>true</c> if the property exists; otherwise, <c>false</c>.</returns>
        public bool HasProperty(string propertyName)
        {
            return default;
        }

        /// <summary>
        /// Performs post-processing on a texture after it has been loaded.
        /// </summary>
        /// <param name="textureLoadingContext">The texture loading context.</param>
        /// <returns><c>true</c> if processing was applied; otherwise, <c>false</c>.</returns>
        public bool PostProcessTexture(TextureLoadingContext textureLoadingContext)
        {
            return default;
        }

        /// <summary>
        /// Gets the shading setup used by this material.
        /// </summary>
        public MaterialShadingSetup MaterialShadingSetup => MaterialShadingSetup.PhongLambert;

        /// <summary>
        /// Gets or sets the material index.
        /// </summary>
        public int Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether this material uses a roughness-based workflow.
        /// </summary>
        public bool UsesRoughnessSetup { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this material is currently being processed.
        /// </summary>
        public bool Processing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this material has finished processing.
        /// </summary>
        public bool Processed { get; set; }

        /// <summary>
        /// Gets a value indicating whether this material uses alpha.
        /// </summary>
        public bool UsesAlpha => false;

        /// <summary>
        /// Applies texture offset and scale to the given texture loading context.
        /// </summary>
        /// <param name="textureLoadingContext">The texture loading context.</param>
        /// <returns><c>true</c> if offset and scale were applied; otherwise, <c>false</c>.</returns>
        public bool ApplyOffsetAndScale(TextureLoadingContext textureLoadingContext)
        {
            return default;
        }
    }
}
