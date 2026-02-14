using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Stl.Reader;
using UnityEngine;

namespace TriLibCore.Stl
{
    public class StlProcessor
    {
        private const string ColorHeader = "COLOR=";
        private const string DefaultElementName = "default";

        private const long endloop_token = -5513532493766152582;
        private const long endsolid_token = -4898810643358303851;
        private const long facet_token = 7096547112153259250;
        private const long loop_token = 6774539739450461193;
        private const long normal_token = -1367968407521166068;
        private const long outer_token = 7096547112162183094;
        private const long solid_token = 7096547112165690854;
        private const long vertex_token = -1367968407301361207;

        private readonly Dictionary<string, IGeometryGroup> _geometryGroups = new Dictionary<string, IGeometryGroup>();

        private readonly Dictionary<string, IModel> _models = new Dictionary<string, IModel>();

        private readonly Vector3[] _tempVertices = new Vector3[3];
        private StlGeometry _activeGeometry;
        private IGeometryGroup _activeGeometryGroup;
        private Color _facetColor = Color.white;
        private Vector3 _facetNormal;
        private string _groupName = DefaultElementName;
        private string _lastGeometryGroupName;
        private int _lastLoopNumber = -1;
        private int _loopNumber;
        private Color _partColor = Color.white;
        private StlReader _reader;
        public IRootModel Process(StlReader stlReader, Stream stream)
        {
            _reader = stlReader;
            if (IsBinary(stream))
            {
                var rootModel = ParseBinary(stream);
                return rootModel;
            }
            else
            {
                var rootModel = ParseASCII(stream);
                return rootModel;
            }
        }

        private static bool CheckForColorHeader(Stream stream)
        {
            var colorHeaderBytes = Encoding.ASCII.GetBytes(ColorHeader);
            var index = 0;

            while (true)
            {
                var nextByte = stream.ReadByte();

                if (nextByte == -1)
                {
                    return false;
                }

                if (nextByte == colorHeaderBytes[index])
                {
                    index++;

                    if (index == colorHeaderBytes.Length)
                    {
                        return true;
                    }
                }
                else
                {
                    index = 0;
                }
            }
        }

        private static Vector3 GetNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            var side1 = b - a;
            var side2 = c - a;
            return Vector3.Cross(side1, side2).normalized;
        }

        private static bool IsBinary(Stream stream)
        {
            var hasBinary = false;
            while (true)
            {
                var b = stream.ReadByte();
                if (b == -1)
                {
                    break;
                }
                if (b < 32 && b != 13 && b != 10 && b != 9)
                {
                    hasBinary = true;
                    break;
                }
            }
            stream.Seek(0, SeekOrigin.Begin);
            return hasBinary;
        }

        private static Vector3 ReadVector3(System.IO.BinaryReader binaryReader)
        {
            var x = binaryReader.ReadSingle();
            var y = binaryReader.ReadSingle();
            var z = binaryReader.ReadSingle();
            if (StlReader.LoadWithYUp)
            {
                return new Vector3(x, y, z);
            }
            return new Vector3(x, z, y);
        }

        private static Vector3 ReadVector3(StlStreamReader stlStreamReader)
        {
            var x = stlStreamReader.ReadTokenAsFloat();
            var y = stlStreamReader.ReadTokenAsFloat();
            var z = stlStreamReader.ReadTokenAsFloat();
            if (StlReader.LoadWithYUp)
            {
                return new Vector3(x, y, z);
            }
            return new Vector3(x, z, y);
        }

        private void AddModel(IModel parent, int vertexCount)
        {
            var geometryGroup = GetActiveGeometryGroup(vertexCount);
            var model = new StlModel();
            model.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData() { ModelName = geometryGroup.Name }, model, _reader.Name);
            model.Parent = parent;
            model.Visibility = true;
            model.GeometryGroup = geometryGroup;
            model.LocalRotation = Quaternion.identity;
            model.LocalScale = Vector3.one;
            _models.Add(_groupName, model);
        }

        private void AddOutputVertex(IGeometryGroup geometryGroup, int vertexIndex, Vector3 vertex, int verticesCount)
        {
            var geometry = GetActiveGeometry(geometryGroup, false);
            geometry.AddVertex(_reader.AssetLoaderContext,
                vertexIndex,
                position: vertex,
                normal: StlReader.ImportNormals ? _facetNormal : default,
                tangent: default,
                color: _facetColor, uv0: StlReader.StoreTriangleIndexInTexCoord0 ? new Vector2(vertexIndex / 3, 0f) : default);
        }

        private StlGeometry GetActiveGeometry(IGeometryGroup geometryGroup, bool isQuad)
        {
            if (_loopNumber != _lastLoopNumber)
            {
                _activeGeometry = geometryGroup.GetGeometry<StlGeometry>(_reader.AssetLoaderContext, _loopNumber, isQuad, false);
                _lastLoopNumber = _loopNumber;
            }
            return _activeGeometry;
        }

        private IGeometryGroup GetActiveGeometryGroup(int verticesCount)
        {
            var geometryGroupName = _groupName;
            if (geometryGroupName != _lastGeometryGroupName)
            {
                if (!_geometryGroups.TryGetValue(geometryGroupName, out var geometryGroup))
                {
                    geometryGroup = CommonGeometryGroup.Create(StlReader.ImportNormals, false, true, StlReader.StoreTriangleIndexInTexCoord0, false, false, false, false);
                    geometryGroup.Name = geometryGroupName;
                    geometryGroup.Setup(_reader.AssetLoaderContext, verticesCount, 1);
                    _geometryGroups.Add(geometryGroupName, geometryGroup);
                }
                _activeGeometryGroup = geometryGroup;
                _lastGeometryGroupName = geometryGroupName;
            }
            return _activeGeometryGroup;
        }
        private IRootModel ParseASCII(Stream stream)
        {
            _loopNumber = 0;
            var stlStreamReader = new StlStreamReader(_reader.AssetLoaderContext, stream);
            var vertexIndex = 0;
            var modelAdded = false;
            var rootModel = new StlRootModel();
            var lastPerc = 0f;
            var vertexCount = 0;
            while (!stlStreamReader.EndOfStream)
            {
                var command = stlStreamReader.ReadToken(false);
                if (command == facet_token)
                {
                    vertexCount += 3;
                }
            }
            stlStreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            while (!stlStreamReader.EndOfStream)
            {
                var command = stlStreamReader.ReadToken(false);
                switch (command)
                {
                    case solid_token:
                        {
                            _groupName = stlStreamReader.ReadTokenAsString(false, true) ?? DefaultElementName;
                            modelAdded = false;
                            break;
                        }
                    case facet_token:
                        {
                            var normal = stlStreamReader.ReadToken();
                            if (normal != normal_token)
                            {
                                throw new Exception("Expected: 'normal'");
                            }
                            if (_reader.AssetLoaderContext.Options.ImportNormals)
                            {
                                _facetNormal = ReadVector3(stlStreamReader);
                            }
                            else
                            {
                                ReadVector3(stlStreamReader);
                                _facetNormal = Vector3.zero;
                            }
                            break;
                        }
                    case outer_token:
                        {
                            var loop = stlStreamReader.ReadToken();
                            if (loop != loop_token)
                            {
                                throw new Exception("Expected: 'loop'");
                            }
                            _loopNumber++;
                            break;
                        }
                    case endloop_token:
                        {
                            _loopNumber--;
                            break;
                        }
                    case endsolid_token:
                        {
                            stlStreamReader.ReadToken(false);
                            if (!modelAdded)
                            {
                                AddModel(rootModel, vertexCount);
                                modelAdded = true;
                            }
                            break;
                        }
                    case vertex_token:
                        {
                            if (_reader.AssetLoaderContext.Options.ImportMeshes)
                            {
                                var value = ReadVector3(stlStreamReader);
                                var scale = _reader.AssetLoaderContext.Options.ScaleFactor;
                                var geometryGroup = GetActiveGeometryGroup(vertexCount);
                                var finalIndex = vertexIndex++ % 3;
                                _tempVertices[finalIndex] = value * scale;
                                if (finalIndex == 2)
                                {
                                    int index0;
                                    int index1;
                                    int index2;
                                    if (StlReader.LoadWithYUp)
                                    {
                                        index0 = 2;
                                        index1 = 1;
                                        index2 = 0;
                                    }
                                    else
                                    {
                                        index0 = 0;
                                        index1 = 1;
                                        index2 = 2;
                                    }
                                    var calculatedNormal = GetNormal(_tempVertices[0], _tempVertices[1], _tempVertices[2]);
                                    if (!StlReader.FixInfacingNormals || Vector3.Dot(calculatedNormal, _facetNormal) > 0)
                                    {
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index2], vertexCount);
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index1], vertexCount);
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index0], vertexCount);
                                    }
                                    else
                                    {
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index0], vertexCount);
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index1], vertexCount);
                                        AddOutputVertex(geometryGroup, geometryGroup.VerticesDataCount, _tempVertices[index2], vertexCount);
                                    }
                                }
                            }
                            break;
                        }
                    default:
                        {
                            while (command != 0)
                            {
                                command = stlStreamReader.ReadToken(false);
                            }
                            break;
                        }
                }
                var perc = (float)stlStreamReader.BaseStream.Position / stlStreamReader.BaseStream.Length;
                if (perc > lastPerc + 0.33f)
                {

                    _reader.UpdateLoadingPercentage(perc, (int)StlReader.ProcessingSteps.Parsing);
                    lastPerc = perc;
                }
            }
            _reader.UpdateLoadingPercentage(1f, (int)StlReader.ProcessingSteps.Parsing);
            if (!modelAdded)
            {
                AddModel(rootModel, vertexCount);
            }
            rootModel.LocalScale = Vector3.one;
            rootModel.LocalRotation = Quaternion.identity;
            rootModel.Visibility = true;
            rootModel.Children = new List<IModel>(_models.Values);
            rootModel.AllGeometryGroups = new List<IGeometryGroup>(_geometryGroups.Values);
            rootModel.AllModels = rootModel.Children;
            return rootModel;
        }

        private IRootModel ParseBinary(Stream stream)
        {
            var isMaterialise = false;
            var binaryReader = new System.IO.BinaryReader(stream);
            if (_reader.AssetLoaderContext.Options.ImportColors && CheckForColorHeader(stream))
            {
                var r = binaryReader.ReadByte();
                var g = binaryReader.ReadByte();
                var b = binaryReader.ReadByte();
                var a = binaryReader.ReadByte();
                _partColor = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                _facetColor = _partColor;
                isMaterialise = true;
            }
            stream.Seek(80, SeekOrigin.Begin);
            var triangleCount = binaryReader.ReadInt32();
            var scale = _reader.AssetLoaderContext.Options.ScaleFactor;
            var geometryGroup = GetActiveGeometryGroup(triangleCount*3);
            var rootModel = new StlRootModel();
            var model = new StlModel();
            model.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData() { ModelName = geometryGroup.Name }, model, _reader.Name);
            model.Parent = rootModel;
            model.Visibility = true;
            model.GeometryGroup = geometryGroup;
            model.LocalRotation = Quaternion.identity;
            model.LocalScale = Vector3.one;
            _models.Add(model.Name, model);
            for (var i = 0; i < triangleCount; i++)
            {
                if (_reader.AssetLoaderContext.Options.ImportNormals)
                {
                    _facetNormal = ReadVector3(binaryReader);
                }
                else
                {
                    ReadVector3(binaryReader);
                    _facetNormal = Vector3.zero;
                }

                Vector3 vertexA;
                Vector3 vertexB;
                Vector3 vertexC;

                if (StlReader.LoadWithYUp)
                {
                    vertexC = ReadVector3(binaryReader) * scale;
                    vertexB = ReadVector3(binaryReader) * scale;
                    vertexA = ReadVector3(binaryReader) * scale;

                }
                else
                {
                    vertexA = ReadVector3(binaryReader) * scale;
                    vertexB = ReadVector3(binaryReader) * scale;
                    vertexC = ReadVector3(binaryReader) * scale;
                }

                var attributes = binaryReader.ReadInt16();
                if (attributes != 0)
                {
                    int r;
                    int g;
                    int b;
                    bool use;
                    if (isMaterialise)
                    {
                        r = (attributes & 0b0000000000011111);
                        g = (attributes & 0b0000001111100000) >> 5;
                        b = (attributes & 0b0111110000000000) >> 10;
                        use = (attributes & 0b1000000000000000) >> 15 == 0;
                    }
                    else
                    {
                        b = (attributes & 0b0111110000000000) >> 10;
                        g = (attributes & 0b0000001111100000) >> 5;
                        r = (attributes & 0b0000000000011111);
                        use = (attributes & 0b1000000000000000) >> 15 == 1;
                    }
                    _facetColor = use ? new Color(r / 32f, g / 32f, b / 32f, 1f) : _partColor;
                }

                if (_reader.AssetLoaderContext.Options.ImportMeshes)
                {
                    var baseIndex = i * 3;
                    var calculatedNormal = GetNormal(vertexA, vertexB, vertexC);
                    if (!StlReader.FixInfacingNormals || Vector3.Dot(_facetNormal, calculatedNormal) > 0f)
                    {
                        AddOutputVertex(geometryGroup, baseIndex + 0, vertexC, triangleCount * 3);
                        AddOutputVertex(geometryGroup, baseIndex + 1, vertexB, triangleCount * 3);
                        AddOutputVertex(geometryGroup, baseIndex + 2, vertexA, triangleCount* 3);
                    }
                    else
                    {
                        AddOutputVertex(geometryGroup, baseIndex + 2, vertexA, triangleCount * 3);
                        AddOutputVertex(geometryGroup, baseIndex + 1, vertexB, triangleCount * 3);
                        AddOutputVertex(geometryGroup, baseIndex + 0, vertexC, triangleCount * 3);
                    }
                }
            }
            _reader.UpdateLoadingPercentage(1f, (int)StlReader.ProcessingSteps.ProcessTriangle, triangleCount);
            rootModel.LocalScale = Vector3.one;
            rootModel.LocalRotation = Quaternion.identity;
            rootModel.Visibility = true;
            rootModel.Children = new List<IModel>(_models.Values);
            rootModel.AllGeometryGroups = new List<IGeometryGroup>(_geometryGroups.Values);
            rootModel.AllModels = rootModel.Children;
            return rootModel;
        }
    }
}