using System.Collections.Generic;
using System.IO;
using TriLibCore.Interfaces;
using TriLibCore.Textures;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.Gltf
{
    public class GltfTexture : ITexture
    {
        public string Name { get; set; }

        public bool Used { get; set; }

        public ITexture GetSubTexture(int index)
        {
            return this;
        }

        public int GetSubTextureCount()
        {
            return 0;
        }

        public float GetWeight(int index)
        {
            return 0f;
        }

        public void AddTexture(ITexture texture)
        {

        }

        public byte[] Data
        {

            get;
            set;
        }

        public Stream DataStream { get; set; }

        public string Filename { get; set; }
        public TextureWrapMode WrapModeU { get; set; } = TextureWrapMode.Repeat;
        public TextureWrapMode WrapModeV { get; set; } = TextureWrapMode.Repeat;
        public Vector2 Tiling { get; set; } = Vector2.one;
        public Vector2 Offset { get; set; }
        public int TextureId { get; set; }
        public string ResolvedFilename { get; set; }
        public int Index { get; set; }

        public bool IsValid => !string.IsNullOrEmpty(Filename) || (DataStream != null && DataStream.Length > 0);
        public bool HasAlpha { get; set; }
        public TextureFormat TextureFormat { get; set; }

        private readonly IList<GltfTexture> TextureVariations = new List<GltfTexture>();
        
        public GltfTexture()
        {
            TextureVariations.Add(this);
        }

        public bool Equals(ITexture other)
        {
            return TextureComparators.TextureEquals(this, other);
        }

        public override bool Equals(object obj)
        {
            return TextureComparators.Equals(this, obj);
        }

        public override int GetHashCode()
        {
            return TextureComparators.GetHashCode(this);
        }

        public GltfTexture GetTextureWithTilingAndOffset(IList<ITexture> textures, Vector2 textureTiling, Vector2 textureOffset, TextureWrapMode wrapU, TextureWrapMode wrapV, AssetLoaderContext assetLoaderContext)
        {
            for (var i = 0; i < TextureVariations.Count; i++)
            {
                var variation = TextureVariations[i];
                if (variation.Tiling == textureTiling &&
                    variation.Offset == textureOffset &&
                    variation.WrapModeU == wrapU &&
                    variation.WrapModeV == wrapV
                )
                {
                    return variation;
                }
            }

            var newTexture = new GltfTexture();
            newTexture.Name = Name;
            newTexture.Filename = Filename;
            newTexture.ResolveFilename(assetLoaderContext);
            newTexture.DataStream = DataStream;
            newTexture.WrapModeU = WrapModeU;
            newTexture.WrapModeV = WrapModeV;
            newTexture.TextureId = TextureId;
            newTexture.TextureFormat = TextureFormat;
            newTexture.Index = textures.Count;
            newTexture.Tiling = textureTiling;
            newTexture.Offset = textureOffset;
            TextureVariations.Add(newTexture);
            textures.Add(newTexture);
            return newTexture;
        }
    }
}