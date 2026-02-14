using TriLibCore.General;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private void ProcessObjects(FBXNode node)
        {
            var objectsNode = node.GetNodeByName(_Objects_token);
            if (objectsNode!= null)
            {
                if (objectsNode.HasSubNodes)
                {
                    foreach (var fbxNode in objectsNode)
                    {
                        var objectId = fbxNode.Properties.GetLongValue(0);
                        if (Document.ObjectExists(objectId))
                        {
                            continue;
                        }
                        var nameAndType = fbxNode.Properties.GetStringValue(1, false);
                        SplitObjectNameData(nameAndType, out var name, out var type);
                        var objectClass = fbxNode.Properties.GetStringValue(2, false);
                        switch (type)
                        {
                            case "NodeAttribute":
                                var nodeAttribute = ProcessNodeAttribute(fbxNode, objectId, name, objectClass);
                                Document.Objects.Add(objectId, nodeAttribute);
                                break;
                            case "Model":
                                var model = ProcessModel(fbxNode, objectId, name, objectClass);
                                Document.Objects.Add(objectId, model);
                                break;
                            case "Material":
                                if (Reader.AssetLoaderContext.Options.ImportMaterials)
                                {
                                    var material = ProcessMaterial(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, material);
                                }
                                break;
                            case "Video":
                                if (Reader.AssetLoaderContext.Options.ImportTextures)
                                {
                                    var video = ProcessVideo(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, video);
                                }
                                break;
                            case "Texture":
                                if (Reader.AssetLoaderContext.Options.ImportTextures)
                                {
                                    var texture = ProcessTexture(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, texture);
                                }
                                break;
                            case "LayeredTexture":
                                if (Reader.AssetLoaderContext.Options.ImportTextures)
                                {
                                    var texture = ProcessLayeredTexture(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, texture);
                                }
                                break;
                            case "Geometry":
                                if (Reader.AssetLoaderContext.Options.ImportMeshes)
                                {
                                    if (Reader.AssetLoaderContext.Options.ImportBlendShapes && objectClass == "Shape")
                                    {
                                        var geometryGroup = ProcessBlendShapeGeometryGroup(fbxNode, objectId, name, objectClass);
                                        Document.Objects.Add(objectId, geometryGroup);
                                    }
                                    else if (objectClass == "Mesh")
                                    {
                                        var geometryGroup = ProcessGeometryGroup(fbxNode, objectId, name, objectClass);
                                        Document.Objects.Add(objectId, geometryGroup);
                                    }
                                }
                                break;
                            case "AnimStack":
                            case "AnimationStack":
                                if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                {
                                    var animationStack = ProcessAnimationStack(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, animationStack);
                                }
                                break;
                            case "AnimLayer":
                            case "AnimationLayer":
                                if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                {
                                    var animationLayer = ProcessAnimationLayer(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, animationLayer);
                                }
                                break;
                            case "AnimCurve":
                            case "AnimationCurve":
                                if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                {
                                    var animationCurve = ProcessAnimationCurve(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, animationCurve);
                                }
                                break;
                            case "AnimCurveNode":
                            case "AnimationCurveNode":
                                if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
                                {
                                    var animationCurveNode = ProcessAnimationCurveNode(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, animationCurveNode);
                                }
                                break;
                            case "Deformer":
                                if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None || Reader.AssetLoaderContext.Options.ImportBlendShapes)
                                {
                                    var deformer = ProcessDeformer(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, deformer);
                                }
                                break;
                            case "SubDeformer":
                                if (objectClass == "BlendShapeChannel")
                                {
                                    var subDeformer = ProcessBlendShapeSubDeformer(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, subDeformer);
                                }
                                else if (objectClass == "Cluster")
                                {
                                    var subDeformer = ProcessSubDeformer(fbxNode, objectId, name, objectClass);
                                    Document.Objects.Add(objectId, subDeformer);
                                }
                                break;
                            case "Implementation":
                                var implementation = ProcessImplementation(fbxNode, objectId, name, objectClass);
                                Document.Objects.Add(objectId, implementation);
                                break;
                            case "BindingTable":
                                var bindingTable = ProcessBindingTable(fbxNode, objectId, name, objectClass);
                                Document.Objects.Add(objectId, bindingTable);
                                break;
                        }
                    }
                }
            }
        }

        private FBXNodeAttribute ProcessNodeAttribute(FBXNode fbxNode, long objectId, string name, string objectClass)
        {
            var nodeAttribute = new FBXNodeAttribute(Document, fbxNode, objectId, name, objectClass);
            return nodeAttribute;
        }
    }
}