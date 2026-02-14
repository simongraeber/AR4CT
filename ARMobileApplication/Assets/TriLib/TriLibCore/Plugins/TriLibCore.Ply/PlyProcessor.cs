using System;
using System.Collections.Generic;
using System.IO;
using LibTessDotNet;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Ply.Reader;
using TriLibCore.Textures;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Ply
{
    public partial class PlyProcessor
    {
        private PlyReader _reader;
        private IList<Vector3> _allVertices;
        private IList<Vector3> _allNormals;
        private IList<Color> _allColors;
        private IList<Vector2> _allUVs;
        private IGeometryGroup _geometryGroup;

        private const long _ply_token = -3351804022671184904;
        private const long _end_header_token = -3837289489351516138;
        private const long _format_token = -1367968407750199268;
        private const long _binary_little_endian_token = 6570124500183046315;
        private const long _binary_big_endian_token = 2560988747252765043;
        private const long _element_token = -5513532493822467209;
        private const long _property_token = -4898810336857675590;
        private const long _comment_token = -5513532495504198950;
        private const long _TextureFile_token = -8289663717619634158;

        private const long _material_token = -4898810434349715444;
        private const long _vertex_token = -1367968407301361207;
        private const long _diffuse_red_token = -8276459643725187793;
        private const long _diffuse_green_token = -3131021851098489823;
        private const long _diffuse_blue_token = 1684168076452431740;
        private const long _specular_red_token = 2071003623302517242;
        private const long _specular_green_token = -2013877966922272212;
        private const long _specular_blue_token = 8860880101248910353;
        private const long _x_token = 34902897112120627;
        private const long _y_token = 34902897112120628;
        private const long _z_token = 34902897112120629;
        private const long _nx_token = 1081989810475739247;
        private const long _ny_token = 1081989810475739248;
        private const long _nz_token = 1081989810475739249;
        private const long _u_token = 34902897112120624;
        private const long _v_token = 34902897112120625;
        private const long _s_token = 34902897112120622;
        private const long _t_token = 34902897112120623;
        private const long _texture_u_token = -4289164790894553076;
        private const long _texture_v_token = -4289164790894553075;
        private const long _red_token = -3351804022671183220;
        private const long _green_token = 7096547112154691134;
        private const long _blue_token = 6774539739450160575;
        private const long _alpha_token = 7096547112148981913;
        private const long _face_token = 6774539739450268610;
        private const long _texcoord_token = -4898810238098815469;
        private const long _materialIndex_token = 3766120907217874982;
        private const long _vertex_indices_token = 7578069101293603633;
        private const long _vertex_index_token = 2138570927453749980;
        private const long _int8_token = 6774539739450370958;
        private const long _char_token = 6774539739450185915;
        private const long _uint8_token = 7096547112167361369;
        private const long _uchar_token = 7096547112167176326;
        private const long _int16_token = 7096547112156431759;
        private const long _short_token = 7096547112165485495;
        private const long _uint16_token = -1367968407326417116;
        private const long _ushort_token = -1367968407317363380;
        private const long _int32_token = 7096547112156431817;
        private const long _int_token = -3351804022671191574;
        private const long _uint32_token = -1367968407326417058;
        private const long _uint_token = 6774539739450723519;
        private const long _float_token = 7096547112153598359;
        private const long _float32_token = -5513532492926073290;
        private const long _double_token = -1367968407807378442;
        private const long _float64_token = -5513532492926073195;
        private const long _list_token = 6774539739450455555;
        private const long _opacity_token = -5513532484836901754;

        private const int _verticesCapacity = 3;

        public IRootModel Process(PlyReader plyReader, Stream stream)
        {
            _allVertices = new List<Vector3>(_verticesCapacity);
            _allNormals = new List<Vector3>(_verticesCapacity);
            _allColors = new List<Color>(_verticesCapacity);
            _allUVs = new List<Vector2>(_verticesCapacity);
            _reader = plyReader;
            var elements = new Dictionary<long, PlyElement>();
            var lists = new List<List<PlyValue>>();
            PlyElement activeElement = null;
            var headerOpen = false;
            var headerClosed = false;
            var textureFileOpen = false;
            PlyTexture textureFile = null;
            var allTextures = new List<ITexture>();
            var littleEndian = false;
            var bigEndian = false;
            var plyStreamReader = new PlyStreamReader(_reader.AssetLoaderContext, stream);
            var lastPerc = 0f;
            while (!plyStreamReader.EndOfStream && !headerClosed)
            {
                var command = plyStreamReader.ReadToken(false);
                switch (command)
                {
                    case _ply_token:
                        {
                            headerOpen = true;
                            break;
                        }
                    case _end_header_token:
                        {
                            if (!headerOpen)
                            {
                                throw new Exception("Expected: ply.");
                            }
                            plyStreamReader.ReadToken(false);
                            headerClosed = true;
                            break;
                        }
                    case _format_token:
                        {
                            var format = plyStreamReader.ReadToken(true);
                            littleEndian = format == _binary_little_endian_token;
                            bigEndian = format == _binary_big_endian_token;
                            plyStreamReader.ReadToken(true);
                            break;
                        }
                    case _element_token:
                        {
                            var elementName = plyStreamReader.ReadToken(true);
                            var elementCount = plyStreamReader.ToInt32NoValue();
                            activeElement = new PlyElement { Count = elementCount };
                            elements.Add(elementName, activeElement);
                            break;
                        }
                    case _property_token:
                        {
                            if (activeElement == null)
                            {
                                throw new Exception("Expected: element.");
                            }
                            var propertyType = GetPropertyType(plyStreamReader);
                            if (propertyType == PlyPropertyType.List)
                            {
                                var counterPropertyType = GetPropertyType(plyStreamReader);
                                var itemPropertyType = GetPropertyType(plyStreamReader);
                                var propertyName = plyStreamReader.ReadToken(true);
                                var property = new PlyListProperty { Type = propertyType, CounterType = counterPropertyType, ItemType = itemPropertyType, Offset = activeElement.Properties.Count };
                                activeElement.Properties.Add(propertyName, property);
                            }
                            else
                            {
                                var propertyName = plyStreamReader.ReadToken(true);
                                var property = new PlyProperty { Type = propertyType, Offset = activeElement.Properties.Count };
                                activeElement.Properties.Add(propertyName, property);
                            }
                            break;
                        }
                    case _comment_token:
                        {
                            while (command != 0)
                            {
                                command = plyStreamReader.ReadToken(false);
                                if (textureFileOpen)
                                {
                                    textureFile = new PlyTexture();
                                    textureFile.Filename = plyStreamReader.GetTokenAsString();
                                    textureFile.ResolveFilename(_reader.AssetLoaderContext);
                                    textureFileOpen = false;
                                    allTextures.Add(textureFile);
                                }
                                else if (command == _TextureFile_token)
                                {
                                    textureFileOpen = true;
                                }
                            }
                            break;
                        }
                    default:
                        {
                            while (command != 0)
                            {
                                command = plyStreamReader.ReadToken(false);
                            }
                            break;
                        }
                }
                var perc = (float)plyStreamReader.BaseStream.Position / plyStreamReader.BaseStream.Length;
                if (perc > lastPerc + 0.33f)
                {

                    _reader.UpdateLoadingPercentage(perc, (int)PlyReader.ProcessingSteps.Parsing);
                    lastPerc = perc;
                }
            }
            plyReader.UpdateLoadingPercentage(1f, (int)PlyReader.ProcessingSteps.Parsing);
            BinaryReader littleEndianBinaryReader;
            if (littleEndian)
            {
                littleEndianBinaryReader = new BinaryReader(plyStreamReader.BaseStream);
                littleEndianBinaryReader.BaseStream.Position = plyStreamReader.Position;
            }
            else
            {
                littleEndianBinaryReader = null;
            }
            BigEndianBinaryReader bigEndianBinaryReader;
            if (bigEndian)
            {
                bigEndianBinaryReader = new BigEndianBinaryReader(plyStreamReader.BaseStream);
                bigEndianBinaryReader.BaseStream.Position = plyStreamReader.Position;
            }
            else
            {
                bigEndianBinaryReader = null;
            }
            foreach (var element in elements.Values)
            {
                var elementData = new List<List<PlyValue>>(element.Count);
                for (var i = 0; i < element.Count; i++)
                {
                    var subElementData = new List<PlyValue>(element.Properties.Count);
                    foreach (var property in element.Properties.Values)
                    {
                        var value = ReadElementData(property, property.Type, littleEndian, littleEndianBinaryReader, bigEndian, plyStreamReader, bigEndianBinaryReader, lists);
                        subElementData.Add(value);
                    }
                    elementData.Add(subElementData);
                }
                element.Data = elementData;
            }

            List<IMaterial> allMaterials = null;
            if (!plyReader.AssetLoaderContext.Options.LoadPointClouds && plyReader.AssetLoaderContext.Options.ImportMaterials)
            {
                if (elements.TryGetValue(_material_token, out var materialElement))
                {
                    allMaterials = new List<IMaterial>(materialElement.Count);
                    for (var i = 0; i < materialElement.Count; i++)
                    {
                        var material = new PlyMaterial();
                        var diffuse_red = 255.0f / materialElement.GetPropertyFloatValue(_diffuse_red_token, i);
                        var diffuse_green = 255.0f / materialElement.GetPropertyFloatValue(_diffuse_green_token, i);
                        var diffuse_blue = 255.0f / materialElement.GetPropertyFloatValue(_diffuse_blue_token, i);
                        material.AddProperty("diffuse", new Color(diffuse_red, diffuse_green, diffuse_blue), false);
                        var specular_red = 255.0f / materialElement.GetPropertyFloatValue(_specular_red_token, i);
                        var specular_green = 255.0f / materialElement.GetPropertyFloatValue(_specular_green_token, i);
                        var specular_blue = 255.0f / materialElement.GetPropertyFloatValue(_specular_blue_token, i);
                        material.AddProperty("specular", new Color(specular_red, specular_green, specular_blue), false);
                        var opacityProperty = materialElement.GetProperty(_opacity_token);
                        var opacity = opacityProperty == null ? 1f : materialElement.GetPropertyFloatValue(opacityProperty, i);
                        material.AddProperty("opacity", opacity, false);
                        material.Index = i;
                        if (textureFile != null)
                        {
                            material.AddProperty("diffuseTex", textureFile, true);
                        }
                        allMaterials.Add(material);
                    }
                }
                if (allMaterials == null && textureFile != null)
                {
                    var material = new PlyMaterial();
                    material.AddProperty("diffuseTex", textureFile, true);
                    allMaterials = new List<IMaterial>() { material };
                }
                plyReader.UpdateLoadingPercentage(1f, (int)PlyReader.ProcessingSteps.ConvertMaterials);
            }

            if (plyReader.AssetLoaderContext.Options.ImportMeshes && elements.TryGetValue(_vertex_token, out var vertexElement))
            {
                for (var i = 0; i < vertexElement.Count; i++)
                {
                    var x = vertexElement.GetPropertyFloatValue(_x_token, i);
                    var y = vertexElement.GetPropertyFloatValue(_y_token, i);
                    var z = vertexElement.GetPropertyFloatValue(_z_token, i);
                    var vertex = new Vector3(-x, y, z) * _reader.AssetLoaderContext.Options.ScaleFactor;
                    _allVertices.Add(vertex);
                    var nxProp = vertexElement.GetProperty(_nx_token);
                    var nyProp = vertexElement.GetProperty(_ny_token);
                    var nzProp = vertexElement.GetProperty(_nz_token);
                    if (nxProp != null && nyProp != null && nzProp != null)
                    {
                        var nx = vertexElement.GetPropertyFloatValue(nxProp, i);
                        var ny = vertexElement.GetPropertyFloatValue(nyProp, i);
                        var nz = vertexElement.GetPropertyFloatValue(nzProp, i);
                        var normal = new Vector3(nx, ny, nz);
                        _allNormals.Add(normal);
                    }
                    var sProp = vertexElement.GetProperty(_u_token) ?? vertexElement.GetProperty(_s_token) ?? vertexElement.GetProperty(_texture_u_token);
                    var tProp = vertexElement.GetProperty(_v_token) ?? vertexElement.GetProperty(_t_token) ?? vertexElement.GetProperty(_texture_v_token);
                    if (sProp != null && tProp != null)
                    {
                        var s = vertexElement.GetPropertyFloatValue(sProp, i);
                        var t = vertexElement.GetPropertyFloatValue(tProp, i);
                        var uv = new Vector2(s, t);
                        _allUVs.Add(uv);
                    }
                    var rProp = vertexElement.GetProperty(_red_token) ?? vertexElement.GetProperty(_diffuse_red_token);
                    var gProp = vertexElement.GetProperty(_green_token) ?? vertexElement.GetProperty(_diffuse_blue_token);
                    var bProp = vertexElement.GetProperty(_blue_token) ?? vertexElement.GetProperty(_diffuse_green_token);
                    var aProp = vertexElement.GetProperty(_alpha_token);
                    if (rProp != null || gProp != null || bProp != null || aProp != null)
                    {
                        var r = rProp == null ? 1f : vertexElement.GetPropertyFloatValue(rProp, i);
                        var g = gProp == null ? 1f : vertexElement.GetPropertyFloatValue(gProp, i);
                        var b = bProp == null ? 1f : vertexElement.GetPropertyFloatValue(bProp, i);
                        var a = aProp == null ? 1f : vertexElement.GetPropertyFloatValue(aProp, i);
                        var color = new Color(r / 255f, g / 255f, b / 255f, a / 255f);
                        _allColors.Add(color);
                    }
                }
                plyReader.UpdateLoadingPercentage(1f, (int)PlyReader.ProcessingSteps.ConvertGeometryGroups);

            }
            _geometryGroup = CommonGeometryGroup.Create(_allNormals.Count > 0, false, _allColors.Count > 0, _allUVs.Count > 0, false, false, false, false);
            _geometryGroup.Setup(_reader.AssetLoaderContext, _verticesCapacity, 1);
            if (plyReader.AssetLoaderContext.Options.LoadPointClouds)
            {
                var geometry = _geometryGroup.GetGeometry<PlyGeometry>(_reader.AssetLoaderContext, 0, false, false);
                for (var i = 0; i < _allVertices.Count; i++)
                {
                    var vertex = _allVertices[i];
                    var normal = ListUtils.FixIndex(i, _allNormals);
                    var color = ListUtils.FixIndex(i, _allColors);
                    var uv = ListUtils.FixIndex(i, _allUVs);
                    geometry.AddVertex(plyReader.AssetLoaderContext,
                        i,
                        position: vertex,
                        normal: normal,
                        tangent: default,
                        color: color, 
                        uv0: uv);
                }

            }
            else if (plyReader.AssetLoaderContext.Options.ImportMeshes && elements.TryGetValue(_face_token, out var faceElement))
            {
                for (var j = 0; j < faceElement.Count; j++)
                {
                    List<PlyValue> texCoord;
                    var texCoordProp = faceElement.GetProperty(_texcoord_token) as PlyListProperty;
                    if (texCoordProp != null)
                    {
                        var texCoordListIndex = faceElement.GetListIndex(texCoordProp, j);
                        texCoord = PlyValue.GetListValue(texCoordListIndex, lists);
                    }
                    else
                    {
                        texCoord = null;
                    }

                    var materialIndexProperty = faceElement.GetProperty(_materialIndex_token);
                    var materialIndex = materialIndexProperty != null ? faceElement.GetPropertyIntValue(materialIndexProperty, j) : 0;

                    var triGeometry = _geometryGroup.GetGeometry<PlyGeometry>(_reader.AssetLoaderContext, materialIndex, false, false);
                    var quadGeometry = _reader.AssetLoaderContext.Options.KeepQuads ? _geometryGroup.GetGeometry<PlyGeometry>(_reader.AssetLoaderContext, materialIndex, true, false) : null;

                    var vertexIndicesProp = (faceElement.GetProperty(_vertex_indices_token) ?? faceElement.GetProperty(_vertex_index_token)) as PlyListProperty;
                    var vertexIndicesListIndex = faceElement.GetListIndex(vertexIndicesProp, j);
                    var vertexIndices = PlyValue.GetListValue(vertexIndicesListIndex, lists);
                    if (vertexIndices.Count == 3)
                    {
                        for (var i = 2; i >= 0; i--)
                        {
                            AddVertex(PlyValue.GetIntValue(vertexIndices[i], vertexIndicesProp.ItemType), triGeometry, texCoord, i, texCoordProp);
                        }
                    }
                    else if (vertexIndices.Count == 4)
                    {
                        if (quadGeometry != null)
                        {
                            AddVertex(PlyValue.GetIntValue(vertexIndices[3], vertexIndicesProp.ItemType), quadGeometry, texCoord, 3, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[2], vertexIndicesProp.ItemType), quadGeometry, texCoord, 2, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[1], vertexIndicesProp.ItemType), quadGeometry, texCoord, 1, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[0], vertexIndicesProp.ItemType), quadGeometry, texCoord, 0, texCoordProp);
                        }
                        else
                        {
                            AddVertex(PlyValue.GetIntValue(vertexIndices[0], vertexIndicesProp.ItemType), triGeometry, texCoord, 0, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[3], vertexIndicesProp.ItemType), triGeometry, texCoord, 3, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[2], vertexIndicesProp.ItemType), triGeometry, texCoord, 2, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[2], vertexIndicesProp.ItemType), triGeometry, texCoord, 2, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[1], vertexIndicesProp.ItemType), triGeometry, texCoord, 1, texCoordProp);
                            AddVertex(PlyValue.GetIntValue(vertexIndices[0], vertexIndicesProp.ItemType), triGeometry, texCoord, 0, texCoordProp);
                        }
                    }
                    else
                    {
                        if (vertexIndices.Count < 256)
                        {
                            var contourVertices = new ContourVertex[vertexIndices.Count];
                            for (var i = 0; i < vertexIndices.Count; i++)
                            {
                                var index = vertexIndices[i];
                                var vertex = ListUtils.FixIndex(PlyValue.GetIntValue(index, vertexIndicesProp.ItemType), _allVertices);
                                contourVertices[i] = new ContourVertex(new Vec3(vertex.x, vertex.y, vertex.z), BuildVertexData(triGeometry, _geometryGroup, PlyValue.GetIntValue(vertexIndices[i], vertexIndicesProp.ItemType), texCoord, texCoordProp, i));
                            }
                            Helpers.Tesselate(contourVertices, _reader.AssetLoaderContext, triGeometry, _geometryGroup, true);
                        }
                    }
                }
                plyReader.UpdateLoadingPercentage(1f, (int)PlyReader.ProcessingSteps.PostProcessGeometries);

            }

            plyStreamReader.Dispose();

            var rootModel = new PlyRootModel
            {
                Visibility = true,
                LocalScale = Vector3.one,
                LocalRotation = Quaternion.identity,
                GeometryGroup = _geometryGroup,
                AllMaterials = allMaterials,
                AllTextures = allTextures,
                AllGeometryGroups = new List<IGeometryGroup>() { _geometryGroup }
            };

            if (allMaterials != null)
            {
                var materialIndices = new int[allMaterials.Count];
                for (var i = 0; i < materialIndices.Length; i++)
                {
                    materialIndices[i] = i;
                }
                rootModel.MaterialIndices = materialIndices;
            }

            rootModel.AllModels = new List<IModel>() { rootModel };

            return rootModel;
        }

        private IVertexData BuildVertexData(PlyGeometry triGeometry, IGeometryGroup geometryGroup, int vertexIndex, List<PlyValue> texCoord, PlyListProperty texCoordProp, int texCoordIndex = -1)
        {
            Vector2 uv;
            if (texCoord != null)
            {
                var u = PlyValue.GetFloatValue(texCoord[texCoordIndex * 2 + 0], texCoordProp);
                var v = PlyValue.GetFloatValue(texCoord[texCoordIndex * 2 + 1], texCoordProp);
                uv = new Vector2(u, v);
            }
            else
            {
                uv = ListUtils.FixIndex(vertexIndex, _allUVs);
            }
            var position = ListUtils.FixIndex(vertexIndex, _allVertices);
            var normal = ListUtils.FixIndex(vertexIndex, _allNormals);
            var color = ListUtils.FixIndex(vertexIndex, _allColors);
            var interpolatedVertex = new InterpolatedVertex(position);
            interpolatedVertex.SetVertexIndex(vertexIndex, geometryGroup);
            interpolatedVertex.SetNormal(normal, geometryGroup);
            interpolatedVertex.SetColor(color, geometryGroup);
            interpolatedVertex.SetUV1(uv, geometryGroup);
            return interpolatedVertex;
        }

        private void AddVertex(int vertexIndex, PlyGeometry geometry, List<PlyValue> texCoord, int texCoordIndex = -1, PlyProperty texCoordProp = null)
        {
            Vector2 uv;
            if (texCoord != null)
            {
                var u = PlyValue.GetFloatValue(texCoord[texCoordIndex * 2 + 0], texCoordProp);
                var v = PlyValue.GetFloatValue(texCoord[texCoordIndex * 2 + 1], texCoordProp);
                uv = new Vector2(u, v);
            }
            else
            {
                uv = ListUtils.FixIndex(vertexIndex, _allUVs);
            }
            geometry.AddVertex(_reader.AssetLoaderContext,
                vertexIndex,
                position: ListUtils.FixIndex(vertexIndex, _allVertices),
                normal: ListUtils.FixIndex(vertexIndex, _allNormals),
                tangent: default,
                color: ListUtils.FixIndex(vertexIndex, _allColors), uv0: uv);
        }

        private static bool CanRead(BinaryReader binaryReader, int size)
        {
            return binaryReader.BaseStream.Position < binaryReader.BaseStream.Length - size;
        }

        private static PlyValue ReadElementData(PlyProperty property, PlyPropertyType propertyType, bool littleEndian, BinaryReader binaryReader, bool bigEndian, PlyStreamReader plyStreamReader, BigEndianBinaryReader bigEndianBinaryReader, List<List<PlyValue>> lists)
        {
            PlyValue value;
            switch (propertyType)
            {
                case PlyPropertyType.Char:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(SByte)) ? (PlyValue)binaryReader.ReadSByte() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(SByte)) ? (PlyValue)bigEndianBinaryReader.ReadSByte() : default;
                        }
                        else
                        {
                            value = plyStreamReader.ToSByte();
                        }
                    }
                    break;
                case PlyPropertyType.UChar:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(Byte)) ? (PlyValue)binaryReader.ReadByte() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(Byte)) ? (PlyValue)bigEndianBinaryReader.ReadByte() : default;
                        }
                        else
                        {
                            value = (plyStreamReader.ToByte());
                        }
                    }
                    break;
                case PlyPropertyType.Short:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(Int16)) ? (PlyValue)binaryReader.ReadInt16() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(Int16)) ? (PlyValue)bigEndianBinaryReader.ReadInt16() : default;
                        }
                        else
                        {
                            value = (plyStreamReader.ToInt16());
                        }
                    }
                    break;
                case PlyPropertyType.UShort:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(UInt16)) ? (PlyValue)binaryReader.ReadUInt16() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(UInt16)) ? (PlyValue)bigEndianBinaryReader.ReadUInt16() : default;
                        }
                        else
                        {
                            value = (plyStreamReader.ToUInt16());
                        }
                    }
                    break;
                case PlyPropertyType.Int:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(Int32)) ? (PlyValue)binaryReader.ReadInt32() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(Int32)) ? (PlyValue)bigEndianBinaryReader.ReadInt32() : default;
                        }
                        else
                        {
                            value = (plyStreamReader.ToInt32());
                        }
                    }
                    break;
                case PlyPropertyType.UInt:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(UInt32)) ? (PlyValue)binaryReader.ReadUInt32() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(UInt32)) ? (PlyValue)bigEndianBinaryReader.ReadUInt32() : default;
                        }
                        else
                        {
                            value = plyStreamReader.ToUInt32();
                        }
                    }
                    break;
                case PlyPropertyType.Float:
                    if (littleEndian)
                    {
                        value = CanRead(binaryReader, sizeof(Single)) ? (PlyValue)binaryReader.ReadSingle() : default;
                    }
                    else
                    {
                        if (bigEndian)
                        {
                            value = CanRead(bigEndianBinaryReader, sizeof(Single)) ? (PlyValue)bigEndianBinaryReader.ReadSingle() : default;
                        }
                        else
                        {
                            value = plyStreamReader.ToSingle();
                        }
                    }
                    break;
                case PlyPropertyType.Double:
                    {
                        if (littleEndian)
                        {
                            value = CanRead(binaryReader, sizeof(Double)) ? (PlyValue)(PlyReader.PlyConversionPrecision * binaryReader.ReadDouble()) : default;
                        }
                        else
                        {
                            if (bigEndian)
                            {
                                value = CanRead(bigEndianBinaryReader, sizeof(Double)) ? (PlyValue)(PlyReader.PlyConversionPrecision * bigEndianBinaryReader.ReadDouble()) : default;
                            }
                            else
                            {
                                value = plyStreamReader.ToDouble();
                            }
                        }
                    }
                    break;
                case PlyPropertyType.List:
                    var listProperty = (PlyListProperty)property;
                    var listCountValue = ReadElementData(property, listProperty.CounterType, littleEndian, binaryReader, bigEndian, plyStreamReader, bigEndianBinaryReader, lists);
                    var listCount = PlyValue.GetIntValue(listCountValue, listProperty.CounterType);
                    var list = new List<PlyValue>(listCount);
                    for (var i = 0; i < listCount; i++)
                    {
                        var itemValue = ReadElementData(property, listProperty.ItemType, littleEndian, binaryReader, bigEndian, plyStreamReader, bigEndianBinaryReader, lists);
                        list.Add(itemValue);
                    }
                    value = lists.Count;
                    lists.Add(list);
                    break;
                default:
                    value = PlyValue.Unknown;
                    break;
            }
            return value;
        }

        private static PlyPropertyType GetPropertyType(PlyStreamReader plyStreamReader)
        {
            PlyPropertyType propertyType;
            var propertyTypeName = plyStreamReader.ReadToken(true);
            switch (propertyTypeName)
            {
                case _int8_token:
                case _char_token:
                    propertyType = PlyPropertyType.Char;
                    break;
                case _uint8_token:
                case _uchar_token:
                    propertyType = PlyPropertyType.UChar;
                    break;
                case _int16_token:
                case _short_token:
                    propertyType = PlyPropertyType.Short;
                    break;
                case _uint16_token:
                case _ushort_token:
                    propertyType = PlyPropertyType.UShort;
                    break;
                case _int32_token:
                case _int_token:
                    propertyType = PlyPropertyType.Int;
                    break;
                case _uint32_token:
                case _uint_token:
                    propertyType = PlyPropertyType.UInt;
                    break;
                case _float_token:
                case _float32_token:
                    propertyType = PlyPropertyType.Float;
                    break;
                case _double_token:
                case _float64_token:
                    propertyType = PlyPropertyType.Double;
                    break;
                case _list_token:
                    propertyType = PlyPropertyType.List;
                    break;
                default:
                    propertyType = PlyPropertyType.Custom;
                    break;
            }
            return propertyType;
        }
    }

    public struct PlyValue
    {
        private uint _intValue;
        public static PlyValue Unknown;

        public static implicit operator PlyValue(sbyte other)
        {
            return new PlyValue { _intValue = (uint)(other + sbyte.MaxValue) };
        }

        public static implicit operator PlyValue(byte other)
        {
            return new PlyValue { _intValue = other };
        }

        public static implicit operator PlyValue(ushort other)
        {
            return new PlyValue { _intValue = other };
        }

        public static implicit operator PlyValue(short other)
        {
            return new PlyValue { _intValue = (uint)(other + short.MaxValue) };
        }

        public static implicit operator PlyValue(uint other)
        {
            return new PlyValue { _intValue = other };
        }

        public static implicit operator PlyValue(int other)
        {
            return new PlyValue { _intValue = (uint)(other + int.MaxValue) };
        }

        public static implicit operator PlyValue(float other)
        {
            unsafe
            {
                return new PlyValue { _intValue = (uint)*(int*)&other };
            }
        }

        public static implicit operator PlyValue(double other)
        {
            unsafe
            {
                var value = (float)other;
                return new PlyValue { _intValue = (uint)*(int*)&value };
            }
        }

        public static int GetIntValue(PlyValue value, PlyProperty property)
        {
            return GetIntValue(value, property.Type);
        }

        public static int GetIntValue(PlyValue value, PlyPropertyType propertyType)
        {
            switch (propertyType)
            {
                case PlyPropertyType.UChar:
                case PlyPropertyType.UInt:
                case PlyPropertyType.UShort:
                    return (int)value._intValue;
                case PlyPropertyType.Char:
                    return (int)(value._intValue - sbyte.MaxValue);
                case PlyPropertyType.Short:
                    return (int)(value._intValue - short.MaxValue);
                case PlyPropertyType.Int:
                    return (int)(value._intValue - int.MaxValue);
                case PlyPropertyType.Double:
                case PlyPropertyType.Float:
                    unsafe
                    {
                        return (int)*(float*)&value._intValue;
                    }

                case PlyPropertyType.List:
                case PlyPropertyType.Custom:
                    return 0;
            }

            return 0;
        }

        public static float GetFloatValue(PlyValue value, PlyProperty property)
        {
            return GetFloatValue(value, property.Type);
        }

        private static float GetFloatValue(PlyValue value, PlyPropertyType propertyType)
        {
            switch (propertyType)
            {
                case PlyPropertyType.UChar:
                case PlyPropertyType.UInt:
                case PlyPropertyType.UShort:
                    return (float)value._intValue;
                case PlyPropertyType.Char:
                    return (float)(value._intValue - sbyte.MaxValue);
                case PlyPropertyType.Short:
                    return (float)(value._intValue - short.MaxValue);
                case PlyPropertyType.Int:
                    return (float)(value._intValue - int.MaxValue);
                case PlyPropertyType.Double:
                case PlyPropertyType.Float:
                    unsafe
                    {
                        return *(float*)&value._intValue;
                    }

                case PlyPropertyType.List:
                case PlyPropertyType.Custom:
                    return 0f;
            }

            return 0f;
        }

        public static List<PlyValue> GetListValue(int index, List<List<PlyValue>> lists)
        {
            return index < lists.Count ? lists[index] : null;
        }
    }
}