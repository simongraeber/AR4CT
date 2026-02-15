using System.Text.RegularExpressions;
using UnityEngine;

namespace CT4AR.Utils
{
    /// <summary>
    /// Fixes transparency on materials loaded by TriLib from Blender-exported FBX files.
    ///
    /// <b>Problem:</b> Blender's FBX exporter writes both <c>Opacity=1.0</c> (default)
    /// and <c>TransparencyFactor</c> (the real value).  TriLib checks <c>Opacity</c>
    /// first, finds 1.0, and treats every material as opaque – the actual transparency
    /// stored in <c>TransparencyFactor</c> is never read.
    ///
    /// <b>Solution:</b> The Blender export script (<c>stl_to_fbx.py</c>) appends
    /// <c>__aXXX</c> to each material name, where XXX = alpha × 100 (e.g. <c>__a040</c>
    /// for alpha 0.40).  This helper parses that tag, sets the correct
    /// <c>_BaseColor</c> alpha, and switches the URP material to transparent blending.
    /// </summary>
    public static class MaterialTransparencyHelper
    {
        // Matches "__a" followed by exactly 3 digits anywhere in the name.
        private static readonly Regex AlphaTagRegex = new Regex(
            @"__a(\d{3})", RegexOptions.Compiled);

        /// <summary>
        /// Iterates every <see cref="Renderer"/> in <paramref name="root"/> and
        /// its children.  For each material whose name contains <c>__aXXX</c>,
        /// the alpha is applied and the material is switched to transparent rendering.
        /// </summary>
        public static void ApplyEncodedTransparency(GameObject root)
        {
            if (root == null) return;

            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                // Use sharedMaterials to avoid creating per-renderer copies.
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];
                    if (mat == null) continue;

                    float alpha = ParseAlphaTag(mat.name);
                    if (alpha < 0f) continue; // no tag found

                    if (alpha < 1f)
                    {
                        SetURPTransparent(mat, alpha);
                        Debug.Log($"[CT4AR] Transparency applied: {mat.name} → alpha {alpha:F2}");
                    }
                }
            }
        }

        /// <summary>
        /// Extracts the alpha value from a material name that contains
        /// <c>__aXXX</c>.  Returns -1 if no tag is found.
        /// </summary>
        private static float ParseAlphaTag(string materialName)
        {
            var match = AlphaTagRegex.Match(materialName);
            if (!match.Success) return -1f;
            return int.Parse(match.Groups[1].Value) / 100f;
        }

        /// <summary>
        /// Switches a URP Lit material to transparent blending and sets the
        /// <c>_BaseColor</c> alpha channel.
        /// </summary>
        private static void SetURPTransparent(Material mat, float alpha)
        {
            // ── URP surface type ────────────────────────────────────────
            mat.SetFloat("_Surface", 1f);               // 0 = Opaque, 1 = Transparent
            mat.SetFloat("_Blend", 0f);                 // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply

            // ── Blend state ─────────────────────────────────────────────
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);           // 5
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);   // 10
            mat.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);           // 1
            mat.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha); // 10
            mat.SetFloat("_ZWrite", 0f);

            // ── Render queue & tag ──────────────────────────────────────
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent; // 3000
            mat.SetOverrideTag("RenderType", "Transparent");

            // ── Keywords ────────────────────────────────────────────────
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.DisableKeyword("_ALPHATEST_ON");

            // ── Apply alpha to BaseColor ────────────────────────────────
            if (mat.HasProperty("_BaseColor"))
            {
                Color c = mat.GetColor("_BaseColor");
                c.a = alpha;
                mat.SetColor("_BaseColor", c);
            }
            else if (mat.HasProperty("_Color"))
            {
                // Fallback for Standard shader
                Color c = mat.GetColor("_Color");
                c.a = alpha;
                mat.SetColor("_Color", c);
            }
        }
    }
}
