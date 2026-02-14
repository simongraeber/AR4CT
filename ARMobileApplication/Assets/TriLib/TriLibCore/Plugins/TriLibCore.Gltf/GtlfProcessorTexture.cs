using TriLibCore.Textures;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private GltfTexture ConvertTexture(int i)
        {
            var texture = textures.GetArrayValueAtIndex(i);
            if (texture.TryGetChildValueAsInt(_source_token , out var source, _temporaryString))
            {
                var image = images.GetArrayValueAtIndex(source);
                var uri = image.GetChildValueAsString(_uri_token , _temporaryString);
                var gltfTexture = new GltfTexture
                {
                    Name = texture.GetChildValueAsString(_name_token, _temporaryString) ?? 
                           image.GetChildValueAsString(_name_token, _temporaryString) ?? 
                           FileUtils.GetFilenameWithoutExtension(uri),
                    Index = source,
                    TextureId = 0
                };
                var textureData = ProcessImage(gltfTexture, images, buffers, bufferViews, source, out var filename);
                if (textureData != null)
                {
                    gltfTexture.DataStream = textureData;
                }
                else
                {
                    gltfTexture.Filename = filename;
                    gltfTexture.ResolveFilename(_reader.AssetLoaderContext);
                }
                return gltfTexture;
            }
            return null;
        }

        private void AddTextureProperty(JsonParser.JsonValue rawTexture, GltfMaterial gltfMaterial, string propertyName)
        {
            var index = rawTexture.GetChildValueAsInt(_index_token , _temporaryString, 0);
            var texture = _textures != null && index >= 0 && index <= _textures.Count - 1 ? (GltfTexture)_textures[index] : null;
            if (texture == null)
            {
                if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                {
                    Debug.Log($"Invalid texture index: {index}");
                }

                return;
            }

            var textureOffset = Vector2.zero;
            var textureTiling = Vector2.one;
            if (rawTexture.TryGetChildWithKey(_extensions_token , out var extensions))
            {
                if (extensions.TryGetChildWithKey(_KHR_texture_transform_token , out var textureTransformObj))
                {
                    var offset = textureTransformObj.GetChildWithKey(_offset_token );
                    var rotation = textureTransformObj.GetChildValueAsFloat(_rotation_token , _temporaryString, 0f);
                    var scale = textureTransformObj.GetChildWithKey(_scale_token );
                    if (_textures != null && index >= 0 && index <= _textures.Count - 1)
                    {
                        textureOffset = offset.Valid ? ConvertVector2(offset) : Vector2.zero;
                        if (offset.Valid)
                        {
                            textureOffset.y = -textureOffset.y;
                        }

                        if (rotation != 0f && _reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning($"Texture with id {index} uses UV rotation, which is not supported by TriLib yet.");
                        }

                        textureTiling = scale.Valid ? ConvertVector2(scale) : Vector2.one;
                        if (scale.Valid)
                        {
                            textureOffset.y -= textureTiling.y - 1f;
                        }
                    }
                }
            }

            var rawTextureOriginal = textures.GetArrayValueAtIndex(index);
            TextureWrapMode wrapModeU;
            TextureWrapMode wrapModeV;
            if (rawTextureOriginal.TryGetChildValueAsInt(_sampler_token , out var samplerIndex, _temporaryString))
            {
                var sampler = samplers.GetArrayValueAtIndex(samplerIndex);
                wrapModeU = ConvertWrapMode(sampler.GetChildValueAsInt(_wrapS_token , _temporaryString, REPEAT));
                wrapModeV = ConvertWrapMode(sampler.GetChildValueAsInt(_wrapT_token , _temporaryString, REPEAT));
            }
            else
            {
                wrapModeU = TextureWrapMode.Repeat;
                wrapModeV = TextureWrapMode.Repeat;
            }

            texture = texture.GetTextureWithTilingAndOffset(_textures, textureTiling, textureOffset, wrapModeU, wrapModeV, _reader.AssetLoaderContext);
            gltfMaterial.AddProperty(propertyName, texture, true);
        }

        private static TextureWrapMode ConvertWrapMode(int wrapMode)
        {
            switch (wrapMode)
            {
                case CLAMP_TO_EDGE:
                    return TextureWrapMode.Clamp;
                case MIRRORED_REPEAT:
                    return TextureWrapMode.Mirror;
                default:
                    return TextureWrapMode.Repeat;
            }
        }
    }
}