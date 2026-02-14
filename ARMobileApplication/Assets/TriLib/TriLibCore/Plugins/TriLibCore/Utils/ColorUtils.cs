using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for color calculations, including conversions from specular 
    /// properties to glossiness or metallic values. These methods are useful when adapting 
    /// material properties between different shading models or pipelines.
    /// </summary>
    public static class ColorUtils
    {
        /// <summary>
        /// Converts a specular color and a shinniness exponent into a glossiness value.
        /// </summary>
        /// <param name="specularBase">
        /// The base specular color. Its red, green, and blue components are used to compute 
        /// a weighted average for intensity.
        /// </param>
        /// <param name="shinninessExponent">
        /// The exponent representing the shininess of the material. Larger values indicate a 
        /// sharper specular highlight.
        /// </param>
        /// <returns>
        /// A glossiness value computed using the formula:
        /// <code>
        /// DiffuseGlossiness = 1 - sqrt(2 / (shinninessExponent * specularIntensity + 2))
        /// </code>
        /// where <c>specularIntensity</c> is calculated as:
        /// <c>0.2125 * specularBase.r + 0.7154 * specularBase.g + 0.0721 * specularBase.b</c>.
        /// </returns>
        public static float SpecularToGlossiness(Color specularBase, float shinninessExponent)
        {
            var specularIntensity = specularBase.r * 0.2125f + specularBase.g * 0.7154f + specularBase.b * 0.0721f;
            return 1f - Mathf.Sqrt(2f / (shinninessExponent * specularIntensity + 2f));
        }

        /// <summary>
        /// Converts a specular color and a default diffuse color into a metallic value.
        /// </summary>
        /// <param name="specularBase">
        /// The base specular color of the material, which affects how “metal-like” the surface appears.
        /// </param>
        /// <param name="defaultDiffuse">
        /// The default diffuse color of the material. This color is used to calculate the diffuse brightness.
        /// </param>
        /// <returns>
        /// A metallic value between 0 and 1, computed by comparing the brightness of the specular and diffuse components.
        /// The calculation involves a correction for dielectric specular reflectance (set to 0.04).
        /// </returns>
        public static float SpecularToMetallic(Color specularBase, Color defaultDiffuse)
        {
            const float dielectricSpecular = 0.04f;
            var diffuseBase = defaultDiffuse;
            var diffuseBrightness = 0.299f * Mathf.Pow(diffuseBase.r, 2f) + 0.587f * Mathf.Pow(diffuseBase.g, 2f) + 0.114f * Mathf.Pow(diffuseBase.b, 2f);
            var specularBrightness = 0.299f * Mathf.Pow(specularBase.r, 2f) + 0.587f * Mathf.Pow(specularBase.g, 2f) + 0.114f * Mathf.Pow(specularBase.b, 2f);
            var specularStrength = Mathf.Max(specularBase.r, Mathf.Max(specularBase.g, specularBase.b));
            var oneMinusSpecularStrength = 1f - specularStrength;
            var A = dielectricSpecular;
            var B = (diffuseBrightness * (oneMinusSpecularStrength / (1f - A)) + specularBrightness) - 2f * A;
            var C = A - specularBrightness;
            var squareRoot = Mathf.Sqrt(Mathf.Max(0f, B * B - 4f * A * C));
            var value = (-B + squareRoot) / (2f * A);
            var metalness = Mathf.Clamp(value, 0f, 1f);
            return metalness;
        }
    }
}
