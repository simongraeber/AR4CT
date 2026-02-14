using System;

namespace TriLibCore.General
{
    /// <summary>Represents a Texture usage type.</summary>
    public enum TextureType
    {
        /// <summary>The Texture usage is unknown.</summary>
        Undefined = 0,
        /// <summary>The Diffuse Texture Type.</summary>
        Diffuse = 1,
        /// <summary>The Normal Map Texture Type.</summary>
        NormalMap = 2,
        /// <summary>The Metalness Texture Type.</summary>
        Metalness = 3,
        /// <summary>The Transparency Material shading.</summary>
        Transparency = 4,
        /// <summary>The Occlusion Texture Type.</summary>
        Occlusion = 5,
        /// <summary>The Emission Texture Type.</summary>
        Emission = 6,
        /// <summary>The Glossiness/Roughness Texture Type.</summary>
        GlossinessOrRoughness = 7,
        /// <summary>The Specular Texture Type.</summary>
        Specular = 8,
        /// <summary>The Displacement Texture Type.</summary>
        Displacement = 9
    }
}