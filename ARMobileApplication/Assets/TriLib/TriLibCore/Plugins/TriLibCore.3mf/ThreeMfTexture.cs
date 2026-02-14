using System.Collections.Generic;
using System.IO;
using IxMilia.ThreeMf;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using TextureFormat = TriLibCore.General.TextureFormat;

namespace TriLibCore.ThreeMf
{
    public class ThreeMfTexture : ITexture
    {
        public IList<ThreeMfTexture2DCoordinate> Coordinates;
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
            return 1f;
        }

        public void AddTexture(ITexture texture)
        {

        }

        public byte[] Data { get; set; }
        public Stream DataStream { get; set; }
        public string Filename { get; set; }
        public TextureWrapMode WrapModeU { get; set; } = TextureWrapMode.Repeat;
        public TextureWrapMode WrapModeV { get; set; } = TextureWrapMode.Repeat;
        public Vector2 Tiling { get; set; } = Vector2.one;
        public Vector2 Offset { get; set; }
        public int TextureId { get; set; }
        public string ResolvedFilename { get; set; }
        public bool IsValid => !string.IsNullOrEmpty(Filename);
        public bool HasAlpha { get; set; }
        public TextureFormat TextureFormat { get; set; }
        
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
    }
}