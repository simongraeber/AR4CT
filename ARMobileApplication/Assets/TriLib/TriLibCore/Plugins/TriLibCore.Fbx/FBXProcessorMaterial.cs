using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _MultiLayer_token = -3837917831302058659;
        private const long _ShadingModel_token = 1251270102485816868;

        private FBXMaterial ProcessMaterial(FBXNode node, long objectId, string name, string objectClass)
        {
            var material = new FBXMaterial(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
                material.Index = -1;
            }
            else
            {
                material.Index = Document.AllMaterials.Count;
                Document.AllMaterials.Add(material);
            }
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null)
            {
                material.CreateLists();
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        object propertyValue = null;
                        var propertyName = property.Properties.GetStringValue(0, false);
                        var indexOfPipe = propertyName.LastIndexOf('|');
                        if (indexOfPipe >= 0)
                        {
                            propertyName = propertyName.Substring(indexOfPipe + 1);
                        }
                        var propertyType = property.Properties.GetStringValue(1, false);
                        switch (propertyType)
                        {
                            case "KString":
                                {
                                    propertyValue = property.Properties.GetStringValue(4, false);
                                    break;
                                }
                            case "Color":
                            case "ColorRGB":
                            case "RGBColor":
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
                        }
                        material.AddProperty(propertyName, propertyValue, false);
                    }
                }
            }
            var multiLayer = node.GetNodeByName(_MultiLayer_token);
            if (multiLayer != null)
            {
                material.MultiLayer = multiLayer.Properties.GetIntValue(0);
            }
            var shadingModel = node.GetNodeByName(_ShadingModel_token);
            if (shadingModel != null)
            {
                material.ShadingModel = shadingModel.Properties.GetStringValue(0, false);
            }
            return material;
        }
    }
}
