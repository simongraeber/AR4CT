using System;
using System.Collections.Generic;
using System.IO;
using TriLibCore.General;
using TriLibCore.Geometries;
using TriLibCore.Gltf.Reader;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private Dictionary<int, Matrix4x4> ConvertBindPoses(int skinIndex)
        {
            var skin = skins.GetArrayValueAtIndex(skinIndex);
            if (skin.TryGetChildWithKey(_joints_token, out var joints))
            {
                var bindPoses = new Dictionary<int, Matrix4x4>();
                IList<Matrix4x4> matrices;
                if (skin.TryGetChildValueAsInt(_inverseBindMatrices_token, out var inverseBindMatrices, _temporaryString))
                {
                    var accessor = accessors.GetArrayValueAtIndex(inverseBindMatrices);
                    matrices = ConvertAccessorDataMatrix4x4(accessor, true);
                }
                else
                {
                    matrices = new Matrix4x4[joints.Count];
                    for (var i = 0; i < matrices.Count; i++)
                    {
                        matrices[i] = RightHandToLeftHandConverter.ConvertMatrix(Matrix4x4.identity);
                    }
                }

                for (var i = 0; i < joints.Count; i++)
                {
                    var matrix = matrices[i];
                    var node = joints.GetArrayValueAtIndex(i).GetValueAsInt(_temporaryString);
                    bindPoses[node] = matrix;
                }

                return bindPoses;
            }

            return null;
        }

        private IList<int> ParseIndices(JsonParser.JsonValue primitive, int vertexCount)
        {
            IList<int> indices;
            if (primitive.TryGetChildValueAsInt(_indices_token, out var indicesIndex, _temporaryString))
            {
                var accessor = accessors.GetArrayValueAtIndex(indicesIndex);
                indices = ConvertAccessorDataInt(accessor);
            }
            else
            {
                indices = new int[vertexCount];
                for (var i = 0; i < vertexCount; i++)
                {
                    indices[i] = i;
                }
            }

            return indices;
        }

        private IGeometryGroup ConvertGeometryGroup(int i)
        {
            var mesh = meshes.GetArrayValueAtIndex(i);
            var meshWeights = mesh.GetChildWithKey(_weights_token);
            var meshPrimitives = mesh.GetChildWithKey(_primitives_token);
            var meshTargetNames = mesh.TryGetChildWithKey(_extras_token, out var meshExtras) ? meshExtras.GetChildWithKey(_targetNames_token) : default;
            var geometryHasSkin = !_usesDraco;
            var geometryHasUV4 = false;
            var geometryHasUV3 = false;
            var geometryHasUV2 = false;
            var geometryHasUV1 = false;
            var geometryHasColors = false;
            var geometryHasNormal = false;
            var geometryHasTangent = false;
            for (var geometryIndex = 0; geometryIndex < meshPrimitives.Count; geometryIndex++)
            {
                var meshPrimitive = meshPrimitives.GetArrayValueAtIndex(geometryIndex);
                var primitiveAttributes = meshPrimitive.GetChildWithKey(_attributes_token);
                geometryHasSkin |= primitiveAttributes.HasValue(_JOINTS_0_token);
                geometryHasUV4 |= primitiveAttributes.HasValue(_TEXCOORD_3_token);
                geometryHasUV3 |= primitiveAttributes.HasValue(_TEXCOORD_2_token);
                geometryHasUV2 |= primitiveAttributes.HasValue(_TEXCOORD_1_token);
                geometryHasUV1 |= primitiveAttributes.HasValue(_TEXCOORD_0_token);
                geometryHasColors |= primitiveAttributes.HasValue(_COLOR_0_token);
                var geometryHasTangentLocal = primitiveAttributes.HasValue(_TANGENT_token);
                geometryHasTangent |= geometryHasTangentLocal;
                geometryHasNormal |= primitiveAttributes.HasValue(_NORMAL_token) || geometryHasTangentLocal;
            }

            var geometryGroup = CommonGeometryGroup.Create(geometryHasNormal, geometryHasTangent, geometryHasColors, geometryHasUV1, geometryHasUV2, geometryHasUV3, geometryHasUV4, geometryHasSkin);
            geometryGroup.Name = mesh.GetChildValueAsString(_name_token, _temporaryString);

            var primitiveVertexIndex = 0;
            var vertexCount = 0;
            for (var geometryIndex = 0; geometryIndex < meshPrimitives.Count; geometryIndex++)
            {
                var meshPrimitive = meshPrimitives.GetArrayValueAtIndex(geometryIndex);
                var primitiveAttributes = meshPrimitive.GetChildWithKey(_attributes_token);

                if (_reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                {
                    if (primitiveAttributes.TryGetChildValueAsInt(_JOINTS_0_token, out var jointsIndex, _temporaryString) &&
                        primitiveAttributes.TryGetChildValueAsInt(_WEIGHTS_0_token, out var weightsIndex, _temporaryString))
                    {
                        var jointsAccessor = accessors.GetArrayValueAtIndex(jointsIndex);
                        var joints = ConvertAccessorDataVector4Int(jointsAccessor);
                        var weightsAccessor = accessors.GetArrayValueAtIndex(weightsIndex);
                        var weights = ConvertAccessorDataVector4(weightsAccessor);
                        for (var vertexIndex = 0; vertexIndex < joints.Count; primitiveVertexIndex++, vertexIndex++)
                        {
                            for (var k = 0; k < 4; k++)
                            {
                                var boneWeight = new BoneWeight1
                                {
                                    boneIndex = joints[vertexIndex][k],
                                    weight = weights[vertexIndex][k]
                                };
                                geometryGroup.AddBoneWeight(primitiveVertexIndex, boneWeight);
                            }
                        }
                    }
                }

                var meshPrimitiveAttributes = meshPrimitive.GetChildWithKey(_attributes_token);
                if (meshPrimitiveAttributes.TryGetChildValueAsInt(_POSITION_token, out var positionIndex, _temporaryString))
                {
                    var accessor = accessors.GetArrayValueAtIndex(positionIndex);
                    vertexCount += GetAccessorCount(accessor);
                }
            }

            geometryGroup.Setup(_reader.AssetLoaderContext, vertexCount, 1);

            primitiveVertexIndex = 0;

            for (var geometryIndex = 0; geometryIndex < meshPrimitives.Count; geometryIndex++)
            {
                var primitive = meshPrimitives.GetArrayValueAtIndex(geometryIndex);
                IList<int> indices;
                IList<Vector3> verticesList = null;
                IList<Vector3> normalsList = null;
                IList<Vector4> tangentsList = null;
                IList<Vector2> uvs = null;
                IList<Vector2> uvs2 = null;
                IList<Vector2> uvs3 = null;
                IList<Vector2> uvs4 = null;
                IList<Color> colorsList = null;
                bool primitiveHasSkin;
                JsonParser.JsonValue dracoMeshCompressionObject = default;
                var hasExtensions = primitive.TryGetChildWithKey(_extensions_token, out var extensionsObject);
                var hasDraco = hasExtensions && extensionsObject.TryGetChildWithKey(_KHR_draco_mesh_compression_token, out dracoMeshCompressionObject);
                if (hasDraco)
                {
                    if (GltfReader.DracoDecompressorCallback == null && _reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        throw new Exception("Mesh contains Draco compression but no DracoCompressionCallback was set for GltfReader.");
                    }
                    var bufferViewIndex = dracoMeshCompressionObject.GetChildValueAsInt(_bufferView_token, _temporaryString, 0);
                    var bufferView = bufferViews.GetArrayValueAtIndex(bufferViewIndex);
                    var bufferViewByteOffset = bufferView.GetChildValueAsInt(_byteOffset_token, _temporaryString, 0);
                    var bufferViewByteLength = bufferView.GetChildValueAsInt(_byteLength_token, _temporaryString, 0);
                    var bufferViewBuffer = bufferView.GetChildValueAsInt(_buffer_token, _temporaryString, 0);
                    var bufferData = _buffersData[bufferViewBuffer];
                    var compressedData = new byte[bufferViewByteLength];
                    var position = bufferData.Position;
                    bufferData.Seek(bufferViewByteOffset, SeekOrigin.Current);
                    for (var j = 0; j < bufferViewByteLength; j++)
                    {
                        var read = bufferData.ReadByte();
                        if (read > -1)
                        {
                            compressedData[j] = (byte)read;
                        }
                    }
                    bufferData.Seek(position, SeekOrigin.Begin);

                    if (GltfReader.DracoDecompressorCallback == null && _reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        throw new Exception("Unable to decompress Draco mesh. Please enable the Draco decompressor on the Edit->Project Settings->TriLib menu.");
                    }

                    var decompressedGeometryGroup = GltfReader.DracoDecompressorCallback(compressedData);
                    if (decompressedGeometryGroup == null && _reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        throw new Exception("Unable to decompress Draco mesh");
                    }

                    verticesList = decompressedGeometryGroup.Vertices;
                    var scaleFactor = _reader.AssetLoaderContext.Options.ScaleFactor;
                    for (var v = 0; v < verticesList.Count; v++)
                    {
                        var vertex = verticesList[v];
                        vertex *= scaleFactor;
                        verticesList[v] = vertex;
                    }

                    normalsList = decompressedGeometryGroup.NormalsList;
                    uvs = decompressedGeometryGroup.UVsList;
                    colorsList = decompressedGeometryGroup.ColorsList;
                    indices = decompressedGeometryGroup.IndicesList;
                    primitiveHasSkin = false;
                }
                else
                {
                    var primitiveAttributes = primitive.GetChildWithKey(_attributes_token);
                    if (primitiveAttributes.TryGetChildValueAsInt(_POSITION_token, out var positionIndex, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(positionIndex);
                        verticesList = ConvertAccessorDataVector3(accessor, true, true);
                    }
                    else
                    {
                        if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            UnityEngine.Debug.Log("Primitive has no vertices");
                        }

                        continue;
                    }

                    if (_reader.AssetLoaderContext.Options.ImportNormals && primitiveAttributes.TryGetChildValueAsInt(_NORMAL_token, out var normalsIndex, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(normalsIndex);
                        normalsList = ConvertAccessorDataVector3(accessor, true, false);
                        for (var j = 0; j < normalsList.Count; j++)
                        {
                            normalsList[j] = normalsList[j].normalized;
                        }
                    }

                    if (_reader.AssetLoaderContext.Options.ImportTangents && primitiveAttributes.TryGetChildValueAsInt(_TANGENT_token, out var tangentsIndex, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(tangentsIndex);
                        tangentsList = ConvertAccessorDataVector4(accessor, true);
                        for (var j = 0; j < tangentsList.Count; j++)
                        {
                            tangentsList[j] = tangentsList[j].normalized;
                        }
                    }

                    if (primitiveAttributes.TryGetChildValueAsInt(_TEXCOORD_0_token, out var uvIndex, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(uvIndex);
                        uvs = ConvertAccessorDataVector2(accessor, true);
                    }

                    if (primitiveAttributes.TryGetChildValueAsInt(_TEXCOORD_1_token, out var uvIndex2, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(uvIndex2);
                        uvs2 = ConvertAccessorDataVector2(accessor, true);
                    }

                    if (primitiveAttributes.TryGetChildValueAsInt(_TEXCOORD_2_token, out var uvIndex3, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(uvIndex3);
                        uvs3 = ConvertAccessorDataVector2(accessor, true);
                    }

                    if (primitiveAttributes.TryGetChildValueAsInt(_TEXCOORD_3_token, out var uvIndex4, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(uvIndex4);
                        uvs4 = ConvertAccessorDataVector2(accessor, true);
                    }

                    if (_reader.AssetLoaderContext.Options.ImportColors && primitiveAttributes.TryGetChildValueAsInt(_COLOR_0_token, out var colorIndex, _temporaryString))
                    {
                        var accessor = accessors.GetArrayValueAtIndex(colorIndex);
                        colorsList = ConvertAccessorDataColor(accessor);
                    }

                    primitiveHasSkin = primitiveAttributes.HasValue(_JOINTS_0_token);

                    indices = ParseIndices(primitive, verticesList.Count);
                    if (_reader.AssetLoaderContext.Options.ImportBlendShapes && primitive.TryGetChildWithKey(_targets_token, out var primitiveTargets))
                    {
                        for (var t = 0; t < primitiveTargets.Count; t++)
                        {
                            var gltfBlendShapeKey = new GltfBlendShapeKey { Name = meshTargetNames.Valid && t < meshTargetNames.Count ? meshTargetNames.GetArrayValueAtIndex(t).ToString() : $"BlendShape{t}" };
                            if (geometryGroup.BlendShapeKeys == null)
                            {
                                geometryGroup.BlendShapeKeys = new List<IBlendShapeKey>(primitiveTargets.Count);
                            }

                            geometryGroup.BlendShapeKeys.Add(gltfBlendShapeKey);
                            var primitiveTarget = primitiveTargets.GetArrayValueAtIndex(t);
                            gltfBlendShapeKey.FrameWeight = meshWeights.Valid && t < meshWeights.Count ? (float)meshWeights.GetArrayValueAtIndex(t).GetValueAsFloat(_temporaryString) : 1f;
                            gltfBlendShapeKey.FrameWeight = gltfBlendShapeKey.FrameWeight == 0f ? 1f : gltfBlendShapeKey.FrameWeight;
                            if (primitiveTarget.TryGetChildValueAsInt(_POSITION_token, out var targetPositionIndex, _temporaryString))
                            {
                                var accessor = accessors.GetArrayValueAtIndex(targetPositionIndex);
                                gltfBlendShapeKey.Vertices = ConvertAccessorDataVector3(accessor, true, true);
                            }

                            if (_reader.AssetLoaderContext.Options.ImportNormals && primitiveTarget.TryGetChildValueAsInt(_NORMAL_token, out var targetNormalsIndex, _temporaryString))
                            {
                                var accessor = accessors.GetArrayValueAtIndex(targetNormalsIndex);
                                gltfBlendShapeKey.Normals = ConvertAccessorDataVector3(accessor, true, false);
                            }

                            if (_reader.AssetLoaderContext.Options.ImportTangents && primitiveTarget.TryGetChildValueAsInt(_TANGENT_token, out var targetTangentIndex, _temporaryString))
                            {
                                var accessor = accessors.GetArrayValueAtIndex(targetTangentIndex);
                                gltfBlendShapeKey.Tangents = ConvertAccessorDataVector3(accessor, false, true);
                            }

                            var size = gltfBlendShapeKey.Vertices.Count;
                            var keyIndices = new List<int>(size);
                            for (var index = 0; index < size; index++)
                            {
                                keyIndices.Add(index);
                            }

                            gltfBlendShapeKey.IndexMap = new Dictionary<int, int>(keyIndices.Count);
                            for (var blendShapeVertexIndex = 0; blendShapeVertexIndex < keyIndices.Count; blendShapeVertexIndex++)
                            {
                                var originalVertexIndex = primitiveVertexIndex + keyIndices[blendShapeVertexIndex];
                                gltfBlendShapeKey.IndexMap.Add(originalVertexIndex, blendShapeVertexIndex);
                            }
                        }
                    }
                }

                var geometry = GetGeometry(geometryGroup, primitive.GetChildValueAsInt(_material_token, _temporaryString, -1));

                void AddVertex(int j)
                {
                    var index = indices[j];
                    var finalPrimitiveVertexIndex = primitiveVertexIndex + index;
                    geometry.AddVertex(_reader.AssetLoaderContext,
                        finalPrimitiveVertexIndex,
                        position: ListUtils.FixIndex(index, verticesList),
                        normal: ListUtils.FixIndex(index, normalsList),
                        tangent: ListUtils.FixIndex(index, tangentsList),
                        color: ListUtils.FixIndex(index, colorsList),
                        uv0: ListUtils.FixIndex(index, uvs),
                        uv1: ListUtils.FixIndex(index, uvs2),
                        uv2: ListUtils.FixIndex(index, uvs3), 
                        uv3: ListUtils.FixIndex(index, uvs4));
                }

                var primitiveMode = hasExtensions ? TRIANGLES : (int)primitive.GetChildValueAsInt(_mode_token, _temporaryString, TRIANGLES);
                switch (primitiveMode)
                {
                    case TRIANGLE_FAN when indices.Count >= 3:
                        {
                            if (hasExtensions)
                            {
                                for (var j = 1; j < indices.Count - 1; j++)
                                {
                                    AddVertex(0);
                                    AddVertex(j);
                                    AddVertex(j + 1);
                                }
                            }
                            else
                            {
                                for (var j = 1; j < indices.Count - 1; j++)
                                {
                                    AddVertex(j + 1);
                                    AddVertex(j);
                                    AddVertex(0);
                                }
                            }

                            break;
                        }
                    case TRIANGLE_STRIP:
                        {
                            if (hasExtensions)
                            {
                                for (var j = 0; j < indices.Count - 2; j++)
                                {
                                    if (j % 2 > 0)
                                    {
                                        AddVertex(j + 1);
                                        AddVertex(j);
                                        AddVertex(j + 2);
                                    }
                                    else
                                    {
                                        AddVertex(j);
                                        AddVertex(j + 1);
                                        AddVertex(j + 2);
                                    }
                                }
                            }
                            else
                            {
                                for (var j = 0; j < indices.Count - 2; j++)
                                {
                                    if (j % 2 > 0)
                                    {
                                        AddVertex(j + 2);
                                        AddVertex(j);
                                        AddVertex(j + 1);
                                    }
                                    else
                                    {
                                        AddVertex(j + 2);
                                        AddVertex(j + 1);
                                        AddVertex(j);
                                    }
                                }
                            }

                            break;
                        }
                    case POINTS:
                    case LINES:
                    case LINE_LOOP:
                    case LINE_STRIP:
                        if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            UnityEngine.Debug.Log($"Unsupported primitive mode:{primitiveMode}");
                        }

                        continue;
                    default:
                        {
                            if (hasExtensions)
                            {
                                for (var j = 0; j < indices.Count; j++)
                                {
                                    AddVertex(j);
                                }
                            }
                            else
                            {
                                for (var j = indices.Count - 1; j >= 0; j--)
                                {
                                    AddVertex(j);
                                }
                            }

                            break;
                        }
                }

                if (primitiveHasSkin)
                {
                    primitiveVertexIndex += verticesList.Count;
                }
            }

            return geometryGroup;
        }

        private GltfGeometry GetGeometry(IGeometryGroup geometryGroup, int materialIndex)
        {
            var geometry = geometryGroup.GetGeometry<GltfGeometry>(_reader.AssetLoaderContext, materialIndex, false, geometryGroup.BlendShapeKeys?.Count > 0);
            return geometry;
        }
    }
}