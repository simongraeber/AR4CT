using System;
using System.IO;
using TriLibCore.Extensions;
using TriLibCore.General;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using Object = UnityEngine.Object;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides a series of utility methods for handling Unity <see cref="Texture"/> objects, 
    /// including creation, manipulation, channel extraction, and conversion of textures.
    /// </summary>
    public static class TextureUtils
    {
        public static Func<GraphicsFormat, GraphicsFormat> GetCompatibleFormatCallback;

        /// <summary>
        /// Uploads the data of a <see cref="Texture2D"/> to the GPU based on the specified <paramref name="textureLoadingContext"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing the Unity texture (if any) and additional options for texture processing.
        /// </param>
        /// <param name="procedural">
        /// A value indicating whether the texture should be treated as procedurally generated.
        /// If <c>true</c>, the texture will be processed only if ConvertTexturesAs2D is also <c>true</c>; otherwise, non-procedural.
        /// </param>
        public static void ApplyTexture2D(TextureLoadingContext textureLoadingContext, bool procedural)
        {
            if (textureLoadingContext.UnityTexture == null ||
                textureLoadingContext.TextureProcessed ||
                !textureLoadingContext.UnityTexture.isReadable)
            {
                return;
            }

            if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
            {
                Debug.Log($"Applying texture [{textureLoadingContext.Texture?.Name ?? "Unnamed"}]");
            }

            // Apply changes and optionally compress the texture.
            if ((procedural && textureLoadingContext.Context.Options.ConvertTexturesAs2D) || !procedural)
            {
                var unityTexture = (Texture2D)textureLoadingContext.UnityTexture;

                // Apply changes if not using Unity's native texture loader.
                if (procedural || !textureLoadingContext.Context.Options.UseUnityNativeTextureLoader)
                {
                    unityTexture.Apply(textureLoadingContext.Context.Options.GenerateMipmaps, false);

                    if (textureLoadingContext.Context.Options.TextureCompressionQuality != TextureCompressionQuality.NoCompression &&
                        CanCompress(textureLoadingContext))
                    {
                        unityTexture.Compress(textureLoadingContext.Context.Options.TextureCompressionQuality == TextureCompressionQuality.Best);
                    }
                }

                unityTexture.Apply(textureLoadingContext.Context.Options.GenerateMipmaps, textureLoadingContext.Context.Options.MarkTexturesNoLongerReadable);
            }
        }

        /// <summary>
        /// Applies the alpha/transparency data from a <paramref name="transparencyTexture"/> onto the color data of a <paramref name="diffuseTexture"/>,
        /// producing a new texture in the <paramref name="textureLoadingContext"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> used to create and store the resulting transparent texture.
        /// </param>
        /// <param name="diffuseTexture">The diffuse (color) texture to which transparency will be applied.</param>
        /// <param name="transparencyTexture">The texture containing transparency or mask data.</param>
        public static void ApplyTransparency(TextureLoadingContext textureLoadingContext, Texture diffuseTexture, Texture transparencyTexture)
        {
            var shader = Shader.Find("Hidden/TriLib/ApplyTransparency");
            var material = new Material(shader);

            material.SetTexture("_DiffuseTexture", diffuseTexture);
            material.SetTexture("_TransparencyTexture", transparencyTexture);
            material.SetInt("_HasDiffuseTexture", diffuseTexture != null ? 1 : 0);
            material.SetInt("_HasTransparencyTexture", transparencyTexture != null ? 1 : 0);

            var width = diffuseTexture?.width ?? transparencyTexture?.width ?? 2;
            var height = diffuseTexture?.height ?? transparencyTexture?.height ?? 2;

            var textureFormat = GetTextureFormat(textureLoadingContext);
            var renderTextureFormat = GetRenderTextureFormat(textureFormat, IsSRGBTexture(textureLoadingContext));
            var renderTexture = RenderTexture.GetTemporary(
                width,
                height,
                0,
                renderTextureFormat,
                textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
            );

            if (!renderTexture.IsCreated())
            {
                renderTexture.useMipMap = false;
                renderTexture.autoGenerateMips = false;
            }

            material.SetTexture(null, textureLoadingContext.OriginalUnityTexture);
            Graphics.Blit(null, renderTexture, material);

            if (renderTexture.useMipMap)
            {
                renderTexture.GenerateMips();
            }

            var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);
            var texture2D = CreateTexture2DInternal(
                textureLoadingContext,
                renderTexture.width,
                renderTexture.height,
                renderTexture.graphicsFormat,
                textureCreationFlags,
                textureFormat
            );

            textureLoadingContext.Context.Allocations.Add(texture2D);
            CopyTextureCPU(renderTexture, texture2D, textureLoadingContext.Context.Options.GenerateMipmaps, false);
            textureLoadingContext.UnityTexture = texture2D;
            textureLoadingContext.UnityTexture.name = $"{GetValidName(textureLoadingContext)}_transparent";

            Graphics.SetRenderTarget(null);
            RenderTexture.ReleaseTemporary(renderTexture);

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Constructs a metallic texture from the specified inputs (diffuse, metallic, specular, glossiness textures, etc.) 
        /// and stores the result within the <paramref name="textureLoadingContext"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> in which the resulting texture is created and stored.
        /// </param>
        /// <param name="diffuseTexture">The diffuse (color) texture.</param>
        /// <param name="metallicTexture">The texture containing metallic data.</param>
        /// <param name="specularTexture">The texture containing specular data.</param>
        /// <param name="glossinessTexture">The texture containing glossiness or roughness data.</param>
        /// <param name="defaultDiffuse">The default diffuse color, if the diffuse texture is absent or partially defined.</param>
        /// <param name="defaultSpecular">The default specular color, if the specular texture is absent or partially defined.</param>
        /// <param name="shininessExponent">The shininess or glossiness exponent for materials.</param>
        /// <param name="defaultRoughness">
        /// An optional default roughness value used if no glossiness or roughness texture is present.
        /// </param>
        /// <param name="defaultMetallic">
        /// An optional default metallic value used if no metallic texture is present.
        /// </param>
        /// <param name="usingRoughness">
        /// <see langword="true"/> if the <paramref name="glossinessTexture"/> is actually a roughness map; <see langword="false"/> otherwise.
        /// </param>
        /// <param name="mixTextureChannelsWithColors">
        /// <see langword="true"/> if the color values (diffuse, specular) should be multiplied by the textures' channels;
        /// <see langword="false"/> to use the textures' channels directly.
        /// </param>
        /// <param name="metallicComponentIndex">The channel index in the metallic texture to sample metallic data from.</param>
        /// <param name="glossinessComponentIndex">The channel index in the glossiness/roughness texture to sample glossiness/roughness data from.</param>
        public static void BuildMetallicTexture(
            TextureLoadingContext textureLoadingContext,
            Texture diffuseTexture,
            Texture metallicTexture,
            Texture specularTexture,
            Texture glossinessTexture,
            Color defaultDiffuse,
            Color defaultSpecular,
            float shininessExponent,
            float? defaultRoughness,
            float? defaultMetallic,
            bool usingRoughness = false,
            bool mixTextureChannelsWithColors = false,
            int metallicComponentIndex = 0,
            int glossinessComponentIndex = 0
        )
        {
            var shader = Shader.Find("Hidden/TriLib/BuildMetallicTexture");
            var material = new Material(shader);

            material.SetTexture("_DiffuseTexture", diffuseTexture);
            material.SetTexture("_MetallicTexture", metallicTexture);
            material.SetTexture("_SpecularTexture", specularTexture);
            material.SetTexture("_GlossinessTexture", glossinessTexture);

            material.SetColor("_DefaultDiffuse", defaultDiffuse);
            material.SetColor("_DefaultSpecular", defaultSpecular);
            material.SetFloat("_DefaultRoughness", defaultRoughness.GetValueOrDefault());
            material.SetFloat("_DefaultMetallic", defaultMetallic.GetValueOrDefault());
            material.SetFloat("_ShininessExponent", shininessExponent);

            material.SetInt("_HasDiffuseTexture", diffuseTexture != null ? 1 : 0);
            material.SetInt("_HasMetallicTexture", metallicTexture != null ? 1 : 0);
            material.SetInt("_HasSpecularTexture", specularTexture != null ? 1 : 0);
            material.SetInt("_HasGlossinessTexture", glossinessTexture != null ? 1 : 0);
            material.SetInt("_HasDefaultRoughness", defaultRoughness.HasValue ? 1 : 0);
            material.SetInt("_HasDefaultMetallic", defaultMetallic.HasValue ? 1 : 0);
            material.SetInt("_UsingRoughness", usingRoughness ? 1 : 0);
            material.SetInt("_MixTextureChannelsWithColors", mixTextureChannelsWithColors ? 1 : 0);
            material.SetInt("_MetallicComponentIndex", metallicComponentIndex);
            material.SetInt("_GlossinessComponentIndex", glossinessComponentIndex);

            material.mainTexture = textureLoadingContext.OriginalUnityTexture;

            var width = Mathf.Max(
                specularTexture != null ? specularTexture.width : 0,
                glossinessTexture != null ? glossinessTexture.width : 0,
                diffuseTexture != null ? diffuseTexture.width : 0
            );
            var height = Mathf.Max(
                specularTexture != null ? specularTexture.height : 0,
                glossinessTexture != null ? glossinessTexture.height : 0,
                diffuseTexture != null ? diffuseTexture.height : 0
            );

            var scaleFactor = textureLoadingContext.Context.Options.ConvertMaterialTexturesUsingHalfRes && width > 2 && height > 2
                ? 0.5f
                : 1f;

            var textureFormat = UnityEngine.TextureFormat.RGBA32;
            var renderTextureFormat = GetRenderTextureFormat(textureFormat, IsSRGBTexture(textureLoadingContext));
            RenderTexture renderTexture;

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                renderTexture = RenderTexture.GetTemporary(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                );

                if (!renderTexture.IsCreated())
                {
                    renderTexture.useMipMap = false;
                    renderTexture.autoGenerateMips = false;
                }
            }
            else
            {
                renderTexture = new RenderTexture(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                )
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };
            }

            Graphics.Blit(null, renderTexture, material);

            textureLoadingContext.Width = width;
            textureLoadingContext.Height = height;

            if (renderTexture.useMipMap)
            {
                renderTexture.GenerateMips();
            }

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);
                var texture2D = CreateTexture2DInternal(
                    textureLoadingContext,
                    renderTexture.width,
                    renderTexture.height,
                    renderTexture.graphicsFormat,
                    textureCreationFlags,
                    textureFormat
                );

                textureLoadingContext.Context.Allocations.Add(texture2D);
                CopyTextureCPU(renderTexture, texture2D, textureLoadingContext.Context.Options.GenerateMipmaps, false);
                textureLoadingContext.UnityTexture = texture2D;
            }
            else
            {
                textureLoadingContext.Context.Allocations.Add(renderTexture);
                textureLoadingContext.UnityTexture = renderTexture;
            }

            textureLoadingContext.UnityTexture.name = $"{GetValidName(textureLoadingContext)}_metallicSmoothness";
            Graphics.SetRenderTarget(null);

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Copies pixel data from a <see cref="RenderTexture"/> to a <see cref="Texture2D"/> on the CPU side.
        /// </summary>
        /// <param name="from">The source <see cref="RenderTexture"/>.</param>
        /// <param name="to">The destination <see cref="Texture2D"/>.</param>
        /// <param name="updateMipMaps">If <c>true</c>, mipmaps are regenerated.</param>
        /// <param name="makeNoLongerReadable">
        /// If <c>true</c>, marks the target texture as not readable, potentially freeing memory.
        /// </param>
        public static void CopyTextureCPU(RenderTexture from, Texture2D to, bool updateMipMaps, bool makeNoLongerReadable)
        {
            RenderTexture.active = from;
            to.ReadPixels(new Rect(0, 0, from.width, from.height), 0, 0);
            to.Apply(updateMipMaps, makeNoLongerReadable);
            RenderTexture.active = null;
        }

        /// <summary>
        /// Creates a new <see cref="Texture2D"/> based on the data in <paramref name="textureLoadingContext"/>,
        /// if a Unity texture has not already been created or loaded.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing metadata (e.g., width, height) for the new texture.
        /// </param>
        public static void CreateTexture2D(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.UnityTexture != null ||
                textureLoadingContext.TextureLoaded ||
                textureLoadingContext.TextureCreated)
            {
                return;
            }

            var textureFormat = GetTextureFormat(textureLoadingContext);
            var format = GetGraphicsFormat(textureLoadingContext, textureFormat, IsSRGBTexture(textureLoadingContext));
            var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);

            if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
            {
                Debug.Log($"Creating texture with {textureLoadingContext.Width}x{textureLoadingContext.Height} ({format}). Flags: {textureCreationFlags}. Name: [{textureLoadingContext.Texture.Name ?? "Unnamed"}]");
            }

            var texture2D = CreateTexture2DInternal(
                textureLoadingContext,
                textureLoadingContext.Width,
                textureLoadingContext.Height,
                format,
                textureCreationFlags,
                textureFormat
            );

            texture2D.wrapModeU = textureLoadingContext.Texture.WrapModeU;
            texture2D.wrapModeV = textureLoadingContext.Texture.WrapModeV;

            textureLoadingContext.Context.Allocations.Add(texture2D);
            textureLoadingContext.OriginalUnityTexture = textureLoadingContext.UnityTexture = texture2D;
            textureLoadingContext.OriginalUnityTexture.name = textureLoadingContext.UnityTexture.name = textureLoadingContext.Texture.Name;
            textureLoadingContext.TextureCreated = true;
        }

        /// <summary>
        /// Extracts a specific color channel (e.g., R, G, B, A) from the original Untiy texture. 
        /// and updates Unity texture in the <paramref name="textureLoadingContext"/> with the extracted channel data.
        /// </summary>
        /// <param name="channelIndex">The index of the channel to extract (0 for Red, 1 for Green, 2 for Blue, 3 for Alpha).</param>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> that contains the source texture and receives the new texture.
        /// </param>
        /// <param name="suffix">
        /// A string suffix added to the resulting texture's name for clarity (e.g., "_redChannel").
        /// </param>
        public static void ExtractChannelData(int channelIndex, TextureLoadingContext textureLoadingContext, string suffix = "")
        {
            if (textureLoadingContext.OriginalUnityTexture == null || textureLoadingContext.UnityTexture == null)
            {
                return;
            }

            var shader = Shader.Find("Hidden/TriLib/ExtractChannelData");
            var material = new Material(shader)
            {
                mainTexture = textureLoadingContext.OriginalUnityTexture
            };

            material.SetInt("_ChannelIndex", channelIndex);

            var textureFormat = GetTextureFormat(textureLoadingContext);
            var renderTextureFormat = GetRenderTextureFormat(textureFormat, IsSRGBTexture(textureLoadingContext));

            var width = textureLoadingContext.OriginalUnityTexture.width;
            var height = textureLoadingContext.OriginalUnityTexture.height;
            var scaleFactor = textureLoadingContext.Context.Options.ConvertMaterialTexturesUsingHalfRes && width > 2 && height > 2
                ? 0.5f
                : 1f;

            RenderTexture renderTexture;
            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                renderTexture = RenderTexture.GetTemporary(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                );

                if (!renderTexture.IsCreated())
                {
                    renderTexture.useMipMap = false;
                    renderTexture.autoGenerateMips = false;
                }
            }
            else
            {
                renderTexture = new RenderTexture(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                )
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };
            }

            Graphics.Blit(null, renderTexture, material);

            if (renderTexture.useMipMap)
            {
                renderTexture.GenerateMips();
            }

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);
                var texture2D = CreateTexture2DInternal(
                    textureLoadingContext,
                    renderTexture.width,
                    renderTexture.height,
                    renderTexture.graphicsFormat,
                    textureCreationFlags,
                    textureFormat
                );

                textureLoadingContext.Context.Allocations.Add(texture2D);
                CopyTextureCPU(renderTexture, texture2D, textureLoadingContext.Context.Options.GenerateMipmaps, false);
                textureLoadingContext.UnityTexture = texture2D;
            }
            else
            {
                textureLoadingContext.Context.Allocations.Add(renderTexture);
                textureLoadingContext.UnityTexture = renderTexture;
            }

            textureLoadingContext.UnityTexture.name = $"{GetValidName(textureLoadingContext)}_{suffix}";

            Graphics.SetRenderTarget(null);
            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Reorders the channels in a normal map texture if needed, modifying the normal map in place.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing a normal map texture, if applicable.
        /// </param>
        public static void FixNormalMap(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.UnityTexture == null ||
                textureLoadingContext.Texture.TextureFormat != TextureFormat.UNorm ||
                !textureLoadingContext.Context.Options.FixNormalMaps)
            {
                return;
            }

            var shader = Shader.Find("Hidden/TriLib/FixNormalMap");
            var material = new Material(shader);

            var textureFormat = GetTextureFormat(textureLoadingContext);
            var renderTextureFormat = GetRenderTextureFormat(textureFormat, false);
            var renderTexture = RenderTexture.GetTemporary(
                textureLoadingContext.UnityTexture.width,
                textureLoadingContext.UnityTexture.height,
                0,
                renderTextureFormat,
                RenderTextureReadWrite.Linear
            );

            if (!renderTexture.IsCreated())
            {
                renderTexture.useMipMap = false;
                renderTexture.autoGenerateMips = false;
            }

            material.SetTexture("_MainTex", textureLoadingContext.UnityTexture);
            Graphics.Blit(textureLoadingContext.UnityTexture, renderTexture, material);

            if (renderTexture.useMipMap)
            {
                renderTexture.GenerateMips();
            }

            var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);
            var texture2D = CreateTexture2DInternal(
                textureLoadingContext,
                renderTexture.width,
                renderTexture.height,
                renderTexture.graphicsFormat,
                textureCreationFlags,
                textureFormat
            );

            textureLoadingContext.Context.Allocations.Add(texture2D);
            CopyTextureCPU(renderTexture, texture2D, textureLoadingContext.Context.Options.GenerateMipmaps, false);
            textureLoadingContext.UnityTexture = texture2D;

            Graphics.SetRenderTarget(null);
            RenderTexture.ReleaseTemporary(renderTexture);

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        /// <summary>
        /// Rescales a non-power-of-two (NPOT) texture to the nearest power-of-two resolution if necessary,
        /// applying any desired mipmap generation or compression.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing the texture to be rescaled.
        /// </param>
        public static void FixNPOTTexture(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.UnityTexture == null)
            {
                return;
            }

            // Check if texture is non-power-of-two and needs resizing based on the import options.
            if (
                !textureLoadingContext.Context.Options.UseUnityNativeTextureLoader &&
                !IsPOT(textureLoadingContext) &&
                (
                    textureLoadingContext.Context.Options.GenerateMipmaps ||
                    textureLoadingContext.Context.Options.TextureCompressionQuality != TextureCompressionQuality.NoCompression
                ) ||
                textureLoadingContext.Context.Options.ForcePowerOfTwoTextures
            )
            {
                var textureResolution = Mathf.Max(
                    GetNextPOT(textureLoadingContext.Width),
                    GetNextPOT(textureLoadingContext.Height)
                );

                var oldTexture = (Texture2D)textureLoadingContext.UnityTexture;
                if (oldTexture.isReadable)
                {
                    oldTexture.Apply(false, false);
                }

                if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
                {
                    Debug.LogWarning(
                        $"Texture [{oldTexture.name ?? "No-Name"}] is non power-of-two ({oldTexture.width}x{oldTexture.height}). Rescaling it to ({textureResolution}x{textureResolution})"
                    );
                }

                var textureFormat = GetTextureFormat(textureLoadingContext);
                var renderTextureFormat = GetRenderTextureFormat(textureFormat, IsSRGBTexture(textureLoadingContext));
                var renderTexture = RenderTexture.GetTemporary(textureResolution, textureResolution, 0, renderTextureFormat);

                var newTextureFormat = GetGraphicsFormat(textureLoadingContext, textureFormat, IsSRGBTexture(textureLoadingContext));
                var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext, true);

                var newTexture = CreateTexture2DInternal(
                    textureLoadingContext,
                    textureResolution,
                    textureResolution,
                    newTextureFormat,
                    textureCreationFlags,
                    textureFormat
                );

                newTexture.name = textureLoadingContext.Texture.Name;

                Graphics.Blit(oldTexture, renderTexture);
                CopyTextureCPU(renderTexture, newTexture, true, false);
                RenderTexture.ReleaseTemporary(renderTexture);

                textureLoadingContext.Context.Allocations.Remove(oldTexture);
                textureLoadingContext.Context.Allocations.Add(newTexture);

                if (Application.isPlaying)
                {
                    Object.Destroy(oldTexture);
                }
                else
                {
                    Object.DestroyImmediate(oldTexture);
                }

                textureLoadingContext.UnityTexture = newTexture;
                textureLoadingContext.OriginalUnityTexture = newTexture;

                ApplyTexture2D(textureLoadingContext, true);
            }
        }

        /// <summary>
        /// Calculates the next power-of-two value for a given integer, at minimum 1.
        /// </summary>
        /// <param name="value">The value to convert to the next power-of-two.</param>
        /// <returns>The next power-of-two value.</returns>
        public static int GetNextPOT(int value)
        {
            if (value < 2)
            {
                return 1;
            }
            return (int)Mathf.Pow(2, (int)Mathf.Log(value - 1, 2) + 1);
        }

        /// <summary>
        /// Checks whether a given filename has a known image file extension.
        /// </summary>
        /// <param name="filename">The filename to check.</param>
        /// <returns>
        /// <see langword="true"/> if the filename extension matches a standard image type; otherwise <see langword="false"/>.
        /// </returns>
        public static bool IsValidTextureFileType(string filename)
        {
            var extension = FileUtils.GetFileExtension(filename, false);
            switch (extension)
            {
                case "bmp":
                case "png":
                case "jpg":
                case "jpeg":
                case "tga":
                case "gif":
                case "pic":
                case "ppm":
                case "pgm":
                case "psd":
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Loads a texture from the specified <see cref="TextureLoadingContext.Stream"/> using Unity's built-in loader LoadImage.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing the raw data stream and other texture parameters.
        /// </param>
        public static void LoadTexture2D(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.UnityTexture != null ||
                textureLoadingContext.TextureLoaded ||
                textureLoadingContext.Stream == null)
            {
                return;
            }

            var textureFormat = GetTextureFormat(textureLoadingContext);
            if (textureLoadingContext.Context.Options.TextureCompressionQuality != TextureCompressionQuality.NoCompression)
            {
                textureFormat = UnityEngine.TextureFormat.DXT5;
            }

            var format = GetGraphicsFormat(textureLoadingContext, textureFormat, IsSRGBTexture(textureLoadingContext));
            var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);

            var texture2D = CreateTexture2DInternal(
                textureLoadingContext,
                4,
                4,
                format,
                textureCreationFlags,
                textureFormat
            );

            var data = textureLoadingContext.Stream.ReadBytes();
            textureLoadingContext.Stream.Seek(0, SeekOrigin.Begin);
            var loaded = texture2D.LoadImage(data, false);
            texture2D.Apply(true, textureLoadingContext.Context.Options.MarkTexturesNoLongerReadable);

            if (loaded)
            {
                if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
                {
                    Debug.Log(
                        $"Loaded texture with {texture2D.width}x{texture2D.height} ({texture2D.graphicsFormat}). Flags: {textureCreationFlags}. Name: [{textureLoadingContext.Texture.Name ?? "Unnamed"}]. Original data: {ProcessUtils.SizeSuffix(data.Length)}"
                    );
                }

                texture2D.wrapModeU = textureLoadingContext.Texture.WrapModeU;
                texture2D.wrapModeV = textureLoadingContext.Texture.WrapModeV;
                texture2D.name = textureLoadingContext.Texture.Name;

                textureLoadingContext.Width = texture2D.width;
                textureLoadingContext.Height = texture2D.height;

                textureLoadingContext.Context.Allocations.Add(texture2D);
                textureLoadingContext.UnityTexture = textureLoadingContext.OriginalUnityTexture = texture2D;
                textureLoadingContext.TextureLoaded = true;
                textureLoadingContext.TextureCreated = true;
            }
            else
            {
                // If loading fails, destroy the new texture to free resources.
                if (Application.isPlaying)
                {
                    Object.Destroy(texture2D);
                }
                else
                {
                    Object.DestroyImmediate(texture2D);
                }
            }
        }

        /// <summary>
        /// Converts a specular workflow texture to an albedo texture by mixing the diffuse and specular channels as needed.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> that will store the newly generated albedo texture.
        /// </param>
        /// <param name="diffuseTexture">The original diffuse texture.</param>
        /// <param name="specularTexture">The specular texture to mix with the diffuse.</param>
        /// <param name="diffuseColor">The diffuse color if the diffuse texture is missing or partially defined.</param>
        /// <param name="specularColor">The specular color if the specular texture is missing or partially defined.</param>
        /// <param name="glossiness">The glossiness level to apply in the mixing.</param>
        /// <param name="outputBaseColor">
        /// <see langword="true"/> to generate a base color texture; <see langword="false"/> to generate a metallic-smoothness texture.
        /// </param>
        /// <param name="reassign">Not used.</param>
        public static void SpecularDiffuseToAlbedo(
            TextureLoadingContext textureLoadingContext,
            Texture diffuseTexture,
            Texture specularTexture,
            Vector4 diffuseColor,
            Vector4 specularColor,
            float glossiness,
            bool outputBaseColor,
            bool reassign = false
        )
        {
            var shader = Shader.Find("Hidden/TriLib/SpecularDiffuseToAlbedo");
            var material = new Material(shader);

            material.SetTexture("_DiffuseTexture", diffuseTexture);
            material.SetTexture("_SpecularTexture", specularTexture);
            material.SetColor("_DiffuseColor", diffuseColor);
            material.SetColor("_SpecularColor", specularColor);
            material.SetFloat("_Glossiness", glossiness);
            material.SetInt("_OutputBaseColor", outputBaseColor ? 1 : 0);
            material.mainTexture = textureLoadingContext.OriginalUnityTexture;

            var width = Mathf.Max(diffuseTexture.width);
            var height = Mathf.Max(diffuseTexture.height);
            var scaleFactor = textureLoadingContext.Context.Options.ConvertMaterialTexturesUsingHalfRes && width > 2 && height > 2
                ? 0.5f
                : 1f;

            var textureFormat = UnityEngine.TextureFormat.RGBA32;
            var renderTextureFormat = GetRenderTextureFormat(textureFormat, IsSRGBTexture(textureLoadingContext));
            RenderTexture renderTexture;

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                renderTexture = RenderTexture.GetTemporary(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                );

                if (!renderTexture.IsCreated())
                {
                    renderTexture.useMipMap = false;
                    renderTexture.autoGenerateMips = false;
                }
            }
            else
            {
                renderTexture = new RenderTexture(
                    (int)(width * scaleFactor),
                    (int)(height * scaleFactor),
                    0,
                    renderTextureFormat,
                    textureLoadingContext.Context.Options.LoadTexturesAsSRGB ? RenderTextureReadWrite.sRGB : RenderTextureReadWrite.Linear
                )
                {
                    useMipMap = false,
                    autoGenerateMips = false
                };
            }

            Graphics.Blit(null, renderTexture, material);

            textureLoadingContext.Width = width;
            textureLoadingContext.Height = height;

            if (renderTexture.useMipMap)
            {
                renderTexture.GenerateMips();
            }

            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                var textureCreationFlags = GetTextureCreationFlags(textureLoadingContext);
                var texture2D = CreateTexture2DInternal(
                    textureLoadingContext,
                    renderTexture.width,
                    renderTexture.height,
                    renderTexture.graphicsFormat,
                    textureCreationFlags,
                    textureFormat
                );

                textureLoadingContext.Context.Allocations.Add(texture2D);
                CopyTextureCPU(renderTexture, texture2D, textureLoadingContext.Context.Options.GenerateMipmaps, false);
                textureLoadingContext.UnityTexture = texture2D;
            }
            else
            {
                textureLoadingContext.Context.Allocations.Add(renderTexture);
                textureLoadingContext.UnityTexture = renderTexture;
            }

            textureLoadingContext.UnityTexture.name = $"{GetValidName(textureLoadingContext)}{(outputBaseColor ? "BaseColor" : "MetallicSmoothness")}";

            Graphics.SetRenderTarget(null);
            if (textureLoadingContext.Context.Options.ConvertTexturesAs2D)
            {
                RenderTexture.ReleaseTemporary(renderTexture);
            }

            if (Application.isPlaying)
            {
                Object.Destroy(material);
            }
            else
            {
                Object.DestroyImmediate(material);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// Returns the most appropriate <see cref="UnityEngine.TextureFormat"/> based on the metadata in <paramref name="textureLoadingContext"/>.
        /// </summary>
        /// <param name="textureLoadingContext">The context describing texture channels and bit depth.</param>
        /// <returns>A <see cref="UnityEngine.TextureFormat"/> suited to the texture data.</returns>
        private static UnityEngine.TextureFormat GetTextureFormat(TextureLoadingContext textureLoadingContext)
        {
            switch (textureLoadingContext.Components)
            {
                case 3 when textureLoadingContext.Has8BPC:
                    return UnityEngine.TextureFormat.RGB24;
                case 4 when textureLoadingContext.Has16BPC:
                    return UnityEngine.TextureFormat.RGBAHalf;
                case 4 when textureLoadingContext.Has8BPC:
                    return UnityEngine.TextureFormat.RGBA32;
                default:
                    if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
                    {
                        Debug.LogWarning("Used default RGBA format for texture.");
                    }
                    return UnityEngine.TextureFormat.RGBA32;
            }
        }

        /// <summary>
        /// Creates a new <see cref="Texture2D"/> using either <see cref="GraphicsFormat"/> or <see cref="UnityEngine.TextureFormat"/>,
        /// depending on <see cref="AssetLoaderOptions.GetCompatibleTextureFormat"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The context for the texture loading process, containing flags and other parameters.
        /// </param>
        /// <param name="width">The width of the new texture.</param>
        /// <param name="height">The height of the new texture.</param>
        /// <param name="graphicsFormat">The <see cref="GraphicsFormat"/> to use if <c>GetCompatibleTextureFormat</c> is enabled.</param>
        /// <param name="textureCreationFlags">The set of <see cref="TextureCreationFlags"/> to apply to the new texture.</param>
        /// <param name="textureFormat">
        /// A <see cref="UnityEngine.TextureFormat"/> fallback for creating the texture if 
        /// <c>GetCompatibleTextureFormat</c> is not enabled.
        /// </param>
        /// <returns>A newly created <see cref="Texture2D"/>.</returns>
        private static Texture2D CreateTexture2DInternal(
            TextureLoadingContext textureLoadingContext,
            int width,
            int height,
            GraphicsFormat graphicsFormat,
            TextureCreationFlags textureCreationFlags,
            UnityEngine.TextureFormat textureFormat
        )
        {
            // Choose between GraphicsFormat-based constructor or fallback to TextureFormat-based constructor.
            if (textureLoadingContext.Context.Options.GetCompatibleTextureFormat)
            {
                if (GetCompatibleFormatCallback != null)
                {
                    graphicsFormat = GetCompatibleFormatCallback(graphicsFormat);
                }
                return new Texture2D(width, height, graphicsFormat, textureCreationFlags);
            }
            return new Texture2D(
                width,
                height,
                textureFormat,
                textureCreationFlags.HasFlag(TextureCreationFlags.MipChain)
            );
        }

        /// <summary>
        /// Retrieves the appropriate <see cref="GraphicsFormat"/> for the given <paramref name="textureFormat"/>,
        /// optionally enabling sRGB if <paramref name="sRGB"/> is <c>true</c>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The context used to track the number of channels. If the chosen <see cref="GraphicsFormat"/> 
        /// includes alpha and <see cref="TextureLoadingContext.Components"/> is less than 4, it is updated to 4.
        /// </param>
        /// <param name="textureFormat">The <see cref="UnityEngine.TextureFormat"/> to convert from.</param>
        /// <param name="sRGB">Whether to use an sRGB format, if possible.</param>
        /// <returns>The <see cref="GraphicsFormat"/> that matches the given parameters.</returns>
        private static GraphicsFormat GetGraphicsFormat(TextureLoadingContext textureLoadingContext, UnityEngine.TextureFormat textureFormat, bool sRGB)
        {
            var format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, sRGB);
            // If the resulting format has alpha and we don't have enough components, update the component count.
            if (GraphicsFormatUtility.HasAlphaChannel(format) && textureLoadingContext.Components < 4)
            {
                textureLoadingContext.Components = 4;
            }
            return format;
        }

        /// <summary>
        /// Converts a given <see cref="UnityEngine.TextureFormat"/> into a <see cref="RenderTextureFormat"/>.
        /// </summary>
        /// <param name="textureFormat">The original texture format.</param>
        /// <param name="sRGB">Specifies whether sRGB should be used.</param>
        /// <returns>A corresponding <see cref="RenderTextureFormat"/>.</returns>
        private static RenderTextureFormat GetRenderTextureFormat(UnityEngine.TextureFormat textureFormat, bool sRGB)
        {
            var format = GraphicsFormatUtility.GetGraphicsFormat(textureFormat, sRGB);
            return GraphicsFormatUtility.GetRenderTextureFormat(format);
        }

        /// <summary>
        /// Calculates the <see cref="TextureCreationFlags"/> for the new texture, 
        /// based on mipmap generation, power-of-two constraints, and other factors.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The context containing the import options and texture metadata.
        /// </param>
        /// <param name="forceMipMap">
        /// If <c>true</c>, always enables mip chain generation, regardless of other factors.
        /// </param>
        /// <returns>The computed <see cref="TextureCreationFlags"/>.</returns>
        private static TextureCreationFlags GetTextureCreationFlags(TextureLoadingContext textureLoadingContext, bool forceMipMap = false)
        {
            var flags = TextureCreationFlags.None;

            if (
                forceMipMap ||
                (
                    textureLoadingContext.Context.Options.UseUnityNativeTextureLoader &&
                    textureLoadingContext.Context.Options.GenerateMipmaps
                ) ||
                (
                    textureLoadingContext.Context.Options.GenerateMipmaps &&
                    (
                        textureLoadingContext.Context.Options.TextureCompressionQuality == TextureCompressionQuality.NoCompression ||
                        (
                            !textureLoadingContext.Context.Options.ForcePowerOfTwoTextures &&
                            IsPOT(textureLoadingContext)
                        )
                    )
                )
            )
            {
                flags |= TextureCreationFlags.MipChain;
            }

            return flags;
        }

        /// <summary>
        /// Determines if the specified texture can be compressed, checking if both width and height are multiples of 4.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the texture width and height.</param>
        /// <returns>
        /// <see langword="true"/> if width and height are multiples of 4; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool CanCompress(TextureLoadingContext textureLoadingContext)
        {
            bool IsMultipleOfFour(int number) => (number & 3) == 0;
            return IsMultipleOfFour(textureLoadingContext.Width) && IsMultipleOfFour(textureLoadingContext.Height);
        }

        /// <summary>
        /// Checks if the given texture is a normal map based on its specified <see cref="TextureType"/> or TextureFormat.
        /// </summary>
        /// <param name="textureLoadingContext">The context describing texture types and formats.</param>
        /// <returns>
        /// <see langword="true"/> if the texture is determined to be a normal map; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsNormalMapTexture(TextureLoadingContext textureLoadingContext)
        {
            return textureLoadingContext.TextureType == TextureType.NormalMap ||
                   textureLoadingContext.Texture?.TextureFormat == TextureFormat.UNorm;
        }

        /// <summary>
        /// Checks if the dimensions stored in <paramref name="textureLoadingContext"/> represent a power-of-two (POT) texture.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing texture dimensions.</param>
        /// <returns><see langword="true"/> if both width and height are power-of-two; otherwise <see langword="false"/>.</returns>
        private static bool IsPOT(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.Width <= 1 || textureLoadingContext.Height <= 1)
            {
                return false;
            }
            return IsPOT(textureLoadingContext.Width) && IsPOT(textureLoadingContext.Height);
        }

        /// <summary>
        /// Checks if an integer is a power-of-two.
        /// </summary>
        /// <param name="value">The integer to evaluate.</param>
        /// <returns>
        /// <see langword="true"/> if <paramref name="value"/> is a power-of-two; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool IsPOT(int value)
        {
            return Mathf.CeilToInt(Mathf.Log(value, 2)) == Mathf.FloorToInt(Mathf.Log(value, 2));
        }

        /// <summary>
        /// Determines if a texture should be treated as sRGB based on its type and the <see cref="AssetLoaderOptions.LoadTexturesAsSRGB"/> option.
        /// Normal maps are never treated as sRGB.
        /// </summary>
        /// <param name="textureLoadingContext">The texture context to evaluate.</param>
        /// <returns>
        /// <see langword="true"/> if sRGB should be used; otherwise <see langword="false"/>.
        /// </returns>
        private static bool IsSRGBTexture(TextureLoadingContext textureLoadingContext)
        {
            return !IsNormalMapTexture(textureLoadingContext) && textureLoadingContext.Context.Options.LoadTexturesAsSRGB;
        }

        /// <summary>
        /// Retrieves a valid name for the texture, falling back to the material name if none is available.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing texture and material data.</param>
        /// <returns>A valid, non-empty string to use as a texture name.</returns>
        private static string GetValidName(TextureLoadingContext textureLoadingContext)
        {
            if (string.IsNullOrWhiteSpace(textureLoadingContext.Texture?.Name))
            {
                return string.IsNullOrWhiteSpace(textureLoadingContext.MaterialMapperContext.Material?.Name)
                    ? "unnamed"
                    : textureLoadingContext.MaterialMapperContext.Material.Name;
            }

            return textureLoadingContext.Texture.Name;
        }

        #endregion
    }
}
