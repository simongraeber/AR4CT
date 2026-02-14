using System;
using System.IO;
using TriLibCore.Utils;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private void GetBufferViewData(int bufferViewIndex, int accessorByteOffset, out StreamChunk bufferData, out int index, out int bufferViewByteStride, out int bufferViewByteOffset)
        {
            var bufferView = bufferViews.GetArrayValueAtIndex(bufferViewIndex);
            bufferView.TryGetChildValueAsInt(_byteStride_token , out bufferViewByteStride, _temporaryString);
            bufferView.TryGetChildValueAsInt(_byteOffset_token , out bufferViewByteOffset, _temporaryString);
            bufferData = _buffersData[bufferView.GetChildValueAsInt(_buffer_token , _temporaryString, 0)];
            index = accessorByteOffset + bufferViewByteOffset;
        }

        private static void ReadBinaryHeader(BinaryReader binaryReader)
        {
            var magic = binaryReader.ReadUInt32();
            if (magic != GLTFHEADER)
            {
                throw new InvalidDataException($"Unexpected magic number: {magic}");
            }

            var version = binaryReader.ReadUInt32();
            if (version != GLTFVERSION2)
            {
                throw new InvalidDataException($"Unknown version number: {version}");
            }

            var length = binaryReader.ReadUInt32();
            var fileLength = binaryReader.BaseStream.Length;
            if (length != fileLength)
            {
                throw new InvalidDataException($"The specified length of the file ({length}) is not equal to the actual length of the file ({fileLength}).");
            }
        }

        private static StreamChunk ReadBinaryChunk(BinaryReader binaryReader, uint format)
        {
            while (true)
            {
                var chunkLength = binaryReader.ReadUInt32();
                var chunkFormat = binaryReader.ReadUInt32();
                if (chunkFormat == format)
                {
                    return new StreamChunk(binaryReader.BaseStream, (int)chunkLength);
                }
                binaryReader.BaseStream.Position += chunkLength;
            }
        }

        private static StreamChunk LoadBinaryBuffer(StreamChunk stream)
        {
            var binaryReader = new BinaryReader(stream);
            ReadBinaryHeader(binaryReader);
            return ReadBinaryChunk(binaryReader, CHUNKBIN);
        }

        private StreamChunk LoadBinaryBuffer(JsonParser.JsonValue buffers, int bufferIndex, int bufferViewByteOffset = 0, int bufferViewLength = 0)
        {
            var buffer = buffers.GetArrayValueAtIndex(bufferIndex);

            var bufferData = LoadBinaryBufferUnchecked(buffer, bufferIndex);

            if (bufferData == null)
            {
                throw new Exception("The gLTF parser could not find a dependency binary file.");
            }


            if (bufferViewByteOffset > 0 || bufferViewLength > 0)
            {
                bufferData = new StreamChunk(bufferData, bufferViewLength, bufferViewByteOffset);
            }

            return bufferData;
        }

        private StreamChunk LoadBinaryBufferUnchecked(JsonParser.JsonValue buffer, int bufferIndex)
        {
            var uri = buffer.GetChildWithKey(_uri_token);
            return TryLoadBase64BinaryBufferUnchecked(uri, bufferIndex, EMBEDDEDGLTFBUFFER)
                   ?? TryLoadBase64BinaryBufferUnchecked(uri, bufferIndex, EMBEDDEDOCTETSTREAM)
                   ?? ExternalReferenceSolver(uri);
        }

        private StreamChunk TryLoadBase64BinaryBufferUnchecked(JsonParser.JsonValue uri, int bufferIndex, string prefix)
        {
            if (!uri.Valid || !uri.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }
            var content = uri.AddOffset(prefix.Length);
            if (!FileUtils.TrySaveFileAtPersistentDataPath(
                    _reader.AssetLoaderContext,
                    bufferIndex.ToString(),
                    $"{bufferIndex}.bin",
                    content.GetByteEnumerator(),
                    out var filename,
                    DecodeBase64Data
                ))
            {
                var data = content.ToString();
                var decoded = Convert.FromBase64String(data);
                var memoryStream = new MemoryStream(decoded);
                return new StreamChunk(memoryStream, decoded.Length);
            }
            return new StreamChunk(File.OpenRead(filename));
        }
    }
}