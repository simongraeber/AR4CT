using TriLibCore.General;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _Definitions_token = -8302793431344042917;
        private const long _Count_token = 7096547112121370666;
        private const long _ObjectType_token = -3837881225460453154;
        private const long _NodeAttribute_token = -1862118164518163755;
        private const long _FbxCamera_token = -4289204107841564900;
        private const long _FbxLight_token = -4898811506363003329;
        private const long _Model_token = 7096547112130589252;
        private const long _Material_token = -4898811314753366996;
        private const long _Video_token = 7096547112138722198;
        private const long _Texture_token = -5513532509092662922;
        private const long _LayeredTexture_token = -6372709929711899504;
        private const long _Geometry_token = -4898811476415170697;
        private const long _AnimStack_token = -4289208054268238702;
        private const long _AnimationStack_token = -6872101102931156823;
        private const long _AnimLayer_token = -4289208054275246245;
        private const long _AnimationLayer_token = -6872101102938164366;
        private const long _AnimCurve_token = -4289208054282968327;
        private const long _AnimationCurve_token = -6872101102945886448;
        private const long _AnimCurveNode_token = 6323912633085927579;
        private const long _AnimationCurveNode_token = -1171110211600631502;
        private const long _Deformer_token = -4898811559208448201;
        private const long _SubDeformer_token = -8290080410693404307;
        private const long _Objects_token = -5513532513629462161;

        private void ProcessDefinitions(FBXNode node)
        {
            var definitions = node.GetNodeByName(_Definitions_token);
            if (definitions != null)
            {
                var countsNode = definitions.GetNodeByName(_Count_token);
                Document.ObjectsCount = countsNode != null ? countsNode.Properties.GetIntValue(0) : 1;
                
                var objectTypes = definitions.GetNodesByName(_ObjectType_token);
                foreach (var @object in objectTypes)
                {
                    {
                        var type = @object.Properties.GetStringHashValue(0);
                        const string name = "Definition";
                        const int objectId = -1;
                        const string objectClass = null;
                        switch (type)
                        {
                            case _NodeAttribute_token:
                                var nodeAttribute = ProcessNodeAttribute(@object, objectId, name, objectClass);
                                Document.NodeAttributeDefinition = nodeAttribute;
                                var validCount = false;
                                var count = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                var propertyTemplateNode = @object.GetNodeByName(_PropertyTemplate_token);
                                if (propertyTemplateNode != null)
                                {
                                    var propertyTemplateValue = propertyTemplateNode.Properties.GetStringHashValue(0);
                                    switch (propertyTemplateValue)
                                    {
                                        case _FbxCamera_token:
                                            Document.CamerasCount = count;
                                            validCount = true;
                                            break;
                                        case _FbxLight_token:
                                            Document.LightsCount = count;
                                            validCount = true;
                                            break;
                                    }
                                }
                                if (!validCount)
                                {
                                    Document.CamerasCount = count;
                                    Document.LightsCount = count;
                                }
                                break;
                            case _Model_token:
                                {
                                    var model = ProcessModel(@object, objectId, name, objectClass);
                                    Document.ModelDefinition = model;
                                    Document.ModelsCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    break;
                                }
                            case _Material_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportMaterials)
                                    {
                                        var material = ProcessMaterial(@object, objectId, name, objectClass);
                                        Document.MaterialDefinition = material;
                                        Document.MaterialsCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }
                                    break;
                                }
                            case _Video_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportTextures)
                                    {
                                        var video = ProcessVideo(@object, objectId, name, objectClass);
                                        Document.VideoDefinition = video;
                                        Document.VideosCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _Texture_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportTextures)
                                    {
                                        var texture = ProcessTexture(@object, objectId, name, objectClass);
                                        Document.TextureDefinition = texture;
                                        Document.TexturesCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _LayeredTexture_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportTextures)
                                    {
                                        var texture = ProcessLayeredTexture(@object, objectId, name, objectClass);
                                        Document.TextureDefinition = texture;
                                        Document.LayeredTexturesCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _Geometry_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportMeshes)
                                    {
                                        var geometryGroup = ProcessGeometryGroup(@object, objectId, name, objectClass);
                                        Document.GeometryGroupDefinition = geometryGroup;
                                        Document.GeometriesCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }
                                    break;
                                }
                            case _AnimStack_token:
                            case _AnimationStack_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                    {
                                        var animationStack = ProcessAnimationStack(@object, objectId, name, objectClass);
                                        Document.AnimationStackDefinition = animationStack;
                                        Document.AnimationStacksCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _AnimLayer_token:
                            case _AnimationLayer_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                    {
                                        var animationLayer = ProcessAnimationLayer(@object, objectId, name, objectClass);
                                        Document.AnimationLayerDefinition = animationLayer;
                                        Document.AnimationLayersCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _AnimCurve_token:
                            case _AnimationCurve_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                    {
                                        var animationCurve = ProcessAnimationCurve(@object, objectId, name, objectClass);
                                        Document.AnimationCurveDefinition = animationCurve;
                                        Document.AnimationCurvesCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _AnimCurveNode_token:
                            case _AnimationCurveNode_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                    {
                                        var animationCurveNode = ProcessAnimationCurveNode(@object, objectId, name, objectClass);
                                        Document.AnimationCurveNodeDefinition = animationCurveNode;
                                        Document.AnimationCurveNodesCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }

                                    break;
                                }
                            case _Deformer_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None || Reader.AssetLoaderContext.Options.ImportBlendShapes)
                                    {
                                        var deformer = ProcessDeformer(@object, objectId, name, objectClass);
                                        Document.DeformerDefinition = deformer;
                                        Document.DeformersCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }
                                    break;
                                }
                            case _SubDeformer_token:
                                {
                                    if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None || Reader.AssetLoaderContext.Options.ImportBlendShapes)
                                    {
                                        var subDeformer = ProcessSubDeformer(@object, objectId, name, objectClass);
                                        Document.SubDeformerDefinition = subDeformer;
                                        Document.SubDeformersCount = @object.GetNodeByName(_Count_token).Properties.GetIntValue(0);
                                    }
                                    break;
                                }
                        }
                    }
                }

                var objects = node.GetNodeByName(_Objects_token);
                if (objects != null)
                {
                    if (Document.ModelsCount == 0)
                    {
                        var models = objects.GetNodesByName(_Model_token);
                        
                        foreach (var model in models)
                        {
                            Document.ModelsCount++;
                        }
                    }
                    if (Document.GeometriesCount == 0)
                    {
                        var models = objects.GetNodesByName(_Geometry_token);
                        
                        foreach (var model in models)
                        {
                            Document.GeometriesCount++;
                        }
                    }
                    if (Document.MaterialsCount == 0)
                    {
                        var models = objects.GetNodesByName(_Material_token);
                        
                        foreach (var model in models)
                        {
                            Document.MaterialsCount++;
                        }
                    }
                    if (Document.SubDeformersCount == 0)
                    {
                        var models = objects.GetNodesByName(_SubDeformer_token);
                        
                        foreach (var model in models)
                        {
                            Document.SubDeformersCount++;
                        }
                    }
                    if (Document.DeformersCount == 0)
                    {
                        var models = objects.GetNodesByName(_Deformer_token);
                        
                        foreach (var model in models)
                        {
                            Document.DeformersCount++;
                        }
                    }
                    if (Document.AnimationCurvesCount == 0)
                    {
                        var models = objects.GetNodesByName(_AnimationCurve_token);
                        
                        foreach (var model in models)
                        {
                            Document.AnimationCurvesCount++;
                        }
                    }
                    if (Document.AnimationLayersCount == 0)
                    {
                        var models = objects.GetNodesByName(_AnimationLayer_token);
                        
                        foreach (var model in models)
                        {
                            Document.AnimationLayersCount++;
                        }
                    }
                    if (Document.AnimationStacksCount == 0)
                    {
                        var models = objects.GetNodesByName(_AnimationStack_token);
                        
                        foreach (var model in models)
                        {
                            Document.AnimationStacksCount++;
                        }
                    }
                    if (Document.TexturesCount == 0)
                    {
                        var models = objects.GetNodesByName(_Texture_token);
                        
                        foreach (var model in models)
                        {
                            Document.TexturesCount++;
                        }
                    }
                }
            }
        }
    }
}