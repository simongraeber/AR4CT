using System;
using System.Collections.Generic;
using System.IO;
using LibTessDotNet;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Obj.Reader;
using TriLibCore.Textures;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Obj
{
    public partial class ObjProcessor
    {

        private const string DefaultElementName = "default";

        private readonly List<string> _processedMaterials = new List<string>();

        private readonly Dictionary<string, IMaterial> _materialLibraries = new Dictionary<string, IMaterial>();

        private readonly Dictionary<string, IGeometryGroup> _geometryGroups = new Dictionary<string, IGeometryGroup>();

        private readonly Dictionary<string, IModel> _models = new Dictionary<string, IModel>();

        private readonly Dictionary<string, ITexture> _textures = new Dictionary<string, ITexture>();

        private string _geometryGroupName = "default.default";
        private string _materialName = DefaultElementName;
        private string _objectName = DefaultElementName;

        private IGeometryGroup _activeGeometryGroup;
        private ObjGeometry _activeGeometry;
        private ObjMaterial _activeMaterial;
        private ObjModel _activeModel;
        private int _unnamedTextureCount;
        private ObjReader _reader;

        private static Matrix4x4 _conversionMatrix = Matrix4x4.TRS(Vector3.zero, Quaternion.Euler(0f, 180f, 0f), new Vector3(1f, 1f, -1f));

        private IList<Vector3> _allVertices;
        private IList<Vector3> _allNormals;
        private IList<Vector2> _allUVs;
        private IList<Color> _allColors;
        private bool _hasColors;
        private bool _hasNormals;
        private bool _hasTextureCoords;

        private const long mtllib_token = -1367968407545357083;
        private const long usemtl_token = -1367968407317454621;
        private const long g_token = 34902897112120610;
        private const long v_token = 34902897112120625;
        private const long vt_token = 1081989810475739491;
        private const long vn_token = 1081989810475739485;
        private const long f_token = 34902897112120609;
        private const long newmtl_token = -1367968407530251734;
        private const long Ka_token = 1081989810475738139;
        private const long Kd_token = 1081989810475738142;
        private const long Ks_token = 1081989810475738157;
        private const long Ke_token = 1081989810475738143;
        private const long illum_token = 7096547112156366652;
        private const long d_token = 34902897112120607;
        private const long Ns_token = 1081989810475738250;
        private const long Ni_token = 1081989810475738240;
        private const long Pr_token = 1081989810475738311;
        private const long Pm_token = 1081989810475738306;
        private const long Ps_token = 1081989810475738312;
        private const long Pc_token = 1081989810475738296;
        private const long Pcr_token = -3351804022671215942;
        private const long norm_token = 6774539739450520865;
        private const long _blenu_token = -1367968409394252678;
        private const long _blenv_token = -1367968409394252677;
        private const long _bm_token = -3351804022671249613;
        private const long _boost_token = -1367968409394153541;
        private const long _cc_token = -3351804022671249592;
        private const long _clamp_token = -1367968409393333037;
        private const long on_token = 1081989810475739268;
        private const long off_token = -3351804022671186070;
        private const long _imfchan_token = -4898812188256917460;
        private const long _mm_token = -3351804022671249272;
        private const long _o_token = 1081989810475737223;
        private const long _s_token = 1081989810475737227;
        private const long _t_token = 1081989810475737228;
        private const long _texres_token = -5513532543293300223;
        private const long o_token = 34902897112120618;

        private void GetActiveGeometryGroup(int verticesCapacity)
        {
            if (!_geometryGroups.TryGetValue(_geometryGroupName, out var geometryGroup))
            {
                geometryGroup = CommonGeometryGroup.Create(_hasNormals, false, _hasColors, _hasTextureCoords, false, false, false, false);
                geometryGroup.Name = _geometryGroupName;
                geometryGroup.Setup(_reader.AssetLoaderContext, verticesCapacity, 1);
                _geometryGroups.Add(_geometryGroupName, geometryGroup);
            }
            _activeGeometryGroup = geometryGroup;
        }

        private void GetActiveGeometry(bool isQuad)
        {
            var materialIndex = _materialName.GetHashCode();
            var geometry = _activeGeometryGroup.GetGeometry<ObjGeometry>(_reader.AssetLoaderContext, materialIndex, isQuad, false); 
            geometry.MaterialName = _materialName;
            _activeGeometry = geometry;
        }

        private void GetActiveMaterial()
        {
            if (!_materialLibraries.TryGetValue(_materialName, out var materialLibrary))
            {
                materialLibrary = new ObjMaterial { Name = _materialName, Index = _materialLibraries.Count };
                _materialLibraries.Add(_materialName, materialLibrary);
            }
            _activeMaterial = (ObjMaterial)materialLibrary;
        }

        private void GetActiveModel(IModel parent, int verticesCapacity)
        {
            GetActiveGeometryGroup(verticesCapacity);
            if (!_models.TryGetValue(_geometryGroupName, out var model))
            {
                model = new ObjModel();
                model.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData() { ModelName = _objectName, MaterialName = _materialName }, model, _reader.Name);
                model.LocalScale = Vector3.one;
                model.LocalRotation = Quaternion.identity;
                model.GeometryGroup = _activeGeometryGroup;
                model.Visibility = true;
                model.Parent = parent; ;
                _models.Add(_geometryGroupName, model);
            }
            _activeModel = (ObjModel)model;
        }

        private static int GetElementIndex(int elementCount, int value)
        {
            if (value < 0)
            {
                return elementCount + value;
            }
            return value - 1;
        }

        
        public IRootModel Process(ObjReader reader, Stream stream)
        {
            _reader = reader;
            var objStreamReader = new ObjStreamReader(reader.AssetLoaderContext, stream);
            var vertexCapacity = 0;
            var hasVertices = false;
            var hasColors = false;
            var hasNormals = false;
            var hasTextureCoords = false;
            while (!objStreamReader.EndOfStream)
            {
                var command = objStreamReader.ReadToken(false);
                switch (command)
                {
                    case v_token:
                        {
                            if (_reader.AssetLoaderContext.Options.ImportMeshes || _reader.AssetLoaderContext.Options.LoadPointClouds)
                            {
                                hasVertices = true;
                                vertexCapacity++;
                                objStreamReader.ReadTokenAsFloat();
                                objStreamReader.ReadTokenAsFloat();
                                objStreamReader.ReadTokenAsFloat();
                                if (ObjReader.ParseVertexColors)
                                {
                                    command = objStreamReader.ReadToken(false);
                                    if (command != 0)
                                    {
                                        hasColors = true;
                                    }
                                }
                            }
                            while (command != 0)
                            {
                                command = objStreamReader.ReadToken(false, false, false, false);
                            }
                            break;
                        }
                    case vt_token:
                        {
                            if (_reader.AssetLoaderContext.Options.ImportMeshes)
                            {
                                hasTextureCoords = true;
                            }
                            while (command != 0)
                            {
                                command = objStreamReader.ReadToken(false, false, false, false);
                            }
                            break;
                        }
                    case vn_token:
                        {
                            if (_reader.AssetLoaderContext.Options.ImportMeshes)
                            {
                                hasNormals = true;
                            }
                            while (command != 0)
                            {
                                command = objStreamReader.ReadToken(false, false, false, false);
                            }
                            break;
                        }
                    default:
                        {
                            while (command != 0)
                            {
                                command = objStreamReader.ReadToken(false, false, false, false);
                            }
                            break;
                        }
                }
            }
            objStreamReader.BaseStream.Seek(0, SeekOrigin.Begin);
            var vertexDataIndices = new List<VertexDataIndices>(255);
            if (vertexCapacity > ushort.MaxValue)
            {
                vertexCapacity = 3;
            }
            _hasNormals = hasNormals;
            _hasColors = hasColors;
            _hasTextureCoords = hasTextureCoords;
            _allVertices = new List<Vector3>(vertexCapacity);
            _allNormals = new List<Vector3>(hasNormals ? vertexCapacity : 0);
            _allUVs = new List<Vector2>(hasTextureCoords ? vertexCapacity : 0);
            _allColors = new List<Color>(hasColors ? vertexCapacity : 0);
            var lastPerc = 0f;
            while (!objStreamReader.EndOfStream)
            {
                var command = objStreamReader.ReadToken(false);
                switch (command)
                {
                    case mtllib_token when !_reader.AssetLoaderContext.Options.LoadPointClouds && _reader.AssetLoaderContext.Options.ImportMaterials:
                        {
                            for (; ; )
                            {
                                var mtLibName = objStreamReader.ReadToken(false, true, true, true);
                                if (mtLibName == 0)
                                {
                                    break;
                                }
                                var mtlLibNameString = objStreamReader.GetTokenAsString();
                                if (_processedMaterials.Contains(mtlLibNameString))
                                {
                                    continue;
                                }
                                ProcessMaterialLibrary(mtlLibNameString);
                            }
                            break;
                        }
                    case usemtl_token when !_reader.AssetLoaderContext.Options.LoadPointClouds && _reader.AssetLoaderContext.Options.ImportMaterials:
                        {
                            var materialName = objStreamReader.ReadToken(false, true, true, true);
                            _materialName = materialName == 0 ? DefaultElementName : objStreamReader.GetTokenAsString();
                            break;
                        }
                    case o_token when !_reader.AssetLoaderContext.Options.LoadPointClouds:
                        {
                            var objectName = objStreamReader.ReadToken(false, true, true, true);
                            _objectName = objectName == 0 ? DefaultElementName : objStreamReader.GetTokenAsString();
                            _geometryGroupName = $"{_objectName}.{DefaultElementName}";
                            break;
                        }
                    case g_token when !_reader.AssetLoaderContext.Options.LoadPointClouds:
                        {
                            var groupName = objStreamReader.ReadToken(false, true, true, true);
                            var groupNameValue = groupName == 0 ? DefaultElementName : objStreamReader.GetTokenAsString();
                            _geometryGroupName = $"{_objectName}.{groupNameValue}";
                            break;
                        }
                    case v_token when _reader.AssetLoaderContext.Options.ImportMeshes || _reader.AssetLoaderContext.Options.LoadPointClouds:
                        {
                            var scaleFactor = _reader.AssetLoaderContext.Options.ScaleFactor;
                            var x = objStreamReader.ReadTokenAsFloat() * scaleFactor;
                            var y = objStreamReader.ReadTokenAsFloat() * scaleFactor;
                            var z = objStreamReader.ReadTokenAsFloat() * scaleFactor;
                            _allVertices.Add(_conversionMatrix.MultiplyPoint(new Vector3(x, y, z)));
                            if (ObjReader.ParseVertexColors)
                            {
                                var r = objStreamReader.ReadToken(false);
                                if (r != 0 && objStreamReader.GetTokenAsFloat(out var rValue))
                                {
                                    var gValue = objStreamReader.ReadTokenAsFloat();
                                    var bValue = objStreamReader.ReadTokenAsFloat();
                                    _allColors.Add(new Color(rValue, gValue, bValue, 1f));
                                }
                            }
                            break;
                        }
                    case vt_token when _reader.AssetLoaderContext.Options.ImportMeshes || _reader.AssetLoaderContext.Options.LoadPointClouds:
                        {
                            var x = objStreamReader.ReadTokenAsFloat();
                            var y = objStreamReader.ReadTokenAsFloat();
                            _allUVs.Add(new Vector2(x, y));
                            break;
                        }
                    case vn_token when _reader.AssetLoaderContext.Options.ImportMeshes && _reader.AssetLoaderContext.Options.ImportNormals || _reader.AssetLoaderContext.Options.LoadPointClouds && _reader.AssetLoaderContext.Options.ImportNormals:
                        {
                            var x = objStreamReader.ReadTokenAsFloat();
                            var y = objStreamReader.ReadTokenAsFloat();
                            var z = objStreamReader.ReadTokenAsFloat();
                            _allNormals.Add(_conversionMatrix.MultiplyVector(new Vector3(x, y, z)));
                            break;
                        }
                    case f_token when !_reader.AssetLoaderContext.Options.LoadPointClouds && _reader.AssetLoaderContext.Options.ImportMeshes:
                        {
                            vertexDataIndices.Clear();
                            var vertexIndex = -1;
                            var uvIndex = -1;
                            var normalIndex = -1;
                            var colorIndex = -1;
                            var lastWasNumber = false;
                            var vertexElementIndex = 0;
                            for (; ; )
                            {
                                var token = objStreamReader.ReadToken(false, false, false, false);
                                if (token == 0)
                                {
                                    if (vertexIndex > -1)
                                    {
                                        var vertexData = new VertexDataIndices(vertexIndex, normalIndex, -1, uvIndex, -1, -1, -1, colorIndex);
                                        vertexDataIndices.Add(vertexData);
                                    }
                                    break;
                                }

                                if (objStreamReader.GetCharAt(0) == ObjStreamReader.ElementSeparatorChar)
                                {
                                    vertexElementIndex++;
                                    lastWasNumber = false;
                                    continue;
                                }

                                if (objStreamReader.GetTokenAsInt(out var elementIndex))
                                {
                                    if (lastWasNumber)
                                    {
                                        var vertexDataIndicesItem = new VertexDataIndices(vertexIndex, normalIndex, -1, uvIndex, -1, -1, -1, colorIndex); 
                                        vertexDataIndices.Add(vertexDataIndicesItem);
                                        vertexElementIndex = 0;
                                        vertexIndex = -1;
                                        uvIndex = -1;
                                        normalIndex = -1;
                                        colorIndex = -1;
                                    }
                                    switch (vertexElementIndex)
                                    {
                                        case 0:
                                            {
                                                vertexIndex = GetElementIndex(_allVertices.Count, elementIndex);
                                                colorIndex = vertexIndex;
                                                break;
                                            }
                                        case 1:
                                            {
                                                uvIndex = GetElementIndex(_allUVs.Count, elementIndex);
                                                break;
                                            }
                                        case 2:
                                            {
                                                normalIndex = GetElementIndex(_allNormals.Count, elementIndex);
                                                break;
                                            }
                                    }
                                    lastWasNumber = true;
                                }
                                else
                                {
                                    throw new Exception(); 
                                }
                            }
                            GetActiveGeometryGroup(vertexCapacity);
                            GetActiveGeometry(vertexDataIndices.Count == 4);
                            vertexDataIndices.Reverse();
                            switch (vertexDataIndices.Count)
                            {
                                case 0:
                                    break;
                                case 3:
                                    AddVertex(_activeGeometry, vertexDataIndices, 0);
                                    AddVertex(_activeGeometry, vertexDataIndices, 1);
                                    AddVertex(_activeGeometry, vertexDataIndices, 2);
                                    break;
                                case 4:
                                    if (_activeGeometry.IsQuad)
                                    {
                                        AddVertex(_activeGeometry, vertexDataIndices, 0);
                                        AddVertex(_activeGeometry, vertexDataIndices, 1);
                                        AddVertex(_activeGeometry, vertexDataIndices, 2);
                                        AddVertex(_activeGeometry, vertexDataIndices, 3);
                                    }
                                    else
                                    {
                                        AddVertex(_activeGeometry, vertexDataIndices, 0);
                                        AddVertex(_activeGeometry, vertexDataIndices, 1);
                                        AddVertex(_activeGeometry, vertexDataIndices, 2);
                                        AddVertex(_activeGeometry, vertexDataIndices, 2);
                                        AddVertex(_activeGeometry, vertexDataIndices, 3);
                                        AddVertex(_activeGeometry, vertexDataIndices, 0);
                                    }
                                    break;
                                default:
                                    var contourVertices = new ContourVertex[vertexDataIndices.Count];
                                    for (var i = 0; i < vertexDataIndices.Count; i++)
                                    {
                                        var vertex = _allVertices[vertexDataIndices[i].VertexIndex];
                                        contourVertices[i] = new ContourVertex(new Vec3(vertex.x, vertex.y, vertex.z), BuildVertexData(_activeGeometry, _activeGeometryGroup, vertexDataIndices, i));
                                    }
                                    Helpers.Tesselate(contourVertices, _reader.AssetLoaderContext, _activeGeometry, _activeGeometryGroup);
                                    break;
                            }
                            break;
                        }
                    default:
                        {
                            while (command != 0)
                            {
                                command = objStreamReader.ReadToken(false, false, false, false);
                            }
                            break;
                        }
                }
                var perc = (float)objStreamReader.BaseStream.Position / objStreamReader.BaseStream.Length;
                if (perc > lastPerc + 0.33f)
                {
                    _reader.UpdateLoadingPercentage(perc, (int)ObjReader.ProcessingSteps.Parsing);
                    lastPerc = perc;
                }
            }
            _reader.UpdateLoadingPercentage(1f, (int)ObjReader.ProcessingSteps.Parsing);
            var rootModel = new ObjRootModel();
            List<IModel> models;
            if (_reader.AssetLoaderContext.Options.LoadPointClouds)
            {
                GetActiveGeometryGroup(vertexCapacity);
                GetActiveGeometry(false);
                for (var i = 0; i < _allVertices.Count; i++)
                {
                    var vertex = _allVertices[i];
                    var normal = ListUtils.FixIndex(i, _allNormals);
                    var color = ListUtils.FixIndex(i, _allColors);
                    var uv = ListUtils.FixIndex(i, _allUVs);
                    _activeGeometry.AddVertex(_reader.AssetLoaderContext,
                        i,
                        position: vertex,
                        normal: normal,
                        tangent: default,
                        color: color, 
                    uv0: uv);

                }
                GetActiveModel(rootModel, vertexCapacity);
                models = new List<IModel>(_models.Count);
                foreach (var kvp in _models)
                {
                    if (kvp.Key == DefaultElementName && kvp.Value.GeometryGroup.GeometriesData.Count == 0)
                    {
                        continue;
                    }
                    models.Add(kvp.Value);
                }
            }
            else if (_reader.AssetLoaderContext.Options.ImportMeshes)
            {
                var geometryIndex = 0;
                foreach (var kvp in _geometryGroups)
                {
                    _geometryGroupName = kvp.Key;
                    GetActiveModel(rootModel, vertexCapacity);
                    var geometries = kvp.Value.GeometriesData;
                    if (geometries.Count == 0)
                    {
                        continue;
                    }
                    _activeModel.MaterialIndices = new int[geometries.Count];
                    foreach (ObjGeometry geometry in geometries.Values)
                    {
                        _materialName = geometry.MaterialName;
                        _activeModel.Name = _materialName;
                        GetActiveMaterial();
                        _activeModel.MaterialIndices[geometry.Index] = _activeMaterial.Index;
                    }
                    _reader.UpdateLoadingPercentage(geometryIndex++, (int)ObjReader.ProcessingSteps.PostProcessGeometry, _geometryGroups.Count);

                }
                models = new List<IModel>(_models.Count);
                foreach (var kvp in _models)
                {
                    if (kvp.Key == DefaultElementName && kvp.Value.GeometryGroup.GeometriesData.Count == 0)
                    {
                        continue;
                    }
                    models.Add(kvp.Value);
                }
            }
            else
            {
                models = null;
            }
            objStreamReader.Dispose();
            rootModel.LocalScale = Vector3.one;
            rootModel.LocalRotation = Quaternion.identity;
            rootModel.Children = models;
            rootModel.Visibility = true;
            rootModel.AllModels = models;
            rootModel.AllMaterials = new List<IMaterial>(_materialLibraries.Values);
            rootModel.AllGeometryGroups = new List<IGeometryGroup>(_geometryGroups.Values);
            rootModel.AllTextures = new List<ITexture>(_textures.Values);
            return rootModel;
        }

        private InterpolatedVertex BuildVertexData(IGeometry activeGeometry, IGeometryGroup activeGeometryGroup, List<VertexDataIndices> vertexDataIndices, int i)
        {
            var vertexData = vertexDataIndices[i];
            var position = ListUtils.FixIndex(vertexData.VertexIndex, _allVertices);
            var normal = ListUtils.FixIndex(vertexData.NormalIndex, _allNormals);
            var color = ListUtils.FixIndex(vertexData.ColorIndex, _allColors);
            var uv = ListUtils.FixIndex(vertexData.UvIndex, _allUVs);
            var interpolatedVertex = new InterpolatedVertex(position);
            interpolatedVertex.SetVertexIndex(vertexData.VertexIndex, activeGeometryGroup);
            interpolatedVertex.SetNormal(normal, activeGeometryGroup);
            interpolatedVertex.SetColor(color, activeGeometryGroup);
            interpolatedVertex.SetUV1(uv, activeGeometryGroup);
            return interpolatedVertex;
        }

        private void AddVertex(CommonGeometry geometry, IList<VertexDataIndices> vertexDataIndices, int vertexDataIndex)
        {
            var vertexData = vertexDataIndices[vertexDataIndex];
            geometry.AddVertex(_reader.AssetLoaderContext,
                vertexData.VertexIndex,
                position: ListUtils.FixIndex(vertexData.VertexIndex, _allVertices),
                normal: ListUtils.FixIndex(vertexData.NormalIndex, _allNormals),
                tangent: default,
                color: ListUtils.FixIndex(vertexData.ColorIndex, _allColors),
                uv0: ListUtils.FixIndex(vertexData.UvIndex, _allUVs));
        }

        
        private void ProcessMaterialLibrary(string mtLibName)
        {
            ObjMaterial material = null;
            var stream = _reader.ReadExternalFile(mtLibName);
            if (stream != null)
            {
                var objStreamReader = new ObjStreamReader(_reader.AssetLoaderContext, stream);
                while (!objStreamReader.EndOfStream)
                {
                    var command = objStreamReader.ReadToken(false);
                    switch (command)
                    {
                        case newmtl_token:
                            {
                                var materialName = objStreamReader.ReadTokenAsString(false, true, true, true);
                                materialName = materialName ?? DefaultElementName;
                                if (!_materialLibraries.TryGetValue(materialName, out var existingMaterial))
                                {
                                    material = new ObjMaterial { Name = materialName, Index = _materialLibraries.Count };
                                    _materialLibraries.Add(materialName, material);
                                }
                                else
                                {
                                    material = (ObjMaterial)existingMaterial;
                                }
                                break;
                            }
                        case Ka_token:
                        case Kd_token:
                        case Ks_token:
                        case Ke_token:
                            {
                                var propertyName = objStreamReader.GetTokenAsString();
                                var x = 0f;
                                var y = 0f;
                                var z = 0f;
                                var xToken = objStreamReader.ReadToken(false);
                                if (xToken == 0)
                                {
                                    goto addColor;
                                }
                                objStreamReader.GetTokenAsFloat(out x);
                                y = x;
                                z = x;
                                var yToken = objStreamReader.ReadToken(false);
                                if (yToken == 0)
                                {
                                    goto addColor;
                                }
                                objStreamReader.GetTokenAsFloat(out y);
                                var zToken = objStreamReader.ReadToken(false);
                                if (zToken == 0)
                                {
                                    goto addColor;
                                }
                                objStreamReader.GetTokenAsFloat(out z);
                                addColor:
                                material?.AddProperty(propertyName, new Color(x, y, z, 1f), false);
                                break;
                            }
                        case illum_token:
                        case d_token:
                        case Ns_token:
                        case Ni_token:
                        case Pr_token:
                        case Pm_token:
                        case Ps_token:
                        case Pc_token:
                        case Pcr_token:
                            {
                                var propertyName = objStreamReader.GetTokenAsString();
                                var value = objStreamReader.ReadTokenAsFloat();
                                material?.AddProperty(propertyName, value, false);
                                break;
                            }
                        case norm_token:
                            {
                                if (_reader.AssetLoaderContext.Options.ImportTextures)
                                {
                                    var propertyName = objStreamReader.GetTokenAsString();
                                    material?.AddProperty(propertyName, ProcessMap(objStreamReader, out var multiplier), true);
                                }
                                break;
                            }
                        default:
                            {
                                if (command != 0 && objStreamReader.TokenStartsWith("map_"))
                                {
                                    if (_reader.AssetLoaderContext.Options.ImportTextures)
                                    {
                                        var propertyName = objStreamReader.GetTokenAsString();
                                        material?.AddProperty(propertyName, ProcessMap(objStreamReader, out var multiplier), true);
                                    }
                                }
                                else
                                {
                                    while (command != 0)
                                    {
                                        command = objStreamReader.ReadToken(false, false, false, false);
                                    }
                                }
                                break;
                            }
                    }

                }
                stream.Close();
                objStreamReader.Dispose();
            }
            _processedMaterials.Add(mtLibName);
        }

        private ObjTexture ProcessMap(ObjStreamReader objStreamReader, out float multiplier)
        {
            var hasFilename = false;
            var texture = new ObjTexture();
            multiplier = 1f;
            for (; ; )
            {
                var token = objStreamReader.ReadToken(false, true, true, false, !hasFilename);
                if (token == 0)
                {
                    break;
                }
                switch (token)
                {
                    case _blenu_token:
                        {
                            var onOff = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _blenv_token:
                        {
                            var onOff = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _bm_token:
                        {
                            multiplier = objStreamReader.ReadTokenAsFloat(true);
                            break;
                        }
                    case _boost_token:
                        {
                            var value = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _cc_token:
                        {
                            var value = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _clamp_token:
                        {
                            var onOff = objStreamReader.ReadToken(true);
                            switch (onOff)
                            {
                                case on_token:
                                    texture.WrapModeU = TextureWrapMode.Clamp;
                                    texture.WrapModeV = TextureWrapMode.Clamp;
                                    break;
                                case off_token:
                                    texture.WrapModeU = TextureWrapMode.Repeat;
                                    texture.WrapModeV = TextureWrapMode.Repeat;
                                    break;
                            }

                            break;
                        }
                    case _imfchan_token:
                        {
                            var value = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _mm_token:
                        {
                            var @base = objStreamReader.ReadToken(true);
                            var gain = objStreamReader.ReadToken(true);
                            break;
                        }
                    case _o_token:
                        {
                            var x = objStreamReader.ReadTokenAsFloat(true);
                            var y = objStreamReader.ReadTokenAsFloat(true);
                            var z = objStreamReader.ReadTokenAsFloat(true);
                            texture.Offset = new Vector2(x, y);
                            break;
                        }
                    case _s_token:
                        {
                            var x = objStreamReader.ReadTokenAsFloat(true);
                            var y = objStreamReader.ReadTokenAsFloat(true);
                            var z = objStreamReader.ReadTokenAsFloat(true);
                            texture.Tiling = new Vector2(x, y);
                            break;
                        }
                    case _t_token:
                        {
                            var x = objStreamReader.ReadTokenAsFloat(true);
                            var y = objStreamReader.ReadTokenAsFloat(true);
                            var z = objStreamReader.ReadTokenAsFloat(true);
                            break;
                        }
                    case _texres_token:
                        {
                            var value = objStreamReader.ReadToken(true);
                            break;
                        }
                    default:
                        {
                            if (hasFilename && objStreamReader.TokenStartsWith("-"))
                            {
                                var value = objStreamReader.ReadToken(true);
                                //todo: stub
                                break;
                            }
                            texture.Filename = objStreamReader.GetTokenAsString();
                            hasFilename = true;
                            break;
                        }
                }
            }
            if (string.IsNullOrWhiteSpace(texture.Filename))
            {
                texture.Filename = $"{DefaultElementName}{++_unnamedTextureCount}";
            }
            if (_textures.TryGetValue(texture.Filename, out var existingTexture))
            {
                return (ObjTexture)existingTexture;
            }
            texture.Name = FileUtils.GetShortFilename(texture.Filename);
            if (texture.Filename != null)
            {
                texture.ResolveFilename(_reader.AssetLoaderContext);
            }
            _textures.Add(texture.Filename, texture);
            return texture;
        }
    }
}
