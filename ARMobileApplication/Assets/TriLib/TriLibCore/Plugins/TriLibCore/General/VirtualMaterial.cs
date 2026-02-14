using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a temporary container used to store material properties
    /// before they are applied to a Unity <see cref="Material"/>.
    /// </summary>
    /// <remarks>
    /// This class is typically used during import pipelines to accumulate
    /// material data in a renderer-agnostic format.
    /// </remarks>
    public class VirtualMaterial
    {
        /// <summary>
        /// Indicates whether the material contains alpha information
        /// in its diffuse color or texture.
        /// </summary>
        public bool HasAlpha;

        /// <summary>
        /// Gets or sets the material global illumination flags.
        /// </summary>
        public MaterialGlobalIlluminationFlags GlobalIlluminationFlags;

        /// <summary>
        /// Gets the float (single) material properties.
        /// </summary>
        public Dictionary<string, float> FloatProperties { get; } = new Dictionary<string, float>();

        /// <summary>
        /// Gets the vector material properties.
        /// </summary>
        public Dictionary<string, Vector3> VectorProperties { get; } = new Dictionary<string, Vector3>();

        /// <summary>
        /// Gets the color material properties.
        /// </summary>
        public Dictionary<string, Color> ColorProperties { get; } = new Dictionary<string, Color>();

        /// <summary>
        /// Gets the texture material properties.
        /// </summary>
        public Dictionary<string, Texture> TextureProperties { get; } = new Dictionary<string, Texture>();

        /// <summary>
        /// Gets the shader keywords and their enabled states.
        /// </summary>
        public Dictionary<string, bool> Keywords { get; } = new Dictionary<string, bool>();

        /// <summary>
        /// Stores the set of generic material properties that have been
        /// assigned and validated during material loading.
        /// </summary>
        private HashSet<GenericMaterialProperty> _genericMaterialProperties { get; }
            = new HashSet<GenericMaterialProperty>();

        /// <summary>
        /// Gets or sets a value indicating whether the source material
        /// defines an emission color.
        /// </summary>
        [Obsolete("Please use the GenericColorPropertyIsSetAndValid method")]
        public bool HasEmissionColor { get; set; }

        /// <summary>
        /// Gets or sets the texture coordinate offset.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Gets or sets the texture coordinate tiling.
        /// </summary>
        public Vector2 Tiling = Vector2.one;

        /// <summary>
        /// Sets a float material property.
        /// </summary>
        /// <param name="property">The shader property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="genericMaterialProperty">
        /// The generic material property associated with this value.
        /// </param>
        public void SetProperty(string property, float value,
            GenericMaterialProperty genericMaterialProperty = GenericMaterialProperty.Unknown)
        {
            FloatProperties[property] = value;
            if (genericMaterialProperty != GenericMaterialProperty.Unknown)
            {
                _genericMaterialProperties.Add(genericMaterialProperty);
            }
        }

        /// <summary>
        /// Sets a vector material property.
        /// </summary>
        /// <param name="property">The shader property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="genericMaterialProperty">
        /// The generic material property associated with this value.
        /// </param>
        public void SetProperty(string property, Vector3 value,
            GenericMaterialProperty genericMaterialProperty = GenericMaterialProperty.Unknown)
        {
            VectorProperties[property] = value;
            if (genericMaterialProperty != GenericMaterialProperty.Unknown)
            {
                _genericMaterialProperties.Add(genericMaterialProperty);
            }
        }

        /// <summary>
        /// Sets a color material property.
        /// </summary>
        /// <param name="property">The shader property name.</param>
        /// <param name="value">The property value.</param>
        /// <param name="genericMaterialProperty">
        /// The generic material property associated with this value.
        /// </param>
        public void SetProperty(string property, Color value,
            GenericMaterialProperty genericMaterialProperty = GenericMaterialProperty.Unknown)
        {
            ColorProperties[property] = value;
            if (genericMaterialProperty != GenericMaterialProperty.Unknown)
            {
                _genericMaterialProperties.Add(genericMaterialProperty);
            }
        }

        /// <summary>
        /// Sets a texture material property.
        /// </summary>
        /// <param name="property">The shader property name.</param>
        /// <param name="value">The texture value.</param>
        /// <param name="genericMaterialProperty">
        /// The generic material property associated with this value.
        /// </param>
        public void SetProperty(string property, Texture value,
            GenericMaterialProperty genericMaterialProperty = GenericMaterialProperty.Unknown)
        {
            TextureProperties[property] = value;
            if (genericMaterialProperty != GenericMaterialProperty.Unknown && value != null)
            {
                _genericMaterialProperties.Add(genericMaterialProperty);
            }
        }

        /// <summary>
        /// Enables the specified shader keyword.
        /// </summary>
        /// <param name="keyword">The keyword to enable.</param>
        public void EnableKeyword(string keyword)
        {
            Keywords[keyword] = true;
        }

        /// <summary>
        /// Disables the specified shader keyword.
        /// </summary>
        /// <param name="keyword">The keyword to disable.</param>
        public void DisableKeyword(string keyword)
        {
            Keywords[keyword] = false;
        }

        /// <summary>
        /// Determines whether a generic color-related material property
        /// has been set and is considered valid.
        /// </summary>
        /// <param name="genericMaterialProperty">
        /// The generic color property to query.
        /// </param>
        /// <returns>
        /// <c>true</c> if the property is set and valid; otherwise, <c>false</c>.
        /// </returns>
        public bool GenericColorPropertyIsSetAndValid(GenericMaterialProperty genericMaterialProperty)
        {
            switch (genericMaterialProperty)
            {
                case GenericMaterialProperty.DiffuseColor:
                case GenericMaterialProperty.DiffuseMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.DiffuseColor);

                case GenericMaterialProperty.SpecularColor:
                case GenericMaterialProperty.SpecularMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.SpecularColor);

                case GenericMaterialProperty.EmissionColor:
                case GenericMaterialProperty.EmissionMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.EmissionColor);
            }
            return false;
        }

        /// <summary>
        /// Determines whether a generic material property has been set
        /// and is considered valid.
        /// </summary>
        /// <remarks>
        /// If the property represents a color, the corresponding texture
        /// property is also taken into account.
        /// </remarks>
        /// <param name="genericMaterialProperty">
        /// The generic material property to query.
        /// </param>
        /// <returns>
        /// <c>true</c> if the property is set and valid; otherwise, <c>false</c>.
        /// </returns>
        public bool GenericPropertyIsSetAndValid(GenericMaterialProperty genericMaterialProperty)
        {
            switch (genericMaterialProperty)
            {
                case GenericMaterialProperty.DiffuseColor:
                case GenericMaterialProperty.DiffuseMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.DiffuseMap);

                case GenericMaterialProperty.SpecularColor:
                case GenericMaterialProperty.SpecularMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.SpecularMap);

                case GenericMaterialProperty.NormalStrength:
                case GenericMaterialProperty.NormalMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.NormalMap);

                case GenericMaterialProperty.AlphaValue:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.TransparencyMap);

                case GenericMaterialProperty.OcclusionStrength:
                case GenericMaterialProperty.OcclusionMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.OcclusionMap);

                case GenericMaterialProperty.DisplacementMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.DisplacementMap);

                case GenericMaterialProperty.EmissionColor:
                case GenericMaterialProperty.EmissionMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.EmissionMap);

                case GenericMaterialProperty.GlossinessOrRoughness:
                case GenericMaterialProperty.Metallic:
                case GenericMaterialProperty.MetallicMap:
                    return _genericMaterialProperties.Contains(GenericMaterialProperty.MetallicMap);
            }
            return false;
        }

        /// <summary>
        /// Determines whether a generic material property has been set,
        /// regardless of its validity.
        /// </summary>
        /// <param name="genericMaterialProperty">
        /// The generic material property to query.
        /// </param>
        /// <returns>
        /// <c>true</c> if the property was set; otherwise, <c>false</c>.
        /// </returns>
        public bool GenericPropertyIsSet(GenericMaterialProperty genericMaterialProperty)
        {
            return _genericMaterialProperties.Contains(genericMaterialProperty);
        }
    }
}
