using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _Connections_token = -8303341710877354960;
        private const long _OO_token = 1081989810475738245;
        private const long _OP_token = 1081989810475738246;

        private void ProcessConnections(FBXNode node)
        {
            var connections = node.GetNodeByName(_Connections_token);
            if (connections != null)
            {
                foreach (var connection in connections)
                {
                    var sourceObjectId = connection.Properties.GetLongValue(1);
                    var sourceObject = Document.GetObjectById(sourceObjectId);
                    if (sourceObject == null)
                    {
                        continue;
                    }
                    var destinationObjectId = connection.Properties.GetLongValue(2);
                    if (destinationObjectId == 0) //TODO: Documents/Document/RootNode.Data[0]
                    {
                        if (sourceObject is FBXModel)
                        {
                            Document.ChildrenCount++;
                        }
                        continue;
                    }
                    var destinationObject = Document.GetObjectById(destinationObjectId);
                    if (destinationObject == null)
                    {
                        continue;
                    }
                    var connectionType = connection.Properties.GetStringHashValue(0);
                    switch (connectionType)
                    {
                        case _OO_token:
                            {
                                switch (sourceObject.ObjectType)
                                {
                                    case FBXObjectType.NodeAttribute when destinationObject.ObjectType == FBXObjectType.Camera && Reader.AssetLoaderContext.Options.ImportCameras:
                                        ((FBXNodeAttribute)sourceObject).ApplyNodeAttributes((FBXCamera)destinationObject, this);
                                        break;
                                    case FBXObjectType.NodeAttribute when destinationObject.ObjectType == FBXObjectType.Light && Reader.AssetLoaderContext.Options.ImportLights:
                                        ((FBXNodeAttribute)sourceObject).ApplyNodeAttributes((FBXLight)destinationObject, this);
                                        break;
                                    case FBXObjectType.Model when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera:
                                    case FBXObjectType.Camera when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera:
                                        {
                                            var destinationModel = (FBXModel)destinationObject;
                                            destinationModel.ChildrenCount++;
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.AnimationLayer:
                                        {
                                            var destinationAnimationLayer = (FBXAnimationLayer)destinationObject;
                                            destinationAnimationLayer.CurveNodesCount++;
                                            break;
                                        }
                                    case FBXObjectType.AnimationLayer when destinationObject.ObjectType == FBXObjectType.AnimationStack:
                                        {
                                            var destinationAnimationStack = (FBXAnimationStack)destinationObject;
                                            destinationAnimationStack.LayersCount++;
                                            break;
                                        }
                                    case FBXObjectType.Material when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            var destinationModel = (FBXModel)destinationObject;
                                            destinationModel.AllMaterialIndicesCount++;
                                            break;
                                        }
                                    case FBXObjectType.Geometry when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            Document.ConnectedGeometriesCount++;
                                            Document.GeometriesCount = Mathf.Max(Document.GeometriesCount, Document.ConnectedGeometriesCount);
                                            break;
                                        }
                                }
                                break;
                            }
                        case _OP_token:
                            {
                                switch (sourceObject.ObjectType)
                                {
                                    case FBXObjectType.AnimationCurve when destinationObject.ObjectType == FBXObjectType.AnimationCurveNode:
                                        {
                                            var destinationAnimationCurveNode = (FBXAnimationCurveNode)destinationObject;
                                            destinationAnimationCurveNode.AnimationCurvesCount++;
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
                foreach (var connection in connections)
                {
                    var sourceObjectId = connection.Properties.GetLongValue(1);
                    var sourceObject = Document.GetObjectById(sourceObjectId);
                    if (sourceObject == null)
                    {
                        continue;
                    }
                    var destinationObjectId = connection.Properties.GetLongValue(2);
                    if (destinationObjectId == 0) //TODO: Documents/Document/RootNode.Data[0]
                    {
                        if (sourceObject is FBXModel sourceModel)
                        {
                            sourceModel.Parent = Document;
                        }
                        continue;
                    }
                    var destinationObject = Document.GetObjectById(destinationObjectId);
                    if (destinationObject == null)
                    {
                        continue;
                    }
                    var connectionType = connection.Properties.GetStringHashValue(0);
                    switch (connectionType)
                    {
                        case _OO_token:
                            {
                                switch (sourceObject.ObjectType)
                                {
                                    case FBXObjectType.Model when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                    case FBXObjectType.Light when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                    case FBXObjectType.Camera when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                        {
                                            var sourceModel = (FBXModel)sourceObject;
                                            var destinationModel = (FBXModel)destinationObject;
                                            sourceModel.Parent = destinationModel;
                                            break;
                                        }
                                    case FBXObjectType.BlendShapeGeometry when destinationObject.ObjectType == FBXObjectType.BlendShapeSubDeformer:
                                        {
                                            var sourceGeometryGroup = (FBXBlendShapeGeometryGroup)sourceObject;
                                            var destinationSubDeformer = (FBXBlendShapeSubDeformer)destinationObject;
                                            destinationSubDeformer.Geometry = sourceGeometryGroup;
                                            break;
                                        }
                                    case FBXObjectType.Geometry when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            var sourceMesh = (IFBXMesh)sourceObject;
                                            var destinationModel = (FBXModel)destinationObject;
                                            if (sourceMesh.Model != null)
                                            {
                                                if (destinationModel.IsGeometryCompatible(sourceMesh.Model))
                                                {
                                                    destinationModel.Mesh = sourceMesh;
                                                }
                                                else
                                                {
                                                    var clonedMesh = FBXMesh.CopyFrom(Reader.AssetLoaderContext, sourceMesh);
                                                    clonedMesh.Model = destinationModel;
                                                    destinationModel.Mesh = clonedMesh;
                                                    Document.AllMeshes.Add(clonedMesh);
                                                }
                                            }
                                            else
                                            {
                                                sourceMesh.Name = destinationModel.Name;
                                                destinationModel.Mesh = sourceMesh;
                                                sourceMesh.Model = destinationModel;
                                            }
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.AnimationLayer:
                                        {
                                            var sourceAnimationCurveNode = (FBXAnimationCurveNode)sourceObject;
                                            var destinationAnimationLayer = (FBXAnimationLayer)destinationObject;
                                            sourceAnimationCurveNode.AnimationLayer = destinationAnimationLayer;
                                            if (destinationAnimationLayer.CurveNodes == null)
                                            {
                                                destinationAnimationLayer.CurveNodes = new List<FBXAnimationCurveNode>(destinationAnimationLayer.CurveNodesCount);
                                            }
                                            destinationAnimationLayer.CurveNodes.Add(sourceAnimationCurveNode);
                                            break;
                                        }
                                    case FBXObjectType.AnimationLayer when destinationObject.ObjectType == FBXObjectType.AnimationStack:
                                        {
                                            var sourceAnimationLayer = (FBXAnimationLayer)sourceObject;
                                            var destinationAnimationStack = (FBXAnimationStack)destinationObject;
                                            sourceAnimationLayer.AnimationStacks.Add(destinationAnimationStack);
                                            if (destinationAnimationStack.AnimatedLayers == null)
                                            {
                                                destinationAnimationStack.AnimatedLayers = new HashSet<FBXAnimationLayer>();
                                            }
                                            destinationAnimationStack.AnimatedLayers.Add(sourceAnimationLayer);
                                            break;
                                        }
                                    case FBXObjectType.Deformer when destinationObject.ObjectType == FBXObjectType.Geometry:
                                        {
                                            var sourceDeformer = (FBXDeformer)sourceObject;
                                            var destinationGeometry = (IFBXMesh)destinationObject;
                                            sourceDeformer.Geometry = destinationGeometry;
                                            break;
                                        }
                                    case FBXObjectType.SubDeformer when destinationObject.ObjectType == FBXObjectType.Deformer:
                                        {
                                            var sourceSubDeformer = (FBXSubDeformer)sourceObject;
                                            var destinationDeformer = (FBXDeformer)destinationObject;
                                            sourceSubDeformer.BaseDeformer = destinationDeformer;
                                            break;
                                        }
                                    case FBXObjectType.BlendShapeSubDeformer when destinationObject.ObjectType == FBXObjectType.Deformer:
                                        {
                                            var sourceSubDeformer = (FBXSubDeformer)sourceObject;
                                            var destinationDeformer = (FBXDeformer)destinationObject;
                                            sourceSubDeformer.BaseDeformer = destinationDeformer;
                                            break;
                                        }
                                    case FBXObjectType.Model when destinationObject.ObjectType == FBXObjectType.BlendShapeSubDeformer:
                                        {
                                            var sourceModel = (FBXModel)sourceObject;
                                            var destinationSubDeformer = (FBXSubDeformer)destinationObject;
                                            destinationSubDeformer.Model = sourceModel;
                                            break;
                                        }
                                    case FBXObjectType.Model when destinationObject.ObjectType == FBXObjectType.SubDeformer:
                                        {
                                            var sourceModel = (FBXModel)sourceObject;
                                            var destinationSubDeformer = (FBXSubDeformer)destinationObject;
                                            destinationSubDeformer.Model = sourceModel;
                                            break;
                                        }
                                    case FBXObjectType.Video when destinationObject.ObjectType == FBXObjectType.Texture:
                                        {
                                            var sourceVideo = (FBXVideo)sourceObject;
                                            var destinationTexture = (FBXTexture)destinationObject;
                                            destinationTexture.Video = sourceVideo;
                                            break;
                                        }
                                    case FBXObjectType.Texture when destinationObject.ObjectType == FBXObjectType.LayeredTexture:
                                        {
                                            var sourceTexture = (FBXTexture)sourceObject;
                                            var destinationTexture = (FBXLayeredTexture)destinationObject;
                                            destinationTexture.AddTexture(sourceTexture);
                                            break;
                                        }
                                    case FBXObjectType.Material when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            var sourceMaterial = (FBXMaterial)sourceObject;
                                            var destinationModel = (FBXModel)destinationObject;
                                            if (destinationModel.AllMaterialIndices == null)
                                            {
                                                destinationModel.AllMaterialIndices = new List<int>(destinationModel.AllMaterialIndicesCount);
                                            }
                                            destinationModel.AllMaterialIndices.Add(sourceMaterial.Index);
                                            if (destinationModel.DiffuseTexture != null)
                                            {
                                                sourceMaterial.AddProperty("DiffuseColorTex", destinationModel.DiffuseTexture, true);
                                            }
                                            break;
                                        }
                                    case FBXObjectType.Texture when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            var sourceTexture = (FBXTexture)sourceObject;
                                            var destinationModel = (FBXModel)destinationObject;
                                            if (destinationModel.MaterialIndices != null)
                                            {
                                                //todo: check if this still working. It is rare to find an FBX model attaching the texture to the model like that
                                                for (var i = 0; i < destinationModel.MaterialIndices.Length; i++)
                                                {
                                                    var materialIndex = destinationModel.MaterialIndices[i];
                                                    var material = Document.AllMaterials[materialIndex];
                                                    if (!material.HasProperty("DiffuseColorTex"))
                                                    {
                                                        material.AddProperty("DiffuseColorTex", sourceTexture, true);
                                                    }
                                                    else if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                                                    {
                                                        Debug.LogWarning($"Model [{destinationModel.Name}] already has a Diffuse Texture. A second Texture cannot be set.");
                                                    }
                                                }
                                            }
                                            else
                                            {
                                                if (destinationModel.DiffuseTexture == null)
                                                {
                                                    destinationModel.DiffuseTexture = sourceTexture;
                                                }
                                                else if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                                                {
                                                    Debug.LogWarning($"Model [{destinationModel.Name}] already has a Diffuse Texture. A second Texture cannot be set.");
                                                }
                                            }
                                            break;
                                        }
                                    case FBXObjectType.Material when destinationObject.ObjectType == FBXObjectType.Implementation:
                                        {
                                            var sourceMaterial = (FBXMaterial)sourceObject;
                                            var destinationImplementation = (FBXImplementation)destinationObject;
                                            sourceMaterial.Implementation = destinationImplementation;
                                            break;
                                        }
                                    case FBXObjectType.BindingTable when destinationObject.ObjectType == FBXObjectType.Implementation:
                                        {
                                            var sourceBindingTable = (FBXBindingTable)sourceObject;
                                            var destinationImplementation = (FBXImplementation)destinationObject;
                                            destinationImplementation.BindingTable = sourceBindingTable;
                                            break;
                                        }
                                }
                                break;
                            }
                        case _OP_token:
                            {
                                var destinationProperty = connection.Properties.GetStringValue(3, false);
                                switch (sourceObject.ObjectType)
                                {
                                    case FBXObjectType.Camera when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                    case FBXObjectType.Light when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                    case FBXObjectType.Model when destinationObject.ObjectType == FBXObjectType.Model || destinationObject.ObjectType == FBXObjectType.Camera || destinationObject.ObjectType == FBXObjectType.Light:
                                        switch (ParseAnimationProperty(destinationProperty))
                                        {
                                            case "LookAtProperty" when Reader.AssetLoaderContext.Options.ImportCameras && destinationObject is FBXCamera camera:
                                                camera.LookAtProperty = (FBXModel)sourceObject;
                                                break;
                                        }
                                        break;
                                    case FBXObjectType.AnimationCurve when destinationObject.ObjectType == FBXObjectType.AnimationCurveNode:
                                        {
                                            var sourceAnimationCurve = (FBXAnimationCurve)sourceObject;
                                            var destinationAnimationCurveNode = (FBXAnimationCurveNode)destinationObject;
                                            var animationCurveNodeBinding = new FBXAnimationCurveNodeBinding
                                            {
                                                AnimationCurveNode = destinationAnimationCurveNode
                                            };
                                            int fieldIndex;
                                            switch (ParseAnimationProperty(destinationProperty))
                                            {
                                                case "X":
                                                case "Visibility":
                                                    fieldIndex = 0;
                                                    break;
                                                case "Y":
                                                    fieldIndex = 1;
                                                    break;
                                                case "Z":
                                                    fieldIndex = 2;
                                                    break;
                                                default:
                                                    fieldIndex = -1;
                                                    break;
                                            }
                                            animationCurveNodeBinding.FieldIndex = fieldIndex;
                                            sourceAnimationCurve.AnimationCurveNodeBinding = animationCurveNodeBinding;
                                            if (destinationAnimationCurveNode.AnimationCurves == null)
                                            {
                                                destinationAnimationCurveNode.AnimationCurves = new List<FBXAnimationCurve>(destinationAnimationCurveNode.AnimationCurvesCount);
                                            }
                                            destinationAnimationCurveNode.AnimationCurves.Add(sourceAnimationCurve);
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.Model:
                                        {
                                            var sourceAnimationCurveNode = (FBXAnimationCurveNode)sourceObject;
                                            var destinationModel = (FBXModel)destinationObject;
                                            var animationCurveModelBinding = new FBXAnimationCurveModelBinding
                                            {
                                                Model = destinationModel
                                            };
                                            FBXMatrixType matrixType;
                                            switch (destinationProperty)
                                            {
                                                case "Lcl Translation":
                                                    matrixType = FBXMatrixType.LclTranslation;
                                                    break;
                                                case "RotationOffset":
                                                    matrixType = FBXMatrixType.RotationOffset;
                                                    break;
                                                case "RotationPivot":
                                                    matrixType = FBXMatrixType.RotationPivot;
                                                    break;
                                                case "PreRotation":
                                                    matrixType = FBXMatrixType.PreRotation;
                                                    break;
                                                case "Lcl Rotation":
                                                    matrixType = FBXMatrixType.LclRotation;
                                                    break;
                                                case "PostRotation":
                                                    matrixType = FBXMatrixType.PostRotation;
                                                    break;
                                                case "ScalingOffset":
                                                    matrixType = FBXMatrixType.ScalingOffset;
                                                    break;
                                                case "ScalingPivot":
                                                    matrixType = FBXMatrixType.ScalingPivot;
                                                    break;
                                                case "Lcl Scaling":
                                                    matrixType = FBXMatrixType.LclScaling;
                                                    break;
                                                case "Visibility":
                                                    matrixType = FBXMatrixType.Visibility;
                                                    break;
                                                default:
                                                    matrixType = FBXMatrixType.Unknown;
                                                    break;
                                            }
                                            animationCurveModelBinding.PropertyMatrixType = matrixType;
                                            sourceAnimationCurveNode.AnimationCurveModelBinding = animationCurveModelBinding;
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.Geometry:
                                        {
                                            var sourceAnimationCurveNode = (FBXAnimationCurveNode)sourceObject;
                                            var destinationGeometry = (IFBXMesh)destinationObject;
                                            var animationCurveGeometryBinding = new FBXAnimationCurveGeometryBinding
                                            {
                                                Geometry = destinationGeometry
                                            };
                                            sourceAnimationCurveNode.AnimationCurveGeometryBinding = animationCurveGeometryBinding;
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.BlendShapeSubDeformer:
                                        {
                                            var sourceAnimationCurveNode = (FBXAnimationCurveNode)sourceObject;
                                            var destinationSubDeformer = (FBXBlendShapeSubDeformer)destinationObject;
                                            var animationCurveBlendShapeBinding = new FBXAnimationCurveBlendShapeBinding
                                            {
                                                BlendShapeSubDeformer = destinationSubDeformer
                                            };
                                            sourceAnimationCurveNode.AnimationCurveBlendShapeBinding = animationCurveBlendShapeBinding;
                                            break;
                                        }
                                    case FBXObjectType.AnimationCurveNode when destinationObject.ObjectType == FBXObjectType.Material:
                                        {
                                            var sourceAnimationCurveNode = (FBXAnimationCurveNode)sourceObject;
                                            var destinationMaterial = (FBXMaterial)destinationObject;
                                            var fbxAnimationCurveMaterialBinding = new FBXAnimationCurveMaterialBinding()
                                            {
                                                Material = destinationMaterial,
                                                Property = destinationProperty
                                            };
                                            sourceAnimationCurveNode.AnimationCurveMaterialBinding = fbxAnimationCurveMaterialBinding;
                                            break;
                                        }
                                    case FBXObjectType.LayeredTexture when destinationObject.ObjectType == FBXObjectType.Material:
                                    case FBXObjectType.Texture when destinationObject.ObjectType == FBXObjectType.Material:
                                        {
                                            var sourceTexture = (FBXTexture)sourceObject;
                                            var destinationMaterial = (FBXMaterial)destinationObject;
                                            var propertyName = destinationProperty;
                                            var indexOfPipe = propertyName.LastIndexOf('|');
                                            if (indexOfPipe >= 0)
                                            {
                                                propertyName = propertyName.Substring(indexOfPipe + 1);
                                            }
                                            var texturePropertyName = $"{propertyName}Tex";
                                            if (!destinationMaterial.HasProperty(texturePropertyName))
                                            {
                                                destinationMaterial.AddProperty(texturePropertyName, sourceTexture, true);
                                            }
                                            else if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                                            {
                                                Debug.LogWarning($"Material [{destinationMaterial.Name}] already has a {propertyName} Texture. A second Texture cannot be set.");
                                            }
                                            break;
                                        }
                                }
                                break;
                            }
                    }
                }
                PostProcessAnimationCurveNodes();
            }
        }
    }
}