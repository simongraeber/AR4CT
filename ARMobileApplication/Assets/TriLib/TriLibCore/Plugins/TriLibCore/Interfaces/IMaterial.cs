using TriLibCore.General;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Material. Each <see cref="IMaterial"/> holds various 
    /// properties (floats, vectors, colors, textures, etc.) that describe how 
    /// the material will appear when rendered in Unity.
    /// </summary>
    public interface IMaterial : IObject
    {
        /// <summary>
        /// Gets or sets a value indicating whether this Material should be rendered 
        /// from both sides (i.e., whether backface culling is disabled).
        /// </summary>
        bool DoubleSided { get; set; }

        /// <summary>
        /// Gets a value indicating whether the albedo (diffuse) color from this Material 
        /// should be multiplied by the albedo texture for final rendering.
        /// </summary>
        bool MixAlbedoColorWithTexture { get; }

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="float"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as a float, if found; otherwise, 0.
        /// </returns>
        float GetFloatValue(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="float"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as a float, if found; otherwise, 0.
        /// </returns>
        float GetGenericFloatValue(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as an <see cref="int"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as an int, if found; otherwise, 0.
        /// </returns>
        int GetIntValue(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as an <see cref="int"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as an int, if found; otherwise, 0.
        /// </returns>
        int GetGenericIntValue(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="string"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as a string, if found; otherwise, <c>null</c>.
        /// </returns>
        string GetStringValue(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="string"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as a string, if found; otherwise, <c>null</c>.
        /// </returns>
        string GetGenericStringValue(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Vector3"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as <see cref="Vector3"/>, if found; otherwise, 
        /// a zero (empty) vector.
        /// </returns>
        Vector3 GetVector3Value(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Vector3"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as <see cref="Vector3"/>, if found; otherwise, 
        /// a zero (empty) vector.
        /// </returns>
        Vector3 GetGenericVector3Value(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Vector4"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as <see cref="Vector4"/>, if found; otherwise, 
        /// a zero (empty) vector.
        /// </returns>
        Vector4 GetVector4Value(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Vector4"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as <see cref="Vector4"/>, if found; otherwise, 
        /// a zero (empty) vector.
        /// </returns>
        Vector4 GetGenericVector4Value(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Color"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as <see cref="Color"/>, if found; otherwise, 
        /// <see cref="Color.white"/>.
        /// </returns>
        Color GetColorValue(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as a <see cref="Color"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as <see cref="Color"/>, if found; otherwise, 
        /// <see cref="Color.white"/>.
        /// </returns>
        Color GetGenericColorValue(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Retrieves a property value from this Material as an <see cref="ITexture"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property to look up.</param>
        /// <returns>
        /// The property value as an <see cref="ITexture"/>, if found; otherwise, <c>null</c>.
        /// </returns>
        ITexture GetTextureValue(string propertyName);

        /// <summary>
        /// Retrieves a property value from this Material as an <see cref="ITexture"/>, 
        /// using a <see cref="GenericMaterialProperty"/> identifier.
        /// </summary>
        /// <param name="materialProperty">
        /// The enumerated property or descriptor identifying the material property to look up.
        /// </param>
        /// <returns>
        /// The property value as an <see cref="ITexture"/>, if found; otherwise, <c>null</c>.
        /// </returns>
        ITexture GetGenericTextureValue(GenericMaterialProperty materialProperty);

        /// <summary>
        /// Adds a new property to this Material.
        /// </summary>
        /// <param name="propertyName">The name of the property to add.</param>
        /// <param name="propertyValue">
        /// The value of the property to add. Can be a float, int, string, color, vector, or texture.
        /// </param>
        /// <param name="isTexture">
        /// Indicates whether the property is a texture. If <c>true</c>, 
        /// <paramref name="propertyValue"/> is treated as an <see cref="ITexture"/>.
        /// </param>
        void AddProperty(string propertyName, object propertyValue, bool isTexture);

        /// <summary>
        /// Returns the material property name (used by specific readers) 
        /// corresponding to a given <see cref="GenericMaterialProperty"/>.
        /// Different readers (importers) may use different naming conventions 
        /// for the same generic property.
        /// </summary>
        /// <param name="genericMaterialProperty">
        /// The <see cref="GenericMaterialProperty"/> to get the name for.
        /// </param>
        /// <returns>
        /// The material property name used internally for the given <paramref name="genericMaterialProperty"/>.
        /// </returns>
        string GetGenericPropertyName(GenericMaterialProperty genericMaterialProperty);

        /// <summary>
        /// Retrieves the <see cref="Color"/> multiplier for the specified 
        /// <see cref="GenericMaterialProperty"/>, according to the current 
        /// <see cref="MaterialMapperContext"/>. Some readers may require specific 
        /// multipliers for certain color-based properties.
        /// </summary>
        /// <param name="genericMaterialProperty">
        /// The <see cref="GenericMaterialProperty"/> to get the color multiplier for.
        /// </param>
        /// <param name="materialMapperContext">
        /// The <see cref="MaterialMapperContext"/> providing additional context 
        /// for determining the appropriate multiplier. This parameter is optional.
        /// </param>
        /// <returns>
        /// The color multiplier associated with the specified property, or 
        /// <see cref="Color.white"/> if not found.
        /// </returns>
        Color GetGenericColorValueMultiplied(
            GenericMaterialProperty genericMaterialProperty,
            MaterialMapperContext materialMapperContext = null);

        /// <summary>
        /// Retrieves the <see cref="float"/> multiplier for the specified 
        /// <see cref="GenericMaterialProperty"/>, according to the current 
        /// <see cref="MaterialMapperContext"/>. Some readers may require specific 
        /// multipliers for certain float-based properties.
        /// </summary>
        /// <param name="genericMaterialProperty">
        /// The <see cref="GenericMaterialProperty"/> to get the float multiplier for.
        /// </param>
        /// <param name="materialMapperContext">
        /// The <see cref="MaterialMapperContext"/> providing additional context 
        /// for determining the appropriate multiplier. This parameter is optional.
        /// </param>
        /// <returns>
        /// The float multiplier associated with the specified property, or 1.0f if not found.
        /// </returns>
        float GetGenericFloatValueMultiplied(
            GenericMaterialProperty genericMaterialProperty,
            MaterialMapperContext materialMapperContext = null);

        /// <summary>
        /// Determines whether this Material has a property with the given name.
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>
        /// <c>true</c> if the property is found; otherwise, <c>false</c>.
        /// </returns>
        bool HasProperty(string propertyName);

        /// <summary>
        /// Optionally post-processes a texture after it has been loaded. 
        /// This hook can be used to perform additional operations such as 
        /// color-space conversions, compression settings, etc.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing both the original 
        /// and the Unity-generated texture references.
        /// </param>
        /// <returns>
        /// <c>true</c> if the texture was processed; otherwise, <c>false</c>.
        /// </returns>
        bool PostProcessTexture(TextureLoadingContext textureLoadingContext);

        /// <summary>
        /// Gets the <see cref="MaterialShadingSetup"/> associated with this Material. 
        /// This setup is typically used by Material Mappers to select an appropriate 
        /// Unity Shader or template.
        /// </summary>
        MaterialShadingSetup MaterialShadingSetup { get; }

        /// <summary>
        /// Gets or sets the index of this Material within a model or collection.
        /// </summary>
        int Index { get; set; }

        /// <summary>
        /// Gets a value indicating whether this Material uses a "Roughness" setup. 
        /// Materials using roughness typically have metallic/roughness maps 
        /// instead of gloss/specular maps.
        /// </summary>
        bool UsesRoughnessSetup { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this Material is currently 
        /// being processed.
        /// </summary>
        bool Processing { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this Material has already 
        /// been processed. Useful for tracking the state of asynchronous 
        /// or multi-stage operations.
        /// </summary>
        bool Processed { get; set; }

        /// <summary>
        /// Gets a value indicating whether this Material uses any alpha (transparency) data, 
        /// whether via alpha textures or alpha color channels.
        /// </summary>
        bool UsesAlpha { get; }

        /// <summary>
        /// Applies material-specific offset and scale transformations to a texture. 
        /// This can be used for shifting or tiling a texture as required by the Material's properties.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing both the original 
        /// and the Unity-generated texture references.
        /// </param>
        /// <returns>
        /// <c>true</c> if an offset or scale was applied; otherwise, <c>false</c>.
        /// </returns>
        bool ApplyOffsetAndScale(TextureLoadingContext textureLoadingContext);
    }
}
