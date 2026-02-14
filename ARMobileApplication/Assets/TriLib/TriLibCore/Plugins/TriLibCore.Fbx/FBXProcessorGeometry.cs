using System;
using System.Collections.Generic;
using LibTessDotNet;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _Vertices_token = -4898811063633504514;
        private const long _Normals_token = -5513532514137162553;
        private const long _ByVertex_token = -4898811596950897696;
        private const long _Materials_token = -4289198167677963833;
        private const long _NormalsIndex_token = 1130422044072632395;
        private const long _NormalsW_token = -4898811274866074512;
        private const long _Tangents_token = -4898811122335377965;
        private const long _TangentsIndex_token = 2516419562778676927;
        private const long _TangentsW_token = -4289192202720303900;
        private const long _Colors_token = -1367968408752395851;
        private const long _ColorIndex_token = -3838187349052149516;
        private const long _UV_token = 1081989810475738438;
        private const long _UVIndex_token = -5513532508678197460;
        private const long _MappingInformationType_token = -2196838938951700963;
        private const long _ReferenceInformationType_token = 3847264155506286784;
        private const long _Direct_token = -1367968408729139154;
        private const long _IndexToDirect_token = -5825794537507908271;
        private const long _Index_token = 7096547112126865389;
        private const long _ByPolygonVertex_token = -339791105047651902;
        private const long _ByVertice_token = -4289206915801412354;
        private const long _ByPolygon_token = -4289206920845536642;
        private const long _AllSame_token = -5513532525766913534;
        private const long _Mapping_mode_not_supported_token = -1683759262767365061;
        private const long _LayerElementMaterial_token = 547854425036223031;
        private const long _LayerElementNormal_token = 3474930834345840119;
        private const long _LayerElementTangent_token = -2957608572615920843;
        private const long _LayerElementUV_token = -6372736986825316463;
        private const long _LayerElementColor_token = 3682432105686711379;
        private const long _Color_token = 7096547112121362046;
        private const long _BBoxMin_token = -5513532526077980700;
        private const long _BBoxMax_token = -5513532526077980938;
        private const long _Primary_Visibility_token = -3455892957121773355;
        private const long _Cast_Shadows_token = 839484601253318615;
        private const long _Receive_Shadows_token = -2774298015373455279;
        private const long _PolygonVertexIndex_token = 2322765863797130425;

        private const int MaximumVerticesPerHull = 8192;

        private readonly int[] _vertexIndicesBuffer = new int[MaximumVerticesPerHull];

        private FBXBlendShapeGeometryGroup ProcessBlendShapeGeometryGroup(FBXNode node, long objectId, string name, string objectClass)
        {
            var geometryGroup = new FBXBlendShapeGeometryGroup(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.BlendShapeGeometryGroups.Add(geometryGroup);
            }

            geometryGroup.Node = node;
            var vertices = node.GetNodeByName(_Vertices_token);
            if (vertices != null)
            {
                var verticesDataValue = vertices.Properties.GetFloatValues();
                var verticesCount = vertices.Properties.GetPropertyArrayLength();
                VerticesByVertexBlendShape(verticesDataValue, verticesCount, geometryGroup);
            }

            if (Reader.AssetLoaderContext.Options.ImportNormals)
            {
                var normals = node.GetNodeByName(_Normals_token);
                if (normals != null)
                {
                    var normalsDataValue = normals.Properties.GetFloatValues();
                    var normalsCount = normals.Properties.GetPropertyArrayLength();
                    NormalsByVertexBlendShape(normalsDataValue, normalsCount, geometryGroup);
                }
            }

            var indexes = node.GetNodeByName(_Indexes_token);
            if (indexes != null)
            {
                var intValues = indexes.Properties.GetIntValues();
                geometryGroup.IndexMap = new Dictionary<int, int>(intValues.Count);
                var blendShapeVertexIndex = 0;
                foreach (var originalVertexIndex in intValues)
                {
                    geometryGroup.IndexMap.Add(originalVertexIndex, blendShapeVertexIndex++);
                }
            }

            return geometryGroup;
        }

        private FBXGeometryProcessor ProcessGeometryLayer(FBXNode layerElement, FBXLayerType layerType)
        {
            long dataNodeName;
            long indexNodeName;
            long weightNodeName;
            switch (layerType)
            {
                case FBXLayerType.Vertices:
                    return ProcessFloatLayer(layerType, layerElement, default, _ByVertex_token, default, false);
                case FBXLayerType.Material:
                    dataNodeName = _Materials_token;
                    indexNodeName = 0;
                    weightNodeName = 0;
                    break;
                case FBXLayerType.Normals:
                    dataNodeName = _Normals_token;
                    indexNodeName = _NormalsIndex_token;
                    weightNodeName = _NormalsW_token;
                    break;
                case FBXLayerType.Tangents:
                    dataNodeName = _Tangents_token;
                    indexNodeName = _TangentsIndex_token;
                    weightNodeName = _TangentsW_token;
                    break;
                case FBXLayerType.Colors:
                    dataNodeName = _Colors_token;
                    indexNodeName = _ColorIndex_token;
                    weightNodeName = 0;
                    break;
                default:
                    dataNodeName = _UV_token;
                    indexNodeName = _UVIndex_token;
                    weightNodeName = 0;
                    break;
            }
            var dataLayer = layerElement.GetNodeByName(dataNodeName);
            if (dataLayer == null)
            {
                return null;
            }
            var weightLayer = weightNodeName == 0 ? default : layerElement.GetNodeByName(weightNodeName);
            var mappingInformationType = layerElement.GetNodeByName(_MappingInformationType_token);
            var referenceInformationType = layerElement.GetNodeByName(_ReferenceInformationType_token);
            if (mappingInformationType != null && referenceInformationType != null)
            {
                var indicesSet = false;
                PropertyAccessorInt indices = default;
                var mappingInformationTypeValue = mappingInformationType.Properties.GetStringHashValue(0);
                var referenceInformationTypeValue = referenceInformationType.Properties.GetStringHashValue(0);
                var referenceTypeValid = false;
                var byIndexMapping = false;
                switch (referenceInformationTypeValue)
                {
                    case _Direct_token:
                        referenceTypeValid = true;
                        break;
                    case _IndexToDirect_token:
                    case _Index_token:
                        byIndexMapping = true;
                        if (indexNodeName != 0)
                        {
                            var indicesNode = layerElement.GetNodeByName(indexNodeName);
                            if (indicesNode != null)
                            {
                                indicesSet = true;
                                indices = indicesNode.Properties.GetIntValues();
                            }
                        }
                        referenceTypeValid = true;
                        break;
                    default:
                        break;
                }
                if (referenceTypeValid)
                {
                    return ProcessFloatLayer(layerType, dataLayer, weightLayer, mappingInformationTypeValue, indices, byIndexMapping && indicesSet);
                }
            }
            return null;
        }

        private FBXGeometryProcessor ProcessFloatLayer(FBXLayerType layerType, FBXNode dataLayer, FBXNode weightLayer, long mappingInformationTypeValue, PropertyAccessorInt indices, bool byIndexMapping)
        {
            var data = dataLayer.Properties.GetFloatValues();
            PropertyAccessorFloat weights;
            if (weightLayer != null)
            {
                weights = weightLayer.Properties.GetFloatValues();
            }
            else
            {
                weights = null;
            }
            int elementsCount;
            switch (layerType)
            {
                case FBXLayerType.Material:
                    elementsCount = 1;
                    break;
                case FBXLayerType.Vertices:
                case FBXLayerType.Normals:
                case FBXLayerType.Tangents:
                    elementsCount = 3;
                    break;
                case FBXLayerType.UV:
                case FBXLayerType.UV2:
                case FBXLayerType.UV3:
                case FBXLayerType.UV4:
                    elementsCount = 2;
                    break;
                case FBXLayerType.Colors:
                    elementsCount = 4;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(layerType), layerType, null);
            }
            switch (mappingInformationTypeValue)
            {
                case _ByPolygonVertex_token:
                    {
                        return data.Count == 0 ? null : new FBXGeometryProcessor(data, indices, weights, elementsCount, byIndexMapping, FBXMeshMappingType.ByPolygonVertex);
                    }
                case _ByVertice_token:
                case _ByVertex_token:
                    {
                        return data.Count == 0 ? null : new FBXGeometryProcessor(data, indices, weights, elementsCount, byIndexMapping, FBXMeshMappingType.ByVertex);
                    }
                case _ByPolygon_token:
                    {
                        return data.Count == 0 ? null : new FBXGeometryProcessor(data, indices, weights, elementsCount, byIndexMapping, FBXMeshMappingType.ByPolygon);
                    }
                case _AllSame_token:
                    {
                        return data.Count == 0 ? null : new FBXGeometryProcessor(data, indices, weights, elementsCount, byIndexMapping, FBXMeshMappingType.AllSame);
                    }
            }
            return null;
        }

        private FBXGeometry GetGeometryByPolygon(IFBXMesh geometryGroup, int materialIndex, bool isQuad)
        {
            return geometryGroup.InnerGeometryGroup.GetGeometry<FBXGeometry>(Reader.AssetLoaderContext, materialIndex, isQuad, geometryGroup.HasBlendShapes);
        }

        private void PostProcessGeometries()
        {
            SetupGeometries();
            if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None || Reader.AssetLoaderContext.Options.ImportBlendShapes)
            {
                PostProcessGeometryDeformers();
                PostProcessDeformers();
            }
            PostProcessPolygons();
        }

        private IFBXMesh ProcessGeometryGroup(FBXNode node, long objectId, string name, string objectClass)
        {
            if (objectId == -1)
            {
                return new FBXMesh(Document, null, 0, null, false);
            }

            FBXGeometryProcessor vertexProcessor = null;
            FBXGeometryProcessor materialProcessor = null;
            FBXGeometryProcessor normalProcessor = null;
            FBXGeometryProcessor tangentProcessor = null;
            FBXGeometryProcessor uvProcessor0 = null;
            FBXGeometryProcessor uvProcessor1 = null;
            FBXGeometryProcessor uvProcessor2 = null;
            FBXGeometryProcessor uvProcessor3 = null;
            FBXGeometryProcessor colorProcessor = null;

            var layerElementVertices = node.GetNodeByName(_Vertices_token);
            if (layerElementVertices != null)
            {
                vertexProcessor = ProcessGeometryLayer(layerElementVertices, FBXLayerType.Vertices);
            }

            var layerElementMaterial = node.GetNodeByName(_LayerElementMaterial_token);
            if (layerElementMaterial != null && Reader.AssetLoaderContext.Options.ImportMaterials)
            {
                materialProcessor = ProcessGeometryLayer(layerElementMaterial, FBXLayerType.Material);
            }

            var layerElementNormal = node.GetNodeByName(_LayerElementNormal_token);
            if (layerElementNormal != null && Reader.AssetLoaderContext.Options.ImportNormals)
            {
                normalProcessor = ProcessGeometryLayer(layerElementNormal, FBXLayerType.Normals);
            }

            var layerElementTangent = node.GetNodeByName(_LayerElementTangent_token);
            if (layerElementTangent != null && Reader.AssetLoaderContext.Options.ImportTangents)
            {
                tangentProcessor = ProcessGeometryLayer(layerElementTangent, FBXLayerType.Tangents);
            }

            var layerElementUvs = node.GetNodesByName(_LayerElementUV_token);
            if (layerElementUvs != null)
            {
                foreach (var layerElementUv in layerElementUvs)
                {
                    var layerIndex = layerElementUv.Properties.GetIntValue(0);
                    switch (layerIndex)
                    {
                        case 0:
                            uvProcessor0 = ProcessGeometryLayer(layerElementUv, FBXLayerType.UV);
                            break;
                        case 1:
                            uvProcessor1 = ProcessGeometryLayer(layerElementUv, FBXLayerType.UV2);
                            break;
                        case 2:
                            uvProcessor2 = ProcessGeometryLayer(layerElementUv, FBXLayerType.UV3);
                            break;
                        case 3:
                            uvProcessor3 = ProcessGeometryLayer(layerElementUv, FBXLayerType.UV4);
                            break;
                    }
                }
            }

            var layerElementColor = node.GetNodeByName(_LayerElementColor_token);
            if (layerElementColor != null && Reader.AssetLoaderContext.Options.ImportColors)
            {
                colorProcessor = ProcessGeometryLayer(layerElementColor, FBXLayerType.Colors);
            }

            var mesh = new FBXMesh(Document, name, objectId, objectClass, false);
            mesh.Node = node;

            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _Color_token:
                                mesh.Color = property.Properties.GetColorValue(4);
                                break;
                            case _BBoxMin_token:
                                mesh.BBoxMin = property.Properties.GetVector3Value(4);
                                break;
                            case _BBoxMax_token:
                                mesh.BBoxMax = property.Properties.GetVector3Value(4);
                                break;
                            case _Primary_Visibility_token:
                                mesh.PrimaryVisibility = property.Properties.GetBoolValue(4);
                                break;
                            case _Cast_Shadows_token:
                                mesh.CastShadows = property.Properties.GetBoolValue(4);
                                break;
                            case _Receive_Shadows_token:
                                mesh.ReceiveShadows = property.Properties.GetBoolValue(4);
                                break;
                        }
                    }
                }
            }

            mesh.VertexProcessor = vertexProcessor;
            mesh.NormalProcessor = normalProcessor;
            mesh.TangentProcessor = tangentProcessor;
            mesh.ColorProcessor = colorProcessor;
            mesh.UVProcessor0 = uvProcessor0;
            mesh.UVProcessor1 = uvProcessor1;
            mesh.UVProcessor2 = uvProcessor2;
            mesh.UVProcessor3 = uvProcessor3;
            mesh.MaterialProcessor = materialProcessor;

            Document.AllMeshes.Add(mesh);

            return mesh;
        }

        private void SetupGeometries()
        {
            for (var meshIndex = 0; meshIndex < Document.AllMeshes.Count; meshIndex++)
            {
                var mesh = Document.AllMeshes[meshIndex];
                var polygonVertexIndexNode = mesh.Node.GetNodeByName(_PolygonVertexIndex_token);
                if (polygonVertexIndexNode != null)
                {
                    var polygonVertexIndices = polygonVertexIndexNode.Properties.GetIntValues();
                    var polygonVertexIndexCount = polygonVertexIndexNode.Properties.GetPropertyArrayLength();
                    var totalVertices = GetTotalVertices(polygonVertexIndexCount, polygonVertexIndices);
                    var hasSkin = false;
                    var hasBlendShapes = false;
                    for (var subDeformerIndex = 0; subDeformerIndex < Document.SubDeformers.Count; subDeformerIndex++)
                    {
                        var subDeformer = Document.SubDeformers[subDeformerIndex];
                        var deformer = subDeformer.BaseDeformer;
                        if (deformer?.Geometry != null)
                        {
                            if (subDeformer is FBXBlendShapeSubDeformer blendShapeSubDeformer && Reader.AssetLoaderContext.Options.ImportBlendShapes)
                            {
                                hasBlendShapes = true;
                                hasSkin = true;
                                break;
                            }
                        }
                    }
                    if (!hasSkin)
                    {
                        for (var subDeformerIndex = 0; subDeformerIndex < Document.SubDeformers.Count; subDeformerIndex++)
                        {
                            var subDeformer = Document.SubDeformers[subDeformerIndex];
                            var deformer = subDeformer.BaseDeformer;
                            if (deformer.Geometry == mesh)
                            {
                                hasSkin = true;
                                break;
                            }
                        }
                    }
                    var geometryGroup = FBXGeometryGroupFactory.Get(
                            Reader.AssetLoaderContext,
                            mesh.NormalProcessor != null,
                            mesh.TangentProcessor != null,
                            mesh.ColorProcessor != null,
                            mesh.UVProcessor0 != null,
                            mesh.UVProcessor1 != null,
                            mesh.UVProcessor2 != null,
                            mesh.UVProcessor3 != null,
                            hasSkin);
                    mesh.HasBlendShapes = hasBlendShapes;
                    mesh.InnerGeometryGroup = geometryGroup;
                    mesh.InnerGeometryGroup.Name = mesh.Name;
                    mesh.InnerGeometryGroup.Setup(Reader.AssetLoaderContext, totalVertices, 1);
                    Document.AllGeometryGroups.Add(mesh.InnerGeometryGroup);
                }
            }
        }

        private void PostProcessPolygons()
        {
            for (var meshIndex = 0; meshIndex < Document.AllMeshes.Count; meshIndex++)
            {
                var mesh = Document.AllMeshes[meshIndex];
                var polygonVertexIndexNode = mesh.Node.GetNodeByName(_PolygonVertexIndex_token);
                if (polygonVertexIndexNode != null)
                {
                    Matrix4x4 geometricMatrix;
                    if (mesh.Model != null)
                    {
                        var geometricTranslation = mesh.Model.Matrices.GetMatrix(FBXMatrixType.GeometricTranslation);
                        var geometricRotation = mesh.Model.Matrices.GetMatrix(FBXMatrixType.GeometricRotation);
                        var geometricScale = mesh.Model.Matrices.GetMatrix(FBXMatrixType.GeometricScaling);
                        var translation = Matrix4x4.Translate(geometricTranslation);
                        var rotation = FBXMatrices.ProcessRotationMatrix4x4(Reader.AssetLoaderContext, geometricRotation, FBXRotationOrder.OrderXYZ);
                        var scale = Matrix4x4.Scale(geometricScale);
                        geometricMatrix = translation * rotation * scale;
                    }
                    else
                    {
                        geometricMatrix = Matrix4x4.identity;
                    }
                    var polygonVertexIndices = polygonVertexIndexNode.Properties.GetIntValues();
                    var polygonVertexIndexCount = polygonVertexIndexNode.Properties.GetPropertyArrayLength();
                    var hasNegativeScale = geometricMatrix.IsNegative();
                    AddVertices(polygonVertexIndexCount, polygonVertexIndices, hasNegativeScale, mesh, geometricMatrix, mesh.InnerGeometryGroup.VerticesCapacity);
                }
            }
        }

        private int GetTotalVertices(int polygonVertexIndexCount, PropertyAccessorInt polygonVertexIndices)
        {
            var indexCounter = 0;
            var totalVertices = 0;
            for (var polygonVertexIndex = 0; polygonVertexIndex < polygonVertexIndexCount; polygonVertexIndex++)
            {
                var vertexIndex = polygonVertexIndices[polygonVertexIndex];
                if (vertexIndex < 0)
                {
                    vertexIndex ^= -1;
                    _vertexIndicesBuffer[indexCounter++] = vertexIndex;
                    switch (indexCounter)
                    {
                        case 3:
                            {
                                totalVertices += 3;
                                break;
                            }
                        case 4:
                            {
                                totalVertices += 4;
                                break;
                            }
                        default:
                            {
                                totalVertices += indexCounter;
                                break;
                            }
                    }
                    indexCounter = 0;
                }
                else
                {
                    indexCounter++;
                }
            }
            return totalVertices;
        }

        private void AddVertices(int polygonVertexIndexCount, PropertyAccessorInt polygonVertexIndices, bool hasNegativeScale, IFBXMesh mesh, Matrix4x4 geometricMatrix, int totalVertices)
        {
            var indexCounter = 0;
            var polygonIndex = 0;
            for (var polygonVertexIndex = 0; polygonVertexIndex < polygonVertexIndexCount; polygonVertexIndex++)
            {
                var vertexIndex = polygonVertexIndices[polygonVertexIndex];
                if (vertexIndex < 0)
                {
                    vertexIndex ^= -1;
                    _vertexIndicesBuffer[indexCounter++] = vertexIndex;
                    switch (indexCounter)
                    {
                        case 3:
                            {
                                var polygonVertexIndex0 = polygonVertexIndex - 2;
                                var polygonVertexIndex1 = polygonVertexIndex - 1;
                                var polygonVertexIndex2 = polygonVertexIndex - 0;
                                if (Document.IsRightHanded && !hasNegativeScale)
                                {
                                    AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                    AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, false, totalVertices);
                                    AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                }
                                else
                                {
                                    AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                    AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, false, totalVertices);
                                    AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                }

                                break;
                            }
                        case 4:
                            {
                                var polygonVertexIndex0 = polygonVertexIndex - 3;
                                var polygonVertexIndex1 = polygonVertexIndex - 2;
                                var polygonVertexIndex2 = polygonVertexIndex - 1;
                                var polygonVertexIndex3 = polygonVertexIndex - 0;
                                if (Reader.AssetLoaderContext.Options.KeepQuads)
                                {
                                    if ((Document.IsRightHanded && !hasNegativeScale))
                                    {
                                        AddVertex(mesh, _vertexIndicesBuffer[3], polygonIndex, polygonVertexIndex3, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, true, totalVertices);
                                    }
                                    else
                                    {
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, true, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[3], polygonIndex, polygonVertexIndex3, geometricMatrix, true, totalVertices);
                                    }
                                }
                                else
                                {
                                    if (Document.IsRightHanded && !hasNegativeScale)
                                    {
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[3], polygonIndex, polygonVertexIndex3, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                    }
                                    else
                                    {
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[1], polygonIndex, polygonVertexIndex1, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[2], polygonIndex, polygonVertexIndex2, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[3], polygonIndex, polygonVertexIndex3, geometricMatrix, false, totalVertices);
                                        AddVertex(mesh, _vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex0, geometricMatrix, false, totalVertices);
                                    }
                                }

                                break;
                            }
                        default:
                            {
                                var contourVertices = new List<ContourVertex>(indexCounter);
                                for (var i = 0; i < indexCounter; i++)
                                {
                                    var tempVertexIndex = _vertexIndicesBuffer[i];
                                    var tempPolygonVertexIndex = polygonVertexIndex - indexCounter + i + 1;
                                    var vertexValues = mesh.VertexProcessor.GetValues(tempVertexIndex, polygonIndex, tempPolygonVertexIndex);
                                    contourVertices.Add(new ContourVertex(new Vec3(vertexValues[0], vertexValues[1], vertexValues[2]), BuildVertexData(mesh, geometricMatrix, tempVertexIndex, tempPolygonVertexIndex, polygonIndex, totalVertices)));
                                }
                                var materialIndex = mesh.MaterialProcessor?.GetValues(_vertexIndicesBuffer[0], polygonIndex, polygonVertexIndex)[0] ?? default(int);
                                var geometry = GetGeometryByPolygon(mesh, (int)materialIndex, false);
                                Helpers.Tesselate(contourVertices, Reader.AssetLoaderContext, geometry, geometry.GeometryGroup, (Document.IsRightHanded && !hasNegativeScale));
                                break;
                            }
                    }

                    polygonIndex++;
                    indexCounter = 0;
                }
                else
                {
                    _vertexIndicesBuffer[indexCounter++] = vertexIndex;
                }
            }
        }


        private void PostProcessGeometryDeformers()
        {
            for (var subDeformerIndex = 0; subDeformerIndex < Document.SubDeformers.Count; subDeformerIndex++)
            {
                var subDeformer = Document.SubDeformers[subDeformerIndex];
                var deformer = subDeformer?.BaseDeformer;
                if (deformer.Geometry != null)
                {
                    if (subDeformer is FBXBlendShapeSubDeformer blendShapeSubDeformer && Reader.AssetLoaderContext.Options.ImportBlendShapes)
                    {
                        var blendShapeGeometryGroup = (FBXBlendShapeGeometryGroup)blendShapeSubDeformer.Geometry;
                        if (blendShapeGeometryGroup == null || blendShapeGeometryGroup.Processed)
                        {
                            continue;
                        }
                        if (deformer.Geometry.InnerGeometryGroup.BlendShapeKeys == null)
                        {
                            deformer.Geometry.InnerGeometryGroup.BlendShapeKeys = new List<IBlendShapeKey>(deformer.Geometry.BlendShapeGeometryBindingsCount);
                        }
                        deformer.Geometry.InnerGeometryGroup.BlendShapeKeys.Add(blendShapeGeometryGroup);
                        var weight = blendShapeSubDeformer.FullWeights?.Count > 0 ? blendShapeSubDeformer.FullWeights[0] : 0f;
                        var vertices = blendShapeGeometryGroup.Vertices;
                        var normals = blendShapeGeometryGroup.Normals;
                        for (var j = 0; j < vertices.Count; j++)
                        {
                            var vertex = vertices[j];
                            vertices[j] = Document.ConvertVector(vertex, true);
                            if (normals != null)
                            {
                                var normal = normals[j]; 
                                normals[j] = Document.ConvertVector(normal);
                            }
                        }
                        blendShapeGeometryGroup.FrameWeight = weight;
                        blendShapeGeometryGroup.Processed = true;
                    }
                }
            }
        }

        public static Vector4 ToVector4(IList<float> values, float weight)
        {
            return new Vector4(values[0], values[1], values[2], weight);
        }

        private static Vector3 ToVector3(IList<float> values)
        {
            return new Vector3(values[0], values[1], values[2]);
        }

        private static Vector2 ToVector2(IList<float> values)
        {
            return new Vector2(values[0], values[1]);
        }

        private static Color ToColor(IList<float> values)
        {
            return new Color(values[0], values[1], values[2], values[3]);
        }

        private IVertexData BuildVertexData(IFBXMesh mesh, Matrix4x4 geometricMatrix, int vertexIndex, int polygonVertexIndex, int polygonIndex, int totalVertices)
        {
            var materialIndex = mesh.MaterialProcessor?.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)[0] ?? default(int);
            var geometry = GetGeometryByPolygon(mesh, (int)materialIndex, false);
            var position = mesh.VertexProcessor != null ? ToVector3(mesh.VertexProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            position = geometricMatrix.MultiplyPoint(position);
            position = Document.BakeMatrix.MultiplyVector(position);
            position = Document.ConvertVector(position, true);
            var normal = mesh.NormalProcessor != null ? ToVector3(mesh.NormalProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            normal = geometricMatrix.MultiplyVector(normal);
            normal = Document.BakeMatrix.MultiplyVector(normal);
            normal = Document.ConvertVector(normal);
            var tangent = mesh.TangentProcessor != null ? (Vector4)ToVector3(mesh.TangentProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            tangent = geometricMatrix.MultiplyVector(tangent);
            tangent = Document.BakeMatrix.MultiplyVector(tangent);
            tangent = Document.ConvertVector(tangent);
            tangent.w = MathUtils.CalculateTangentSign(normal, tangent);
            var color = mesh.ColorProcessor != null ? ToColor(mesh.ColorProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Color);
            var uv0 = mesh.UVProcessor0 != null ? ToVector2(mesh.UVProcessor0.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv1 = mesh.UVProcessor1 != null ? ToVector2(mesh.UVProcessor1.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv2 = mesh.UVProcessor2 != null ? ToVector2(mesh.UVProcessor2.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv3 = mesh.UVProcessor3 != null ? ToVector2(mesh.UVProcessor3.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var interpolatedVertex = new InterpolatedVertex(position);
            interpolatedVertex.SetVertexIndex(vertexIndex, geometry.GeometryGroup);
            interpolatedVertex.SetNormal(normal, geometry.GeometryGroup);
            interpolatedVertex.SetTangent(tangent, geometry.GeometryGroup);
            interpolatedVertex.SetColor(color, geometry.GeometryGroup);
            interpolatedVertex.SetUV1(uv0, geometry.GeometryGroup);
            interpolatedVertex.SetUV2(uv1, geometry.GeometryGroup);
            interpolatedVertex.SetUV3(uv2, geometry.GeometryGroup);
            interpolatedVertex.SetUV4(uv3, geometry.GeometryGroup);
            return interpolatedVertex;
        }

        private void AddVertex(IFBXMesh mesh, int vertexIndex, int polygonIndex, int polygonVertexIndex, Matrix4x4 geometricMatrix, bool isQuad, int totalVertices)
        {
            var materialIndex = mesh.MaterialProcessor?.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)[0] ?? default(int);
            var geometry = GetGeometryByPolygon(mesh, (int)materialIndex, isQuad);
            var position = mesh.VertexProcessor != null ? ToVector3(mesh.VertexProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            position = geometricMatrix.MultiplyPoint(position);
            position = Document.BakeMatrix.MultiplyVector(position);
            position = Document.ConvertVector(position, true);
            var normal = mesh.NormalProcessor != null ? ToVector3(mesh.NormalProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            normal = geometricMatrix.MultiplyVector(normal);
            normal = Document.BakeMatrix.MultiplyVector(normal);
            normal = Document.ConvertVector(normal);
            var tangent = mesh.TangentProcessor != null ? (Vector4)ToVector3(mesh.TangentProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default;
            tangent = geometricMatrix.MultiplyVector(tangent);
            tangent = Document.BakeMatrix.MultiplyVector(tangent);
            tangent = Document.ConvertVector(tangent);
            tangent.w = MathUtils.CalculateTangentSign(normal, tangent);
            var color = mesh.ColorProcessor != null ? ToColor(mesh.ColorProcessor.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Color);
            var uv0 = mesh.UVProcessor0 != null ? ToVector2(mesh.UVProcessor0.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv1 = mesh.UVProcessor1 != null ? ToVector2(mesh.UVProcessor1.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv2 = mesh.UVProcessor2 != null ? ToVector2(mesh.UVProcessor2.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            var uv3 = mesh.UVProcessor3 != null ? ToVector2(mesh.UVProcessor3.GetValues(vertexIndex, polygonIndex, polygonVertexIndex)) : default(Vector2);
            geometry.AddVertex(Reader.AssetLoaderContext,
                vertexIndex,
                position,
                normal,
                tangent,
                color,
                uv0,
                uv1,
                uv2,
                uv3
            );
        }
    }
}
