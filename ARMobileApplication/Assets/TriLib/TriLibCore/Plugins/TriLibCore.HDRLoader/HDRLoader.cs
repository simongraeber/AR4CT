using System.IO;
using UnityEngine;

namespace HDRLoader
{
    /// <summary>Represents a class used to load HDR (Radiance HDR) images.</summary>
    public static class HDRLoader
    {
        /// <summary>Creates a Texture from the given HDR Image Stream.</summary>
        /// <param name="stream">The Stream containing the HDR Image data.</param>
        /// <param name="gamma">The HDR image original gamma value.</param>
        /// <param name="exposure">The HDR image original exposure value.</param>
        /// <param name="linear">Pass `true` to this argument to load the HDR texture in linear colorspace.</param>
        /// <returns>The Texture2D created from teh HDR data.</returns>
        public static Texture2D Load(Stream stream, out float gamma, out float exposure, bool linear = false)
        {
            using (var binaryReader = new BinaryReader(stream))
            {
                var rgbe = new Rgbe();
                var header = Rgbe.ReadHeader(binaryReader);
                gamma = header.Gamma;
                exposure = header.Exposure;
                var rgbeData = new byte[header.Width * header.Height * 4];
                Rgbe.ReadPixelsRawRle(binaryReader, rgbeData, 0, header.Width, header.Height);
                var texture2D = new Texture2D(header.Width, header.Height, TextureFormat.RGBAFloat, false, linear);
                var rawTextureData = texture2D.GetRawTextureData<float>();
                for (var y = 0; y < header.Height; y++)
                {
                    for (var x = 0; x < header.Width; x++)
                    {
                        var upsideY = header.Height - y - 1;
                        rgbe.Rgbe2Float(rawTextureData, rgbeData, (y * header.Width + x) * 4, (upsideY * header.Width + x) * 4);
                    }
                }
                texture2D.Apply(false, true);
                return texture2D;
            }
        }
    }

}