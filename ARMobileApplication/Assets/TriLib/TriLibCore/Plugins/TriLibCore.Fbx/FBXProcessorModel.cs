using System.Collections.Generic;
using TriLibCore.Extensions;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private FBXModel ProcessModel(FBXNode node, long objectId, string name, string objectClass)
        {
            FBXModel model;
            if (Reader.AssetLoaderContext.Options.ImportCameras && objectClass == "Camera")
            {
                model = new FBXCamera(Document, name, objectId, objectClass);
                Document.AllCameras.Add((FBXCamera)model);
            }
            else if (Reader.AssetLoaderContext.Options.ImportLights && objectClass == "Light")
            {
                model = new FBXLight(Document, name, objectId, objectClass);
                Document.AllLights.Add((FBXLight)model);
            }
            else
            {
                model = new FBXModel(Document, name, objectId, objectClass);
            }
            model.Name = Reader.MapName(Reader.AssetLoaderContext, new ModelNamingData() { ModelName = name, Id = objectId.ToString(), Class = objectClass }, model, Reader.Name);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AllModels.Add(model);
            }
            model.IsBone = objectClass == "LimbNode" || objectClass == "Null";
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringValue(0, false);
                        switch (propertyName)
                        {
                            case "Visibility":
                                model.Visibility = property.Properties.GetBoolValue(4);
                                break;
                            case "Visibility Inheritance":
                                model.VisibilityInheritance = property.Properties.GetBoolValue(4);
                                break;
                            case "RotationOrder":
                                model.RotationOrder = (FBXRotationOrder)property.Properties.GetIntValue(4);
                                break;
                            case "InheritType":
                                model.InheritType = (FBXInheritType)property.Properties.GetIntValue(4);
                                break;
                            case "Lcl Translation":
                                model.Matrices.Update(FBXMatrixType.LclTranslation, property.Properties.GetVector3Value(4));
                                break;
                            case "RotationOffset":
                                model.Matrices.Update(FBXMatrixType.RotationOffset, property.Properties.GetVector3Value(4));
                                break;
                            case "RotationPivot":
                                model.Matrices.Update(FBXMatrixType.RotationPivot, property.Properties.GetVector3Value(4));
                                break;
                            case "PreRotation":
                                model.Matrices.Update(FBXMatrixType.PreRotation, property.Properties.GetVector3Value(4));
                                break;
                            case "Lcl Rotation":
                                model.Matrices.Update(FBXMatrixType.LclRotation, property.Properties.GetVector3Value(4));
                                break;
                            case "PostRotation":
                                model.Matrices.Update(FBXMatrixType.PostRotation, property.Properties.GetVector3Value(4));
                                break;
                            case "ScalingOffset":
                                model.Matrices.Update(FBXMatrixType.ScalingOffset, property.Properties.GetVector3Value(4));
                                break;
                            case "ScalingPivot":
                                model.Matrices.Update(FBXMatrixType.ScalingPivot, property.Properties.GetVector3Value(4));
                                break;
                            case "Lcl Scaling":
                                model.Matrices.Update(FBXMatrixType.LclScaling, property.Properties.GetVector3Value(4));
                                break;
                            case "GeometricTranslation":
                                model.Matrices.Update(FBXMatrixType.GeometricTranslation, property.Properties.GetVector3Value(4));
                                break;
                            case "GeometricRotation":
                                model.Matrices.Update(FBXMatrixType.GeometricRotation, property.Properties.GetVector3Value(4));
                                break;
                            case "GeometricScaling":
                                model.Matrices.Update(FBXMatrixType.GeometricScaling, property.Properties.GetVector3Value(4));
                                break;
                        }

                        var propertyFlags = property.Properties.GetStringValue(3, false);
                        if (Reader.AssetLoaderContext.Options.UserPropertiesMapper != null && propertyFlags != null && propertyFlags.Contains("U"))
                        {
                            var propertyTypeName = property.Properties.GetStringValue(1, false);
                            object propertyValue = null;
                            switch (propertyTypeName)
                            {
                                case "KString":
                                    {
                                        propertyValue = property.Properties.GetStringValue(4, false);
                                        break;
                                    }
                                case "Color":
                                case "ColorRGB":
                                    {
                                        propertyValue = property.Properties.GetColorValue(4);
                                        break;
                                    }
                                case "ColorAndAlpha":
                                    {
                                        propertyValue = property.Properties.GetColorAlphaValue(4);
                                        break;
                                    }
                                case "Number":
                                case "float":
                                case "double":
                                case "Float":
                                    {
                                        propertyValue = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                                case "Int":
                                case "int":
                                case "enum":
                                case "Integer":
                                    {
                                        propertyValue = property.Properties.GetIntValue(4);
                                        break;
                                    }
                                case "Vector2D":
                                    {
                                        propertyValue = property.Properties.GetVector2Value(4);
                                        break;
                                    }
                                case "Vector":
                                case "Vector3D":
                                    {
                                        propertyValue = property.Properties.GetVector3Value(4);
                                        break;
                                    }
                                case "Vector4D":
                                    {
                                        propertyValue = property.Properties.GetVector4Value(4);
                                        break;
                                    }
                                case "bool":
                                case "Bool":
                                    {
                                        propertyValue = property.Properties.GetBoolValue(4);
                                        break;
                                    }
                                default:
                                    {
                                        if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                                        {
                                            UnityEngine.Debug.Log($"Unknown property type: {propertyTypeName}");
                                        }
                                        break;
                                    }
                            }

                            if (model.UserProperties == null)
                            {
                                model.UserProperties = new Dictionary<string, object>(2);
                            }

                            model.UserProperties.Add(propertyName, propertyValue);
                        }
                    }
                }
            }
            return model;
        }

        private void PostProcessModelGeometries()
        {
            for (var m = 0; m < Document.AllModels.Count; m++)
            {
                var model = (FBXModel)Document.AllModels[m];
                if (model.GeometryGroup != null)
                {
                    var geometryGroup = model.GeometryGroup;
                    model.MaterialIndices = new int[geometryGroup.GeometriesData.Count];
                    foreach (var kvp in geometryGroup.GeometriesData)
                    {
                        var geometryGroupGeometry = (IFBXGeometry)kvp.Value;
                        if (model.AllMaterialIndices == null || model.AllMaterialIndices.Count == 0)
                        {
                            model.MaterialIndices[geometryGroupGeometry.Index] = -1;
                        }
                        else
                        {
                            if (geometryGroupGeometry.MaterialIndex < 0 || geometryGroupGeometry.MaterialIndex >= model.AllMaterialIndices.Count)
                            {
                                model.MaterialIndices[geometryGroupGeometry.Index] = -1;
                            }
                            else
                            {
                                model.MaterialIndices[geometryGroupGeometry.Index] = model.AllMaterialIndices[geometryGroupGeometry.MaterialIndex];
                            }
                        }
                    }
                }
                model.SetupBindPoses();
            }
        }

        private void PostProcessModels()
        {
            Document.LocalPosition = Vector3.zero;
            Document.LocalRotation = Quaternion.identity;
            Document.LocalScale = Vector3.one;
            for (var m = 0; m < Document.AllModels.Count; m++)
            {
                var model = (FBXModel)Document.AllModels[m];
                model.TransformMatrices(Reader.AssetLoaderContext);
                var localPosition = model.LocalPosition;
                var localRotation = model.LocalRotation;
                var localScale = model.LocalScale;
                Document.ConvertMatrix(ref localPosition, ref localRotation, ref localScale, model);
                if (!Reader.AssetLoaderContext.Options.BakeAxisConversion)
                {
                    if (model is FBXLight)
                    {
                        localRotation *= Quaternion.Euler(90f, 0f, 0f);
                    }
                    else if (model is FBXCamera)
                    {
                        localRotation *= Quaternion.Euler(0f, -90f, 0f);
                    }
                }
                model.LocalPosition = localPosition;
                model.LocalRotation = localRotation;
                model.LocalScale = localScale;
            }
        }

        private void ProcessPivots(FBXModel model)
        {
            if (model != Document && (model.BindPoses == null || model.BindPoses.Length == 0))
            {
                var pivotMatrix = model.Matrices.GetMatrix(FBXMatrixType.RotationPivot);
                Quaternion _ = default;
                var finalPivot = Document.ConvertVector(pivotMatrix, true);
                Document.ApplyDocumentOrientation(ref finalPivot, ref _, model);
                var matrix = model.GetGlobalMatrixNoScale();
                model.OriginalGlobalMatrix = matrix;
                var worldSpacePivot = matrix.MultiplyPoint(finalPivot);
                model.Pivot = worldSpacePivot;
                model.HasCustomPivot = true;

            }
            if (model.Children != null)
            {
                for (var i = 0; i < model.Children.Count; i++)
                {
                    var child = (FBXModel)model.Children[i];
                    ProcessPivots(child);
                }
            }
        }
    }
}
