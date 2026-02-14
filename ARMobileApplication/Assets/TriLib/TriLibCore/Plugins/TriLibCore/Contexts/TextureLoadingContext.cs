using System;
using System.IO;
using TriLibCore.General;
using TriLibCore.Interfaces;
using Unity.Collections;
using UnityEngine;

namespace TriLibCore
{
    /// <summary>
    /// Contains data and state information for loading and processing a texture as part
    /// of TriLib's model import pipeline. This context holds both the source texture data
    /// (via <see cref="TextureDataContext"/>) and metadata (dimensions, component count, raw data)
    /// used to create a Unity <see cref="Texture"/>.
    /// </summary>
    public class TextureLoadingContext : IAssetLoaderContext, IAwaitable
    {
        /// <summary>
        /// Gets or sets the type of texture being loaded (e.g., Diffuse, Normal, Specular).
        /// </summary>
        public TextureType TextureType;

        /// <summary>
        /// Gets or sets the <see cref="TextureDataContext"/> that stores the original texture
        /// data, including the source <see cref="ITexture"/>, dimensions, raw stream, and component count.
        /// </summary>
        public TextureDataContext TextureDataContext;

        /// <summary>
        /// Gets or sets the <see cref="MaterialMapperContext"/> associated with this texture load.
        /// This context holds references to the virtual material and other material-specific properties.
        /// </summary>
        public MaterialMapperContext MaterialMapperContext;

        /// <summary>
        /// Indicates whether the texture has been processed.
        /// </summary>
        public bool TextureProcessed;

        /// <summary>
        /// Gets or sets a value indicating whether the texture loading process has completed.
        /// </summary>
        public bool Completed { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="AssetLoaderContext"/> containing the overall model loading data.
        /// </summary>
        public AssetLoaderContext Context { get; set; }

        /// <summary>
        /// Gets or sets the TriLib <see cref="ITexture"/> reference stored in the 
        /// <see cref="TextureDataContext"/>.
        /// </summary>
        public ITexture Texture
        {
            get { return TextureDataContext.Texture; }
            set { TextureDataContext.Texture = value; }
        }

        /// <summary>
        /// Gets or sets the Unity texture instance that will eventually be created from the loaded data.
        /// </summary>
        public Texture UnityTexture { get; set; }

        /// <summary>
        /// Gets or sets the source stream for the texture data, stored in the underlying <see cref="TextureDataContext"/>.
        /// </summary>
        public Stream Stream
        {
            get { return TextureDataContext.Stream; }
            set { TextureDataContext.Stream = value; }
        }

        /// <summary>
        /// (Obsolete) The number of bytes per pixel used during texture creation.
        /// </summary>
        [Obsolete("Please use CreationBitsPerChannel instead.")]
        public int CreationBytesPerPixel;

        /// <summary>
        /// Gets or sets the number of bits per channel (e.g., 8 or 16) from the original image data.
        /// </summary>
        public int CreationBitsPerChannel;

        /// <summary>
        /// Gets or sets the raw texture data as an array of bytes, which can be used
        /// to bypass the standard TriLib image loaders.
        /// <remarks>
        /// When setting this field, ensure that the corresponding "Width", "Height", "Components", 
        /// and "CreationBitsPerChannel" fields are also filled.
        /// </remarks>
        /// </summary>
        public byte[] RawData;

        /// <summary>
        /// The texture data coming from StbImage.
        /// </summary>
        public byte[] ByteData;

        /// <summary>
        /// Gets a value indicating whether the texture data is 8 bits per channel.
        /// </summary>
        public bool Has8BPC => CreationBitsPerChannel == 8;

        /// <summary>
        /// Gets a value indicating whether the texture data is 16 bits per channel.
        /// </summary>
        public bool Has16BPC => CreationBitsPerChannel == 16;

        /// <summary>
        /// Gets the 16-bit raw texture data from the Unity texture when it has an RGBA16 format.
        /// If the texture is not 16 bits per channel, returns the default value.
        /// </summary>
        [Obsolete("This data does not reflect the raw RGB/RGBA data.")]
        public NativeArray<ushort> Data16 => Has16BPC ? ((Texture2D)UnityTexture).GetRawTextureData<ushort>() : default;

        /// <summary>
        /// Gets the 8-bit raw texture data from the Unity texture when it has an RGBA8 format.
        /// If the texture is not 8 bits per channel, returns the default value.
        /// </summary>
        [Obsolete("This data does not reflect the raw RGB/RGBA data.")]
        public NativeArray<byte> Data => Has8BPC ? ((Texture2D)UnityTexture).GetRawTextureData<byte>() : default;

        /// <summary>
        /// Gets or sets a value indicating whether the source texture uses its alpha channel.
        /// </summary>
        public bool HasAlpha
        {
            get { return TextureDataContext.HasAlpha; }
            set { TextureDataContext.HasAlpha = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Unity texture has been successfully loaded.
        /// </summary>
        public bool TextureLoaded
        {
            get { return TextureDataContext.TextureLoaded; }
            set { TextureDataContext.TextureLoaded = value; }
        }

        /// <summary>
        /// Gets or sets the width of the texture in pixels.
        /// </summary>
        public int Width
        {
            get { return TextureDataContext.Width; }
            set { TextureDataContext.Width = value; }
        }

        /// <summary>
        /// Gets or sets the height of the texture in pixels.
        /// </summary>
        public int Height
        {
            get { return TextureDataContext.Height; }
            set { TextureDataContext.Height = value; }
        }

        /// <summary>
        /// Gets or sets the number of color components in the texture (e.g., 3 for RGB or 4 for RGBA).
        /// If <see cref="AssetLoaderOptions.UseUnityNativeTextureLoader"/> is false and 
        /// <see cref="AssetLoaderOptions.EnforceAlphaChannelTextures"/> is true, returns 4.
        /// Otherwise, it returns the value from <see cref="TextureDataContext.Components"/>.
        /// </summary>
        public int Components
        {
            get
            {
                if (!Context.Options.UseUnityNativeTextureLoader && Context.Options.EnforceAlphaChannelTextures)
                {
                    return 4;
                }
                return TextureDataContext.Components;
            }
            set { TextureDataContext.Components = value; }
        }

        /// <summary>
        /// Gets or sets the original Unity texture created from the loaded texture data.
        /// </summary>
        public Texture OriginalUnityTexture
        {
            get { return TextureDataContext.OriginalUnityTexture; }
            set { TextureDataContext.OriginalUnityTexture = value; }
        }

        /// <summary>
        /// Gets or sets a value indicating whether the Unity texture has been created.
        /// </summary>
        public bool TextureCreated
        {
            get { return TextureDataContext.TextureCreated; }
            set { TextureDataContext.TextureCreated = value; }
        }

        /// <summary>
        /// Gets a value indicating whether the texture embedded data stream is valid.
        /// </summary>
        /// <remarks>
        /// A stream is considered valid if it is not <c>null</c>, can be read, and has a length greater than zero.
        /// </remarks>
        public bool HasValidEmbeddedDataStream => Texture.DataStream != null && Texture.DataStream.CanRead && Texture.DataStream.Length > 0;

        /// <summary>
        /// Gets a value indicating whether either a texture stream or raw texture data is available.
        /// </summary>
        public bool HasValidData => Stream != null || RawData != null;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureLoadingContext"/> class.
        /// </summary>
        /// <param name="createTextureData">
        /// If <c>true</c>, a new <see cref="TextureDataContext"/> is created and assigned; 
        /// otherwise, it remains uninitialized.
        /// </param>
        public TextureLoadingContext(bool createTextureData = true)
        {
            if (createTextureData)
            {
                TextureDataContext = new TextureDataContext()
                {
                    Context = null
                };
            }
        }
    }
}
