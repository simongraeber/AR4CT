#pragma warning disable 618
using System;
using System.IO;
using StbImageSharp;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Textures
{
    /// <summary>
    /// Provides methods for loading and processing textures (such as reading texture data, 
    /// creating <see cref="Texture2D"/> instances, and applying post-processing operations).
    /// </summary>
    public static class TextureLoaders
    {
        /// <summary>
        /// Attempts to resolve the <see cref="ITexture.Filename"/> by searching in 
        /// <paramref name="assetLoaderContext"/>. This may update <see cref="ITexture.ResolvedFilename"/> 
        /// if a matching file is found.
        /// </summary>
        /// <param name="texture">The texture to resolve.</param>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> that provides the base path and other loading options.
        /// </param>
        public static void ResolveFilename(this ITexture texture, AssetLoaderContext assetLoaderContext)
        {
            if (texture.Filename != null && texture.ResolvedFilename == null)
            {
                texture.ResolvedFilename = FileUtils.FindFile(
                    assetLoaderContext.BasePath,
                    texture.Filename,
                    assetLoaderContext.Options.SearchTexturesRecursively
                );
            }
        }

        /// <summary>
        /// Loads a Unity texture from the data provided in the <paramref name="textureLoadingContext"/>.
        /// This method attempts to read raw or embedded texture data, then either uses Unity’s native 
        /// LoadImage or StbImageSharp to decode the image 
        /// into a <see cref="Texture2D"/>.
        /// </summary>
        /// <param name="textureLoadingContext">
        /// The <see cref="TextureLoadingContext"/> containing streams, raw data, 
        /// and options for texture creation.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if the texture was successfully loaded; otherwise, <see langword="false"/>.
        /// </returns>
        public static bool LoadTexture(TextureLoadingContext textureLoadingContext)
        {
            if (!GetTextureDataStream(textureLoadingContext))
            {
                return false;
            }

            // If the texture is manually provided as raw data (not in a file or embedded stream):
            if (textureLoadingContext.RawData != null)
            {
                CreateAndLoadRawData(
                    textureLoadingContext,
                    textureLoadingContext.Width,
                    textureLoadingContext.Height,
                    textureLoadingContext.RawData,
                    textureLoadingContext.CreationBitsPerChannel,
                    textureLoadingContext.Components
                );
            }
            else
            {
                // If no raw data is supplied, either load with Unity’s native loader or using StbImageSharp.
                if (textureLoadingContext.Context.Options.UseUnityNativeTextureLoader)
                {
                    textureLoadingContext.Components = 4;
                    TextureUtils.LoadTexture2D(textureLoadingContext);
                }
                else
                {
                    StbLoadFromContext(textureLoadingContext);
                }
            }

            // Clean up the stream if it is disposable.
            textureLoadingContext.Stream.TryToDispose();

            if (!textureLoadingContext.TextureLoaded && textureLoadingContext.Context.Options.ShowLoadingWarnings)
            {
                Debug.LogWarning(
                    $"Could not load texture [{textureLoadingContext.Texture.Name ?? textureLoadingContext.Texture.Filename ?? "No-name"}]"
                );
                return false;
            }
            return true;
        }

        /// <summary>
        /// Retrieves the texture data stream from the context, either by calling 
        /// registered ITextureMapper instances, opening a file based 
        /// on <see cref="ITexture.ResolvedFilename"/>, or using embedded data.
        /// </summary>
        /// <param name="textureLoadingContext">The context holding texture info and streams.</param>
        /// <returns>
        /// <see langword="true"/> if a valid data stream is available; otherwise, <see langword="false"/>.
        /// </returns>
        private static bool GetTextureDataStream(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.Texture == null)
            {
                return false;
            }

            // Check if any user-defined texture mappers can handle this texture.
            if (textureLoadingContext.Context.Options.TextureMappers != null)
            {
                Array.Sort(
                    textureLoadingContext.Context.Options.TextureMappers,
                    (a, b) => a.CheckingOrder > b.CheckingOrder ? -1 : 1
                );

                foreach (var textureMapper in textureLoadingContext.Context.Options.TextureMappers)
                {
                    textureMapper.Map(textureLoadingContext);
                    if (textureLoadingContext.HasValidData)
                    {
                        break;
                    }
                }
            }

            // If no valid data yet, try loading from file.
            if (!textureLoadingContext.HasValidData)
            {
                if (textureLoadingContext.Texture.ResolvedFilename != null)
                {
                    textureLoadingContext.Stream = File.OpenRead(textureLoadingContext.Texture.ResolvedFilename);
                }

                // If still no valid data, consider embedded data streams.
                if (!textureLoadingContext.HasValidData && textureLoadingContext.HasValidEmbeddedDataStream)
                {
                    textureLoadingContext.Stream = textureLoadingContext.Texture.DataStream;
                }
            }
            return textureLoadingContext.HasValidData;
        }

        /// <summary>
        /// Scans the texture’s pixel data for alpha values to detect if the texture contains 
        /// any non-opaque pixels. Updates <see cref="TextureLoadingContext.HasAlpha"/> accordingly.
        /// </summary>
        /// <param name="textureLoadingContext">The context with texture data and format details.</param>
        public static void ScanForAlphaPixels(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.UnityTexture == null || textureLoadingContext.Components != 4)
            {
                return;
            }

            // Check if we've already determined alpha usage for this texture.
            if (!textureLoadingContext.Context.TexturesWithAlphaChecked.TryGetValue(textureLoadingContext.UnityTexture, out var hasAlpha))
            {
                // If using the Unity native texture loader, scanning is not possible.
                if (textureLoadingContext.Context.Options.UseUnityNativeTextureLoader)
                {
                    if (textureLoadingContext.Context.Options.ShowLoadingWarnings)
                    {
                        Debug.LogWarning(
                            "Cannot scan textures for alpha pixels when using the Unity native texture loader. " +
                            "Disable AssetLoaderOptions.UseUnityNativeTextureLoader to use this feature."
                        );
                    }
                    return;
                }

                // For 16-bit-per-channel textures, search for any pixel whose alpha is less than <see cref="ushort.MaxValue"/>.
                if (textureLoadingContext.Has16BPC)
                {
                    var data = textureLoadingContext.ByteData;
                    if (data != null && data.Length >= 8)
                    {
                        for (var i = 6; i + 1 < data.Length; i += 8) // alpha starts at byte 6 for RGBA16 (R=0, G=2, B=4, A=6)
                        {
                            var alpha = (ushort)(data[i] | (data[i + 1] << 8));
                            if (alpha < ushort.MaxValue)
                            {
                                hasAlpha = true;
                                break;
                            }
                        }
                    }
                }
                else
                {
                    // For 8-bit-per-channel textures, search for any pixel whose alpha is less than 255.
                    var data = textureLoadingContext.ByteData;
                    if (data != null && data.Length >= 4)
                    {
                        for (var i = 3; i < data.Length; i += 4)
                        {
                            if (data[i] < byte.MaxValue)
                            {
                                hasAlpha = true;
                                break;
                            }
                        }
                    }
                }

                textureLoadingContext.Context.TexturesWithAlphaChecked.Add(textureLoadingContext.UnityTexture, hasAlpha);
            }

            // Update the context with our alpha detection result.
            textureLoadingContext.HasAlpha |= hasAlpha;
        }

        /// <summary>
        /// Loads a texture using <see cref="StbImageSharp"/> from the current stream in <paramref name="textureLoadingContext"/>.
        /// Flips the image vertically if needed, and sets the context’s dimension/format fields.
        /// </summary>
        /// <param name="textureLoadingContext">The context holding stream data and loading options.</param>
        private static void StbLoadFromContext(TextureLoadingContext textureLoadingContext)
        {
            try
            {
                // Flip images vertically by default.
                StbImage.stbi_set_flip_vertically_on_load(1);

                // If not loading the entire texture at once, only request RGBA from StbImageSharp.
                if (!textureLoadingContext.Context.Options.LoadTexturesAtOnce)
                {
                    var result = ImageResult.FromStream(
                        textureLoadingContext.Stream,
                        ColorComponents.RedGreenBlueAlpha
                    );
                    if (result.Data != null)
                    {
                        LoadRawData(textureLoadingContext, result.Data);
                    }
                }
                else
                {
                    // If loading all texture data at once, respect whether alpha channel is enforced or not.
                    var colorComp = textureLoadingContext.Context.Options.EnforceAlphaChannelTextures
                        ? ColorComponents.RedGreenBlueAlpha
                        : ColorComponents.Default;
                    var result = ImageResult.FromStream(textureLoadingContext.Stream, colorComp);
                    if (result.Data != null)
                    {
                        CreateAndLoadRawData(
                            textureLoadingContext,
                            result.Width,
                            result.Height,
                            result.Data,
                            8,
                            GetComponentsCount(result.Comp)
                        );
                    }
                }
            }
            catch (Exception)
            {
                // If loading fails, log a warning if requested.
                if (textureLoadingContext.Context.Options.ShowLoadingWarnings &&
                    !textureLoadingContext.TextureLoaded)
                {
                    Debug.LogWarning(
                        $"Could not load texture [{textureLoadingContext.Texture.Name ?? textureLoadingContext.Texture.Filename ?? "No-name"}]"
                    );
                }
            }
        }

        /// <summary>
        /// Creates a <see cref="Texture2D"/> using <paramref name="width"/> x <paramref name="height"/> 
        /// with <paramref name="components"/> channels, and then copies the <paramref name="data"/> into it.
        /// </summary>
        /// <param name="textureLoadingContext">The context to update.</param>
        /// <param name="width">The width of the new texture.</param>
        /// <param name="height">The height of the new texture.</param>
        /// <param name="data">The raw pixel data to copy.</param>
        /// <param name="bitsPerChannel">The number of bits in each color channel (e.g., 8 or 16).</param>
        /// <param name="components">The number of channels in the texture (1–4).</param>
        private static void CreateAndLoadRawData(
            TextureLoadingContext textureLoadingContext,
            int width,
            int height,
            byte[] data,
            int bitsPerChannel,
            int components)
        {
            textureLoadingContext.Width = width;
            textureLoadingContext.Height = height;
            textureLoadingContext.ByteData = data;
            textureLoadingContext.CreationBitsPerChannel = bitsPerChannel;
            textureLoadingContext.Components = components;

            CreateTextureInternal(textureLoadingContext);
            LoadRawData(textureLoadingContext, data);
        }

        /// <summary>
        /// Copies the <paramref name="data"/> array into <see cref="TextureLoadingContext.UnityTexture"/>, 
        /// marking the texture as loaded.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the Unity texture.</param>
        /// <param name="data">The pixel data to copy into the texture.</param>
        private static void LoadRawData(TextureLoadingContext textureLoadingContext, byte[] data)
        {
            ((Texture2D)textureLoadingContext.UnityTexture).SetPixelData(data, 0);
            textureLoadingContext.TextureLoaded = true;
        }

        /// <summary>
        /// Converts a <see cref="ColorComponents"/> value to its integer equivalent number of channels (1–4).
        /// </summary>
        /// <param name="colorComponents">The color component descriptor from StbImageSharp.</param>
        /// <returns>The integer number of channels (e.g. 1, 2, 3, or 4).</returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="colorComponents"/> is an unexpected value.
        /// </exception>
        private static int GetComponentsCount(ColorComponents colorComponents)
        {
            switch (colorComponents)
            {
                case ColorComponents.Grey:
                    return 1;
                case ColorComponents.GreyAlpha:
                    return 2;
                case ColorComponents.RedGreenBlue:
                    return 3;
                case ColorComponents.RedGreenBlueAlpha:
                    return 4;
                default:
                    throw new ArgumentOutOfRangeException(nameof(colorComponents), colorComponents, null);
            }
        }

        /// <summary>
        /// Writes the contents of <paramref name="stream"/> into a file at <paramref name="destinationFile"/> 
        /// using the specified buffer size and file sharing mode.
        /// </summary>
        /// <param name="stream">The source stream to read from.</param>
        /// <param name="destinationFile">The file path to write to.</param>
        /// <param name="bufferSize">The size (in bytes) of the buffer used while copying the stream.</param>
        /// <param name="mode">
        /// A <see cref="FileMode"/> specifying how the file is opened or created. 
        /// Defaults to <see cref="FileMode.OpenOrCreate"/>.
        /// </param>
        /// <param name="access">
        /// A <see cref="FileAccess"/> specifying the level of access permitted. 
        /// Defaults to <see cref="FileAccess.ReadWrite"/>.
        /// </param>
        /// <param name="share">
        /// A <see cref="FileShare"/> specifying the type of access other processes have to the file. 
        /// Defaults to <see cref="FileShare.ReadWrite"/>.
        /// </param>
        public static void WriteToFile(Stream stream, string destinationFile, int bufferSize = 4096,
            FileMode mode = FileMode.OpenOrCreate, FileAccess access = FileAccess.ReadWrite, FileShare share = FileShare.ReadWrite)
        {
            using (var destinationFileStream = new FileStream(destinationFile, mode, access, share))
            {
                while (stream.Position < stream.Length)
                {
                    destinationFileStream.WriteByte((byte)stream.ReadByte());
                }
            }
        }

        /// <summary>
        /// Applies any post-processing to the texture as dictated by the <see cref="IMaterial"/> 
        /// in <see cref="TextureLoadingContext.MaterialMapperContext"/>, then marks the texture as processed.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the texture to post-process.</param>
        /// <returns>
        /// A value indicating whether the material actually performed any post-processing.
        /// </returns>
        public static bool PostProcessTexture(TextureLoadingContext textureLoadingContext)
        {
            if (textureLoadingContext.TextureProcessed)
            {
                return false;
            }

            if (textureLoadingContext.MaterialMapperContext.VirtualMaterial != null &&
                textureLoadingContext.TextureType == TextureType.Diffuse)
            {
                textureLoadingContext.MaterialMapperContext.VirtualMaterial.HasAlpha |= textureLoadingContext.HasAlpha;
            }

            var result = textureLoadingContext.MaterialMapperContext.Material.PostProcessTexture(textureLoadingContext);
            textureLoadingContext.TextureProcessed = true;
            return result;
        }

        /// <summary>
        /// Creates a texture structure (e.g. <see cref="Texture2D"/>) without fully loading pixel data, 
        /// based on basic information (width, height, etc.) found via <see cref="StbImageSharp.ImageInfo"/>.
        /// </summary>
        /// <param name="textureLoadingContext">The context that will contain the created texture.</param>
        public static void CreateTexture(TextureLoadingContext textureLoadingContext)
        {
            if (!GetTextureDataStream(textureLoadingContext))
            {
                return;
            }

            var imageInfo = ImageInfo.FromStream(textureLoadingContext.Stream);
            if (imageInfo != null)
            {
                textureLoadingContext.Width = imageInfo.Value.Width;
                textureLoadingContext.Height = imageInfo.Value.Height;
                textureLoadingContext.Components = (int)imageInfo.Value.ColorComponents;
                textureLoadingContext.CreationBitsPerChannel = imageInfo.Value.BitsPerChannel;
                CreateTextureInternal(textureLoadingContext);
            }
        }

        /// <summary>
        /// Creates a <see cref="Texture2D"/> in <paramref name="textureLoadingContext"/>, respecting the 
        /// <see cref="AssetLoaderOptions.MaxTexturesResolution"/> setting. 
        /// Called internally once width/height are known.
        /// </summary>
        /// <param name="textureLoadingContext">The context to update with the newly created texture.</param>
        public static void CreateTextureInternal(TextureLoadingContext textureLoadingContext)
        {
            if (
                textureLoadingContext.Context.Options.MaxTexturesResolution == 0 ||
                (
                    textureLoadingContext.Width <= textureLoadingContext.Context.Options.MaxTexturesResolution &&
                    textureLoadingContext.Height <= textureLoadingContext.Context.Options.MaxTexturesResolution
                )
            )
            {
                TextureUtils.CreateTexture2D(textureLoadingContext);
            }
        }

        /// <summary>
        /// Applies (uploads) the <see cref="Texture2D"/> data to the GPU, optionally treating the texture 
        /// as procedural if requested. See <see cref="TextureUtils.ApplyTexture2D"/> for details.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the texture to apply.</param>
        /// <param name="procedural">
        /// <see langword="true"/> to treat the texture as procedural; <see langword="false"/> otherwise.
        /// </param>
        public static void ApplyTexture(TextureLoadingContext textureLoadingContext, bool procedural)
        {
            TextureUtils.ApplyTexture2D(textureLoadingContext, procedural);
        }

        /// <summary>
        /// Fixes a non-power-of-two (NPOT) texture by rescaling it to the nearest power-of-two resolution if needed.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the texture to fix.</param>
        public static void FixNPOTTexture(TextureLoadingContext textureLoadingContext)
        {
            TextureUtils.FixNPOTTexture(textureLoadingContext);
        }

        /// <summary>
        /// If the texture in <paramref name="textureLoadingContext"/> is recognized as a normal map, 
        /// adjusts its channels to properly match Unity’s normal map expectations.
        /// </summary>
        /// <param name="textureLoadingContext">The context containing the texture to fix.</param>
        public static void FixNormalMap(TextureLoadingContext textureLoadingContext)
        {
            TextureUtils.FixNormalMap(textureLoadingContext);
        }
    }
}
