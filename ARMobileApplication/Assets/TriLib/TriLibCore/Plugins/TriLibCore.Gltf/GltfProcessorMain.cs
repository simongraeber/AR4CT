#pragma warning disable 162, 414
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TriLibCore.Gltf.Reader;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;
using static TriLibCore.Utils.JsonParser;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private const int BYTE = 5120;
        private const uint CHUNKBIN = 0x004E4942;
        private const uint CHUNKJSON = 0x4E4F534A;
        private const int CLAMP_TO_EDGE = 33071;
        private const int CUBICSPLINE = 3;
        private const string EMBEDDEDGLTFBUFFER = "data:application/gltf-buffer;base64,";
        private const string EMBEDDEDJPEG = "data:image/jpeg;base64,";
        private const string EMBEDDEDOCTETSTREAM = "data:application/octet-stream;base64,";
        private const string EMBEDDEDPNG = "data:image/png;base64,";
        private const int FLOAT = 5126;
        private const uint GLTFHEADER = 0x46546C67;
        private const uint GLTFVERSION2 = 2;
        private const int LINE_LOOP = 2;
        private const int LINE_STRIP = 3;
        private const int LINEAR = 1;
        private const int LINES = 1;
        private const int MAT2 = 5;
        private const int MAT3 = 6;
        private const int MAT4 = 7;
        private const int MIRRORED_REPEAT = 33648;
        private const int POINTS = 0;
        private const int REPEAT = 10497;
        private const int ROTATION = 2;
        private const int SCALAR = 1;
        private const int SCALE = 3;
        private const int SHORT = 5122;
        private const int STEP = 2;
        private const int TRANSLATION = 1;
        private const int TRIANGLE_FAN = 6;
        private const int TRIANGLE_STRIP = 5;
        private const int TRIANGLES = 4;
        private const int UNSIGNED_BYTE = 5121;
        private const int UNSIGNED_INT = 5125;
        private const int UNSIGNED_SHORT = 5123;
        private const int VEC2 = 2;
        private const int VEC3 = 3;
        private const int VEC4 = 4;
        private const int WEIGHTS = 4;
        private readonly GltfReader _reader;
        private readonly byte[] _tempBytes8 = new byte[8];
        private readonly TemporaryString _temporaryString = new TemporaryString(39);
        private List<IAnimation> _animations;
        private StreamChunk[] _buffersData;
        private List<ICamera> _cameras;
        private List<IGeometryGroup> _geometryGroups;
        private List<ILight> _lights;
        private List<IMaterial> _materials;
        private Dictionary<int, GltfModel> _models;
        private bool _quantitized;
        private Dictionary<int, Matrix4x4>[] _skins;
        private Stream _stream;
        private List<ITexture> _textures;
        private bool _usesDraco;
        private bool _usesLights;
        private JsonValue accessors;
        private JsonValue animations;
        private JsonValue buffers;
        private JsonValue bufferViews;
        private JsonValue cameras;
        private JsonValue images;
        private JsonValue lights;
        private JsonValue materials;
        private JsonValue meshes;
        private JsonValue nodes;
        private JsonValue samplers;
        private JsonValue scenes;
        private JsonValue skins;
        private JsonValue textures;
        public GtlfProcessor(GltfReader reader)
        {
            _reader = reader;
        }

        public Stream ProcessImage(GltfTexture gltfTexture, JsonValue images, JsonValue buffers, JsonValue bufferViews, int imageIndex, out string filename)
        {
            var image = images.GetArrayValueAtIndex(imageIndex);
            var uri = image.GetChildWithKey(_uri_token);
            if (image.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                var bufferView = bufferViews.GetArrayValueAtIndex(bufferViewIndex);
                var bufferBytes = LoadBinaryBuffer(
                    buffers,
                    bufferView.GetChildValueAsInt(_buffer_token, _temporaryString, 0),
                    bufferView.GetChildValueAsInt(_byteOffset_token, _temporaryString, 0),
                    bufferView.GetChildValueAsInt(_byteLength_token, _temporaryString, 0));
                filename = null;
                return bufferBytes;
            }
            if (uri.StartsWith("data:image/"))
            {
                return !TryToExtractEmbeddedImageBytes(gltfTexture, uri, imageIndex, out filename, out var decoded) ? decoded : null;
            }
            var imageData = ExternalReferenceSolver(uri, true);
            if (imageData != null)
            {
                filename = null;
                return imageData;
            }
            else
            {
                filename = uri.ToString();
                return null;
            }
        }

        private static void DecodeBase64Data(IEnumerator<byte> input, string filename)
        {
            var base64decoder = new Base64Decoder();
            using (var fileWriter = new BinaryWriter(File.Create(filename)))
            {
                while (input.MoveNext())
                {
                    if (base64decoder.DecodeByte(input.Current, out var decoded))
                    {
                        fileWriter.Write(decoded);
                    }
                }
            }
        }

        private static Encoding FindEncoding(Stream stream)
        {
            var primaryEncoding = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            return primaryEncoding;
        }

        private static JsonValue LoadModel(Stream stream, GtlfProcessor processor)
        {
            var binaryFile = false;
            var magic = 0u;
            magic |= (uint)stream.ReadByte();
            magic |= (uint)stream.ReadByte() << 8;
            magic |= (uint)stream.ReadByte() << 16;
            magic |= (uint)stream.ReadByte() << 24;
            if (magic == GLTFHEADER)
            {
                binaryFile = true;
            }
            stream.Position = 0;
            var fileData = binaryFile ? ParseBinary(stream) : ParseText(stream);
            return new JsonParser(fileData, processor._reader.AssetLoaderContext.Options.UserPropertiesMapper != null).ParseRootValue();
        }

        private static BinaryReader ParseBinary(Stream stream)
        {
            var binaryReader = new BinaryReader(stream, FindEncoding(stream), true);
            ReadBinaryHeader(binaryReader);
            ReadBinaryChunk(binaryReader, CHUNKJSON);
            return binaryReader;
        }

        private static BinaryReader ParseText(Stream stream)
        {
            return new BinaryReader(stream, FindEncoding(stream), true);
        }

        private float ClampValueFloat(float data, JsonValue min, JsonValue max, int elementIndex, int accessorComponentType, bool accessorNormalized)
        {
            return data;
            //todo: clamping is not well implemented in some glTF2 files that don't fullfill the specs, removing for now
            if (min.Valid)
            {
                var minValue = (_minFloatValues.TryGetValue(min.GetHashCode(), out var minValueFromDic)) ? minValueFromDic : float.MinValue;//_minIntValues[min.GetHashCode()];
                data = Mathf.Max(minValue, data);
            }
            if (max.Valid)
            {
                var maxValue = (_maxFloatValues.TryGetValue(max.GetHashCode(), out var maxValueFromDic)) ? maxValueFromDic : float.MaxValue; //_maxIntValues[max.GetHashCode()];
                data = Mathf.Min(maxValue, data);
            }
            return data;
        }

        private int ClampValueInt(int data, JsonValue min, JsonValue max, int elementIndex)
        {
            return data;
            //todo: clamping is not well implemented in some glTF2 files that don't fullfill the specs, removing for now
            if (min.Valid)
            {
                var minValue = (_minIntValues.TryGetValue(min.GetHashCode(), out var minValueFromDic)) ? minValueFromDic : int.MinValue;//_minIntValues[min.GetHashCode()];
                //var minValue = min.GetArrayValueAtIndex(elementIndex).GetValueAsInt(_temporaryString);
                data = Mathf.Max(minValue, data);
            }
            if (max.Valid)
            {
                var maxValue = (_maxIntValues.TryGetValue(max.GetHashCode(), out var maxValueFromDic)) ? maxValueFromDic : int.MaxValue; //_maxIntValues[max.GetHashCode()];
                //var maxValue = max.GetArrayValueAtIndex(elementIndex).GetValueAsInt(_temporaryString);
                data = Mathf.Min(maxValue, data);
            }
            return data;
        }

        private Color ConvertColor(JsonValue value)
        {
            if (value.Count == 3)
            {
                return new Color(value.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(2).GetValueAsFloat(_temporaryString), 1f);
            }
            return new Color(value.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(2).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(3).GetValueAsFloat(_temporaryString));
        }

        private Matrix4x4 ConvertMatrix(JsonValue nodeMatrix)
        {
            var matrix = new Matrix4x4
            {
                [0] = nodeMatrix.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString),
                [1] = nodeMatrix.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString),
                [2] = nodeMatrix.GetArrayValueAtIndex(2).GetValueAsFloat(_temporaryString),
                [3] = nodeMatrix.GetArrayValueAtIndex(3).GetValueAsFloat(_temporaryString),
                [4] = nodeMatrix.GetArrayValueAtIndex(4).GetValueAsFloat(_temporaryString),
                [5] = nodeMatrix.GetArrayValueAtIndex(5).GetValueAsFloat(_temporaryString),
                [6] = nodeMatrix.GetArrayValueAtIndex(6).GetValueAsFloat(_temporaryString),
                [7] = nodeMatrix.GetArrayValueAtIndex(7).GetValueAsFloat(_temporaryString),
                [8] = nodeMatrix.GetArrayValueAtIndex(8).GetValueAsFloat(_temporaryString),
                [9] = nodeMatrix.GetArrayValueAtIndex(9).GetValueAsFloat(_temporaryString),
                [10] = nodeMatrix.GetArrayValueAtIndex(10).GetValueAsFloat(_temporaryString),
                [11] = nodeMatrix.GetArrayValueAtIndex(11).GetValueAsFloat(_temporaryString),
                [12] = nodeMatrix.GetArrayValueAtIndex(12).GetValueAsFloat(_temporaryString),
                [13] = nodeMatrix.GetArrayValueAtIndex(13).GetValueAsFloat(_temporaryString),
                [14] = nodeMatrix.GetArrayValueAtIndex(14).GetValueAsFloat(_temporaryString),
                [15] = nodeMatrix.GetArrayValueAtIndex(15).GetValueAsFloat(_temporaryString)
            };
            return matrix;
        }

        private Quaternion ConvertRotation(JsonValue nodeRotation)
        {
            return new Quaternion(
                nodeRotation.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString),
                nodeRotation.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString),
                nodeRotation.GetArrayValueAtIndex(2).GetValueAsFloat(_temporaryString),
                nodeRotation.GetArrayValueAtIndex(3).GetValueAsFloat(_temporaryString));
        }

        private GltfModel ConvertScene(int i)
        {
            var scene = scenes.GetArrayValueAtIndex(i);
            var sceneModel = new GltfModel();
            sceneModel.Name = scene.GetChildValueAsString(_name_token, _temporaryString);
            if (string.IsNullOrWhiteSpace(sceneModel.Name))
            {
                sceneModel.Name = "Scene";
            }
            sceneModel.LocalScale = Vector3.one;
            sceneModel.Visibility = true;
            if (scene.TryGetChildWithKey(_nodes_token, out var nodes))
            {
                sceneModel.Children = new List<IModel>(nodes.Count);
                for (var j = 0; j < nodes.Count; j++)
                {
                    var nodeIndex = nodes.GetArrayValueAtIndex(j).GetValueAsInt(_temporaryString);
                    sceneModel.Children.Add(ConvertModel(null, nodeIndex));
                }
            }
            return sceneModel;
        }

        private Vector2 ConvertVector2(JsonValue value)
        {
            return new Vector2(value.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString));
        }

        private Vector3 ConvertVector3(JsonValue value)
        {
            return new Vector3(value.GetArrayValueAtIndex(0).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(1).GetValueAsFloat(_temporaryString), value.GetArrayValueAtIndex(2).GetValueAsFloat(_temporaryString));
        }

        private StreamChunk ExternalReferenceSolver(JsonValue filename, bool ignoreFiles = false)
        {
            if (!filename.Valid)
            {
                if (_buffersData[0] == null)
                {
                    _stream.Position = 0;
                    return LoadBinaryBuffer(new StreamChunk(_stream));
                }
                return _buffersData[0];
            }
            if (ignoreFiles)
            {
                return null;
            }
            var stream = _reader.ReadExternalFile(filename.GetValueAsString(_temporaryString));
            _reader.SetupStream(ref stream);
            return stream == null ? null : new StreamChunk(stream);
        }
        private Vector3 TransformVector(Vector3 vertex)
        {
            return vertex * _reader.AssetLoaderContext.Options.ScaleFactor;
        }
        private bool TryToExtractEmbeddedImageBytes(GltfTexture gltfTexture, JsonValue uri, int imageIndex, out string filename, out Stream decoded)
        {
            if (!uri.Valid)
            {
                decoded = null;
                filename = null;
                return false;
            }
            JsonValue content;
            string localFilename;
            if (uri.StartsWith(EMBEDDEDPNG))
            {
                content = uri.AddOffset(EMBEDDEDPNG.Length);
                localFilename = $"{imageIndex}.png";
            }
            else if (uri.StartsWith(EMBEDDEDJPEG))
            {
                content = uri.AddOffset(EMBEDDEDJPEG.Length);
                localFilename = $"{imageIndex}.jpg";
            }
            else
            {
                decoded = null;
                filename = null;
                return false;
            }
            if (!FileUtils.TrySaveFileAtPersistentDataPath(
                    _reader.AssetLoaderContext,
                    gltfTexture.Name ?? imageIndex.ToString(),
                    localFilename,
                    content.GetByteEnumerator(),
                    out filename,
                    DecodeBase64Data
                ))
            {
                var data = content.ToString();
                decoded = new MemoryStream(Convert.FromBase64String(data));
                return false;
            }
            decoded = null;
            return true;
        }
    }
}