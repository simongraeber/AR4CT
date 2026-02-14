using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using TriLibCore.Fbx.ASCII;
using TriLibCore.Fbx.Binary;
using TriLibCore.Fbx.Reader;
using TriLibCore.General;
using Debug = UnityEngine.Debug;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _PropertyTemplate_token = 355745346917254004;
        private const long _Properties70_token = 1183621237966419921;
        private const long _Properties60_token = 1183621237966419890;

        private const long PropertiesTemplateName = _PropertyTemplate_token;

        public long PropertiesName => Document.Version >= 7000 ? _Properties70_token : _Properties60_token;

        public FBXDocument Document;

        private readonly string[] _values = new string[4];

        public readonly FbxReader Reader;

        #region Binary
        public int ActiveDataSize;
        public int ActiveSubDataSize;
        public BinaryReader ActiveBinaryReader;
        public FBXBinaryReader FBXBinaryReader;
        #endregion

        #region ASCII
        public FBXASCIIReader ActiveASCIIReader;
        #endregion

        public char ActivePropertyType;
        public bool ActivePropertyEncoded;

        public FBXProcessor(FbxReader reader)
        {
            Reader = reader;
        }

        
        public FBXRootModel Process(FBXNode rootNode, bool isBinary)
        {
            Document = new FBXDocument
            (
               isBinary,
               rootNode
            );
            Document.Reader = Reader;

            ProcessHeaderExtension(rootNode);
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.ProcessHeaderExtension);

            ProcessGlobalSettings(rootNode);
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.ProcessGlobalSettings);

            ProcessDefinitions(rootNode);
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.ProcessDefinitions);

            Document.Setup();

            ProcessObjects(rootNode);
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.ProcessObjects);

            ProcessConnections(rootNode);
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.ProcessConnections);

            PostProcessModels();
            Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessModels);

            if (FbxReader.PivotMode != FBXPivotMode.Legacy)
            {
                var canProcess = true;
                if (Document.AllAnimations != null)
                {
                    foreach (var animation in Document.AllAnimations)
                    {
                        if (animation is FBXAnimationStack stack && stack.AnimatedTimes?.Count > 0)
                        {
                            if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                            {
                                Debug.LogWarning("TriLib can't move pivots of animated FBX models. Rolling back to legacy (baked) pivot mode.");
                            }
                            canProcess = false;
                            break;
                        }
                    }
                }
                if (canProcess)
                {
                    ProcessPivots(Document);
                }
                Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessPivots);
            }

            if (Reader.AssetLoaderContext.Options.ImportMeshes)
            {
                PostProcessGeometries();
                PostProcessModelGeometries();
                Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessGeometries);
            }

            if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
            {
                PostProcessAnimations();
                Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessAnimations);
            }

            if (Reader.AssetLoaderContext.Options.ImportCameras)
            {
                PostProcessCameras();
                Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessCameras);
            }

            if (Reader.AssetLoaderContext.Options.ImportTextures)
            {
                PostProcessTextures();
                Reader.UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.PostProcessTextures);
            }

            return Document;
        }

        private void OutputNode(StreamWriter streamWriter, FBXNode node, int ident)
        {
            OutputIdent(streamWriter, ident);
            streamWriter.Write(node.Name);
            streamWriter.Write(": ");
            var hasProperties = node.Properties != null && node.Properties.Values.Count > 0;
            var hasChildren = node.Children != null && node.Children.Count > 0;
            if (hasProperties)
            {
                var isArray = node.Properties.ArrayLength > 1;
                if (isArray)
                {
                    streamWriter.Write('*');
                    streamWriter.Write(node.Properties.ArrayLength);
                    streamWriter.Write('\n');
                    OutputIdent(streamWriter, ident);
                    streamWriter.Write("{\n");
                    OutputIdent(streamWriter, ident + 1);
                    streamWriter.Write("a: ");
                }
                for (var i = 0; i < node.Properties.Values.Count; i++)
                {
                    if (i > 0)
                    {
                        streamWriter.Write(", ");
                    }
                    if (isArray)
                    {
                        var property = node.Properties[i];
                        property.GetPropertyData(node.Properties, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
                        streamWriter.Write(propertyType + ":" + arrayLength + "<binary>");
                    }
                    else
                    {
                        var property = node.Properties[i];
                        property.GetPropertyData(node.Properties, out var propertyType, out var compressedLength, out var encoded, out var arrayLength, out var innerDataLength);
                        streamWriter.Write(node.Properties.GetConvertedStringValue(i) + "<" + propertyType + ">");
                    }
                }
                if (isArray)
                {
                    streamWriter.Write('\n');
                    OutputIdent(streamWriter, ident);
                    streamWriter.Write('}');
                }
                if (!hasChildren)
                {
                    streamWriter.Write('\n');
                }
            }
            if (hasChildren)
            {
                streamWriter.Write(" {\n");
                var innerIdent = ident + 1;
                foreach (var child in node.Children)
                {
                    OutputNode(streamWriter, child, innerIdent);
                }
                OutputIdent(streamWriter, ident);
                streamWriter.Write("}\n");
            }
            else if (!hasProperties)
            {
                streamWriter.Write('\n');
            }
        }

        private static void OutputIdent(StreamWriter streamWriter, int ident)
        {
            for (var i = 0; i < ident; i++)
            {
                streamWriter.Write('\t');
            }
        }

        private void PostProcessCameras()
        {
            for (var i = 0; i < Document.AllCameras.Count; i++)
            {
                var camera = (FBXCamera)Document.AllCameras[i];
                camera.Setup(Reader.AssetLoaderContext);
            }
        }

        private static void SplitObjectNameData(string nameData, out string name, out string type)
        {
            var binary = false;
            var colonPosition = nameData.IndexOf("\0\u0001", StringComparison.Ordinal);
            if (colonPosition < 0)
            {
                colonPosition = nameData.IndexOf("::", StringComparison.Ordinal);
            }
            else
            {
                binary = true;
            }
            if (colonPosition >= 0)
            {
                var firstPart = nameData.Substring(0, colonPosition);
                var lastPart = nameData.Substring(colonPosition + 2);
                type = binary ? lastPart : firstPart;
                name = binary ? firstPart : lastPart;
                return;
            }
            type = null;
            name = null;
        }

        public FBXProperty LoadArrayProperty(FBXProperties properties, ref byte[] decoded, ref MemoryStream decodedMemoryStream, ref BinaryReader decodedBinaryReader, out char propertyType, out bool encoded)
        {
            var property = properties[0];
            property.GetPropertyData(properties, out propertyType, out _, out encoded, out var arrayLength, out _);
            ActiveDataSize = FBXProperty.GetDataSize(propertyType, arrayLength, out ActiveSubDataSize);
            ActivePropertyType = propertyType;
            ActivePropertyEncoded = encoded;
            if (encoded)
            {
                if (decoded == null)
                {
                    var position = FBXBinaryReader.BaseStream.Position;
                    decoded = new byte[ActiveDataSize];
                    FBXBinaryReader.BaseStream.Seek(2, SeekOrigin.Current);
                    using (var inflateStream = new DeflateStream(FBXBinaryReader.BaseStream, CompressionMode.Decompress, true))
                    {
                        inflateStream.Read(decoded, 0, decoded.Length);
                    }
                    FBXBinaryReader.BaseStream.Seek(position, SeekOrigin.Begin);
                    decodedMemoryStream = new MemoryStream(decoded);
                    decodedBinaryReader = new BinaryReader(decodedMemoryStream, Encoding.UTF8, true);
                }
                decodedBinaryReader.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            var binaryEncoded = propertyType != 'R' && propertyType != 'S' && encoded;
            ActiveBinaryReader = binaryEncoded ? decodedBinaryReader : FBXBinaryReader;
            return property;
        }

        public void ReleaseActiveBinaryReader()
        {
            ActiveBinaryReader = FBXBinaryReader;
        }
    }
}
