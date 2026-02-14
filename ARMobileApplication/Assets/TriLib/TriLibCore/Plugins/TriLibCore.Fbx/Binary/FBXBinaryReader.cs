using System;
using System.IO;
using System.Text;
using TriLibCore.Fbx.Reader;

namespace TriLibCore.Fbx.Binary
{
    public class FBXBinaryReader : BinaryReader
    {
        public const int MaxChars = 1024;

        public readonly char[] Chars = new char[MaxChars];
        public readonly byte[] Buffer = new byte[MaxChars];

        public long CharsHash;

        private readonly FBXProcessor _processor;
        private readonly ReaderBase _reader;
        private bool _is64Bits;
        private int _version;

        public FBXBinaryReader(FBXProcessor processor, Stream inputStream, ReaderBase reader, bool leaveOpen) : base(inputStream, Encoding.UTF8, leaveOpen)
        {
            _reader = reader;
            _processor = processor;
            _processor.FBXBinaryReader = this;
            _processor.ReleaseActiveBinaryReader();
        }

        public FBXNode ReadBinaryDocument()
        {
            BaseStream.Seek(23, SeekOrigin.Begin);
            _version = ReadInt32();
            if (_version < 7000)
            {
                throw new Exception("Only files generated with FBX SDK 7.0 onwards can be loaded");
            }
            _is64Bits = _version >= 7500;
            return ReadRootNode();
        }

        private string ReadStringEx()
        {
            var byteCount = Read7BitEncodedInt();
            if (byteCount <= 0)
            {
                return null;
            }
            if (byteCount > MaxChars)
            {
                throw new Exception("Invalid string");
            }
            Array.Clear(Buffer, 0, Buffer.Length);
            Array.Clear(Chars, 0, Chars.Length);
            BaseStream.Read(Buffer, 0, byteCount);
            Encoding.UTF8.GetChars(Buffer, 0, byteCount, Chars, 0);
            var charCount = 0;
            var zeroCount = 0;
            do
            {
                var currentChar = Chars[charCount++];
                zeroCount = currentChar == '\0' ? zeroCount + 1 : 0;
            } while (charCount < Buffer.Length && zeroCount < 2);
            var value = new string(Chars, 0, charCount - 2);
            return value;
        }

        private FBXNode ReadRootNode()
        {
            var rootNode = new FBXNode("FBXRoot");
            while (BaseStream.Position < BaseStream.Length)
            {
                _reader.UpdateLoadingPercentage((float)BaseStream.Position / BaseStream.Length, (int)FbxReader.ProcessingSteps.Parsing);
                var node = ReadNode();
                if (node != null)
                {
                    rootNode.Add(node);
                }
                else
                {
                    break;
                }
            }
            return rootNode;
        }

        private FBXNode ReadNode()
        {
            long endOffset;
            long numProperties;
            long dataLength;
            if (_is64Bits)
            {
                endOffset = ReadInt64();
                numProperties = ReadInt64();
                dataLength = ReadInt64();
            }
            else
            {
                endOffset = ReadInt32();
                numProperties = ReadInt32();
                dataLength = ReadInt32();
            }
            var name = ReadStringEx();
            if (!string.IsNullOrEmpty(name))
            {
                var node = new FBXNode(name);
                var arrayLength = 0;
                node.Properties = new FBXProperties(_processor, 0);
                if (numProperties > 0)
                {
                    for (var i = 0; i < numProperties; i++)
                    {
                        var property = new FBXProperty();
                        property.Position = BaseStream.Position;
                        var propertyType = ReadChar();
                        switch (propertyType)
                        {
                            case 'b':
                            case 'c':
                            case 'y':
                            case 'i':
                            case 'f':
                            case 'd':
                            case 'l':
                                {
                                    arrayLength = ReadInt32();
                                    var encoded = ReadInt32() == 1;
                                    var compressedLength = ReadInt32();
                                    BaseStream.Seek(compressedLength, SeekOrigin.Current);
                                    break;
                                }
                            default:
                                {
                                    int innerDataLength;
                                    switch (propertyType)
                                    {
                                        case 'S':
                                        case 'R':
                                            innerDataLength = ReadInt32();
                                            break;
                                        case 'B':
                                        case 'C':
                                            innerDataLength = sizeof(byte);
                                            break;
                                        case 'Y':
                                            innerDataLength = sizeof(short);
                                            break;
                                        case 'I':
                                        case 'F':
                                            innerDataLength = sizeof(int);
                                            break;
                                        case 'D':
                                        case 'L':
                                            innerDataLength = sizeof(long);
                                            break;
                                        default:
                                            throw new Exception($"Unknown data type: {propertyType}");
                                    }
                                    BaseStream.Seek(innerDataLength, SeekOrigin.Current);
                                    break;
                                }
                        }
                        node.Properties.Values.Add(property);
                    }
                }
                node.Properties.ArrayLength = arrayLength;
                while (BaseStream.Position < endOffset)
                {
                    var subNode = ReadNode();
                    if (subNode != null)
                    {
                        node.Add(subNode);
                    }
                    else
                    {
                        break;
                    }
                }
                return node;
            }
            return null;
        }
    }
}
