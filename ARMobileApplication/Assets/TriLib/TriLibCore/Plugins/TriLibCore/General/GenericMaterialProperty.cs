namespace TriLibCore.General
{
    /// <summary>Represents Generic (common) Material Properties.</summary>
    public enum GenericMaterialProperty
    {
        /// <summary>The diffuse Color Property.</summary>
        DiffuseColor = 0,
        /// <summary>The diffuse Texture Property.</summary>
        DiffuseMap = 1,
        /// <summary>The Specular Color Property.</summary>
        SpecularColor = 2,
        /// <summary>The Specular Texture Property.</summary>
        SpecularMap = 3,
        /// <summary>The normal texture property.</summary>
        NormalMap = 4,
        /// <summary>The alpha (opacity) value property.</summary>
        AlphaValue = 5,
        /// <summary>The Occlusion Texture Property.</summary>
        OcclusionMap = 6,
        /// <summary>The Transparency Texture Property.</summary>
        TransparencyMap = 7,
        /// <summary>The Emission Color Property.</summary>
        EmissionColor = 8,
        /// <summary>The Emission Texture Property.</summary>
        EmissionMap = 9,
        /// <summary>The Metallic Property.</summary>
        Metallic = 10,
        /// <summary>The Glossiness/Roughness Property.</summary>
        GlossinessOrRoughness = 11,
        /// <summary>The Metallic Texture Property.</summary>
        MetallicMap = 12,
        /// <summary>The Glossiness/Roughness Texture Property.</summary>
        GlossinessOrRoughnessMap = 13,
        /// <summary>The Occlusion Strength Property.</summary>
        OcclusionStrength = 14,
        /// <summary>The Normal Strength Property.</summary>
        NormalStrength = 15,
        /// <summary>
        /// The UV U channel offset Property.
        /// </summary>
        UOffset = 17,
        /// <summary>
        /// The UV V channel offset Property.
        /// </summary>
        VOffset = 18,
        /// <summary>The Displacement Texture Property.</summary>
        DisplacementMap = 20,
        /// <summary>The Displacement Strength Property.</summary>
        DisplacementStrength = 21,
        /// <summary>
        /// Unknown Property type.
        /// </summary>
        Unknown = 1000
    }
}