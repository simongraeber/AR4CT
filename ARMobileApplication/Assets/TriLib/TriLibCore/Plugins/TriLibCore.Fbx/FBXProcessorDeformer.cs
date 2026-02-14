using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _Link_DeformAcuracy_token = 1539041074812752531;
        private const long _Indexes_token = -5513532518616455845;
        private const long _Transform_token = -4289191746330321657;
        private const long _TransformLink_token = 2937905295581636001;
        private const long _Weights_token = -5513532506444404394;
        private const long _DeformPercent_token = 8455858521895960539;
        private const long _FullWeights_token = -8300725961468769721;

        private FBXDeformer ProcessDeformer(FBXNode node, long objectId, string name, string objectClass)
        {
            var deformer = new FBXDeformer(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.Deformers.Add(deformer);
            }
            if (node != null && node.HasSubNodes)
            {
                foreach (var subNode in node)
                {
                    if (subNode.NameHashCode == _Link_DeformAcuracy_token)
                    {
                        deformer.Link_DeformAcuracy = subNode.Properties.GetFloatValue(0);
                    }
                }
            }
            return deformer;
        }

        private FBXBlendShapeSubDeformer ProcessBlendShapeSubDeformer(FBXNode node, long objectId, string name, string objectClass)
        {
            var subDeformer = new FBXBlendShapeSubDeformer(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.SubDeformers.Add(subDeformer);
            }
            var deformPercent = node.GetNodeByName(_DeformPercent_token);
            if (deformPercent != null)
            {
                subDeformer.DeformPercent = deformPercent.Properties.GetFloatValue(0);
            }
            var fullWeights = node.GetNodeByName(_FullWeights_token);
            if (fullWeights != null)
            {
                subDeformer.FullWeights = fullWeights.Properties.GetFloatValues();
            }
            return subDeformer;
        }

        private FBXSubDeformer ProcessSubDeformer(FBXNode node, long objectId, string name, string objectClass)
        {
            var subDeformer = new FBXSubDeformer(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.SubDeformers.Add(subDeformer);
            }
            if (node.HasSubNodes)
            {
                foreach (var subNode in node)
                {
                    switch (subNode.NameHashCode)
                    {
                        case _Indexes_token:
                            subDeformer.Indexes = subNode.Properties.GetIntValues();
                            break;
                        case _Transform_token when !subDeformer.transformParsed: // this is a workaround for Clusters with two Transform nodes
                            subDeformer.Transform = subNode.Properties.GetMatrixValue();
                            subDeformer.transformParsed = true;
                            break;
                        case _TransformLink_token:
                            subDeformer.TransformLink = subNode.Properties.GetMatrixValue();
                            break;
                        case _Weights_token:
                            subDeformer.Weights = subNode.Properties.GetFloatValues();
                            break;
                    }
                }
            }
            return subDeformer;
        }

        private void PostProcessDeformers()
        {
            for (var subDeformerIndex = 0; subDeformerIndex < Document.SubDeformers.Count; subDeformerIndex++)
            {
                var subDeformer = Document.SubDeformers[subDeformerIndex];
                var deformer = subDeformer.BaseDeformer;
                if (deformer.Geometry != null)
                {
                    var model = deformer.Geometry.Model;

                    if (model.Bones == null)
                    {
                        model.Bones = new List<IModel>(model.BonesCount);
                        model.BindPosesList = new List<Matrix4x4>(model.BonesCount);
                    }
                    if (model.Bones.Contains(subDeformer.Model))
                    {
                        continue;
                    }

                    if (subDeformer.Model == null)
                    {
                        if (Reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                        {
                            Debug.LogWarning($"Deformer [{subDeformer.Name}] has no model assigned.");
                        }
                        continue;
                    }
                    
                    var modelIndex = model.Bones.Count;
                    model.Bones.Add(subDeformer.Model);

                    var transformLink = subDeformer.TransformLink;

                    var globalMatrix = model.GetGlobalMatrix(Reader.AssetLoaderContext);

                    var newTransform = transformLink.inverse * globalMatrix; 
                 
                    model.BindPosesList.Add(Document.ConvertMatrixComplex(newTransform, null));

                    if (subDeformer.Indexes != null && subDeformer.Weights != null)
                    {
                        var indexesCount = subDeformer.Indexes.Count;
                        for (var i = 0; i < indexesCount; i++)
                        {
                            var vertexIndex = subDeformer.Indexes[i];
                            var weight = subDeformer.Weights[i];
                            deformer.Geometry.InnerGeometryGroup.AddBoneWeight(vertexIndex, new BoneWeight1
                            {
                                boneIndex = modelIndex,
                                weight = weight
                            });
                        }
                    }
                }
            }
        }
    }
}
