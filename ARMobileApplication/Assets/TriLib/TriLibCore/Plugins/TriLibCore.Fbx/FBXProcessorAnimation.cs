using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private const long _X_token = 34902897112120595;
        private const long _Y_token = 34902897112120596;
        private const long _Z_token = 34902897112120597;
        private const long _Visibility_token = -3837689926742612777;
        private const long _KeyTime_token = -5513532517080237529;
        private const long _KeyValueFloat_token = -4462348612258096315;
        private const long _KeyAttrDataFloat_token = -8194675643196809273;
        private const long _KeyAttrRefCount_token = -9190187893921319257;
        private const long _KeyAttrFlags_token = 1046148410471322620;
        private const long _Pre_Extrapolation_token = 3932256567092467113;
        private const long _Type_token = 6774539739449755839;
        private const long _Repetition_token = -3837799182367699402;
        private const long _Post_Extrapolation_token = -3964146216341699824;
        private const long _BlendMode_token = -4289207259909263569;
        private const long _RotationAccumulationMode_token = -6240473710179061333;
        private const long _ScaleAccumulationMode_token = 1007376254017630093;
        private const long _Weight_token = -1367968408189145411;
        private const long _Mute_token = 6774539739449543582;
        private const long _Solo_token = 6774539739449716324;
        private const long _Lock_token = 6774539739449507504;
        private const long _mLayerID_token = -4898810453513155612;
        private const long _BlendModeByPass_token = -6112191667541287657;
        private const long _MutedForParent_token = -3222834754088061715;
        private const long _LockedByParent_token = 4100154850135877712;
        private const long _MutedForSolo_token = 1109977441749390818;
        private const long _MultiTake_token = -4289197624105211141;
        private const long _Description_token = -8302782505289598249;
        private const long _LocalStart_token = -3837949652653645444;
        private const long _LocalStop_token = -4289198650600661080;
        private const long _ReferenceStart_token = -4773494372191042788;
        private const long _ReferenceStop_token = 1036128831458970120;

        private string ParseAnimationProperty(string property)
        {
            var valuesCount = property.SplitNoAlloc('|', _values);
            return valuesCount > 1 ? _values[1] : _values[0];
        }

        private FBXAnimationCurveNode ProcessAnimationCurveNode(FBXNode node, long objectId, string name, string objectClass)
        {
            var animationCurveNode = new FBXAnimationCurveNode(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AnimationCurveNodes.Add(animationCurveNode);
            }
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null)
            {
                if (properties.HasSubNodes)
                {
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringValue(0, false);
                        var valuesCount = propertyName.SplitNoAlloc('|', _values);
                        if (valuesCount == 2)
                        {
                            var value1Hash = HashUtils.GetHash(_values[1]);
                            switch (value1Hash)
                            {
                                case _X_token:
                                    {
                                        animationCurveNode.DX = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                                case _Y_token:
                                    {
                                        animationCurveNode.DY = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                                case _Z_token:
                                    {
                                        animationCurveNode.DZ = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                                case _DeformPercent_token:
                                    {
                                        animationCurveNode.DeformPercent = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                                case _Visibility_token:
                                    {
                                        animationCurveNode.Visibility = property.Properties.GetFloatValue(4);
                                        break;
                                    }
                            }
                        }
                    }
                }
            }
            return animationCurveNode;
        }

        private FBXAnimationCurve ProcessAnimationCurve(FBXNode node, long objectId, string name, string objectClass)
        {
            var animationCurve = new FBXAnimationCurve(Document, name, objectId, objectClass);
            if (objectId != -1)
            {
                animationCurve.Node = node;
                var keyTime = node.GetNodeByName(_KeyTime_token);
                var keyValueFloat = node.GetNodeByName(_KeyValueFloat_token);
                var keyAttrDataFloat = node.GetNodeByName(_KeyAttrDataFloat_token);
                var keyAttrRefCount = node.GetNodeByName(_KeyAttrRefCount_token);
                var keyAttrFlags = node.GetNodeByName(_KeyAttrFlags_token);

                var preExtrapolation = node.GetNodeByName(_Pre_Extrapolation_token);
                if (preExtrapolation != null)
                {
                    var type = preExtrapolation.GetNodeByName(_Type_token).Properties.GetStringValue(0, false);
                    var repetition = preExtrapolation.GetNodeByName(_Repetition_token).Properties.GetIntValue(0);
                    animationCurve.PreExtrapolationType = ParseExtrapolationType(type[0]);
                    animationCurve.PreExtrapolationRepetition = repetition;
                }
                else
                {
                    animationCurve.PreExtrapolationType = FBXAnimationCurve.eConstant;
                    animationCurve.PreExtrapolationRepetition = -1;
                }

                var postExtrapolation = node.GetNodeByName(_Post_Extrapolation_token);
                if (postExtrapolation != null)
                {
                    var type = postExtrapolation.GetNodeByName(_Type_token).Properties.GetStringValue(0, false);
                    var repetition = postExtrapolation.GetNodeByName(_Repetition_token).Properties.GetIntValue(0);
                    animationCurve.PostExtrapolationType = ParseExtrapolationType(type[0]);
                    animationCurve.PostExtrapolationRepetition = repetition;
                }
                else
                {
                    animationCurve.PostExtrapolationType = FBXAnimationCurve.eConstant;
                    animationCurve.PostExtrapolationRepetition = -1;
                }
                animationCurve.KeyTimes = keyTime.Properties.GetLongValues(true);
                animationCurve.KeyValues = keyValueFloat.Properties.GetFloatValues();
                animationCurve.KeyAttrRefCounts = keyAttrRefCount.Properties.GetIntValues();
                animationCurve.KeyAttrDataValues = keyAttrDataFloat.Properties.GetIEE754Values();
                animationCurve.KeyFlags = keyAttrFlags.Properties.GetIntValues();
                Document.AnimationCurves.Add(animationCurve);
            }
            return animationCurve;
        }

        private static int ParseExtrapolationType(char type)
        {
            switch (type)
            {
                case 'K':
                    return FBXAnimationCurve.eKeepSlope;
                case 'M':
                    return FBXAnimationCurve.eMirrorRepetition;
                case 'R':
                    return FBXAnimationCurve.eRepetition;
            }
            return FBXAnimationCurve.eConstant;
        }

        private static void UpdateMatrix(
            FBXAnimatedMatrices matrices,
            FBXMatrixType matrixType,
            int fieldIndex,
            FBXAnimationLayer animationLayer,
            float newValue,
            FBXAnimationCurve.KeyTangent? tangentsAndWeights = null)
        {
            var animationLayerWeight = animationLayer.Weight / 100f;
            var existingValue = matrices.GetField(matrixType, fieldIndex, false);
            switch (animationLayer.BlendMode)
            {
                case FBXAnimationLayerBlendMode.eBlendAdditive:
                    {
                        switch (matrixType)
                        {
                            case FBXMatrixType.LclScaling:
                                existingValue = animationLayer.ScaleAccumulationMode == FBXScaleAccumulationMode.eScaleAdditive ? existingValue + newValue : Mathf.Pow(existingValue, animationLayerWeight) * newValue;
                                break;
                            case FBXMatrixType.LclRotation:
                                if (animationLayer.RotationAccumulationMode == FBXRotationAccumulationOrder.eRotationByChannel)
                                {
                                    throw new System.Exception();
                                }
                                else
                                {
                                    existingValue += newValue * animationLayerWeight;
                                }
                                break;
                            default:
                                existingValue += newValue * animationLayerWeight;
                                break;
                        }
                        break;
                    }
                case FBXAnimationLayerBlendMode.eBlendOverride:
                    {
                        existingValue = newValue * animationLayerWeight;
                        break;
                    }
                case FBXAnimationLayerBlendMode.eBlendOverridePassthrough:
                    {
                        existingValue = Mathf.Lerp(existingValue, newValue, animationLayerWeight);
                        break;
                    }
            }
            matrices.UpdateField(matrixType, fieldIndex, existingValue, tangentsAndWeights.GetValueOrDefault(),  false);
        }

        private static void UpdateBlendShapeChannels(FBXBlendShapeChannels matrices, string channelName, FBXAnimationLayer animationLayer, float newValue, FBXAnimationCurve.KeyTangent? tangentsAndWeights = null)//, int interpolationType = 0, int tangentMode = 0)
        {
            var animationLayerWeight = animationLayer.Weight / 100f;
            var channelIndex = matrices.GetChannelIndex(channelName);
            var existingValue = matrices.GetMatrix(channelIndex);
            switch (animationLayer.BlendMode)
            {
                case FBXAnimationLayerBlendMode.eBlendAdditive:
                    {
                        existingValue += newValue * animationLayerWeight;
                        break;
                    }
                case FBXAnimationLayerBlendMode.eBlendOverride:
                    {
                        existingValue = newValue * animationLayerWeight;
                        break;
                    }
                case FBXAnimationLayerBlendMode.eBlendOverridePassthrough:
                    {
                        existingValue = Mathf.Lerp(existingValue, newValue, animationLayerWeight);
                        break;
                    }
            }
            matrices.Update(channelIndex, existingValue, tangentsAndWeights.GetValueOrDefault());
        }

        private void PostProcessAnimations()
        {

            for (var animationIndex = 0; animationIndex < Document.AllAnimations.Count; animationIndex++)
            {
                var animationStack = (FBXAnimationStack)Document.AllAnimations[animationIndex];

                if (animationStack.AnimatedModels == null)
                {
                    continue;
                }
                
                var animatedMatrices = new FBXAnimatedMatrices();
                var blendShapeChannels = new FBXBlendShapeChannels();
                var blendShapeAnimationCurves = new FBXGenericAnimationCurve[FBXBlendShapeChannels.MaxChannels];

                var allAnimatedTimes = new SortedSet<long>(animationStack.AnimatedTimes);
                var allAnimatedLayers = new SortedSet<FBXAnimationLayer>(animationStack.AnimatedLayers);

                var animationStart = animationStack.LocalStart;
                var animationStop = animationStack.GetLocalStop();
                var step = (long)(Document.ConvertToFBXTime(1f / animationStack.FrameRate) * Reader.AssetLoaderContext.Options.ResampleFrequency);

                for (var animationTime = animationStart; animationTime <= animationStop; animationTime += step)
                {
                    allAnimatedTimes.Add(animationTime);
                }

                foreach (var model in animationStack.AnimatedModels)
                {
                    FBXGenericAnimationCurve localPositionKeyframesX = null;
                    FBXGenericAnimationCurve localPositionKeyframesY = null;
                    FBXGenericAnimationCurve localPositionKeyframesZ = null;
                    FBXGenericAnimationCurve localRotationKeyframesX = null;
                    FBXGenericAnimationCurve localRotationKeyframesY = null;
                    FBXGenericAnimationCurve localRotationKeyframesZ = null;
                    FBXGenericAnimationCurve localRotationKeyframesW = null;
                    FBXGenericAnimationCurve localScaleKeyframesX = null;
                    FBXGenericAnimationCurve localScaleKeyframesY = null;
                    FBXGenericAnimationCurve localScaleKeyframesZ = null;
                    FBXGenericAnimationCurve visibilityKeyframes = null;

                    var curvesCount = 0;

                    foreach (var animationLayer in allAnimatedLayers)
                    {
                        foreach (var animationTime in allAnimatedTimes)
                        {
                            if (animationTime > animationStop)
                            {
                                continue;
                            }
                            ProcessResampled(
                                animationStack,
                                animationLayer,
                                animatedMatrices,
                                blendShapeChannels,
                                model,
                                animationTime,
                                animationStart,
                                ref curvesCount,
                                ref localRotationKeyframesX,
                                ref localRotationKeyframesY,
                                ref localRotationKeyframesZ,
                                ref localRotationKeyframesW,
                                ref localPositionKeyframesX,
                                ref localPositionKeyframesY,
                                ref localPositionKeyframesZ,
                                ref localScaleKeyframesX,
                                ref localScaleKeyframesY,
                                ref localScaleKeyframesZ,
                                blendShapeAnimationCurves);
                            ProcessCommon(
                                animationStack,
                                animatedMatrices,
                                model,
                                blendShapeChannels,
                                animationLayer,
                                animationTime,
                                animationStart,
                                ref curvesCount,
                                ref visibilityKeyframes
                                );
                        }
                    }
                    var genericAnimationCurves = new List<IAnimationCurve>(curvesCount + blendShapeAnimationCurves.Length);
                    for (var blendShapeAnimationCurveIndex = 0; blendShapeAnimationCurveIndex < blendShapeAnimationCurves.Length; blendShapeAnimationCurveIndex++)
                    {
                        var blendShapeAnimationCurve = blendShapeAnimationCurves[blendShapeAnimationCurveIndex];
                        if (blendShapeAnimationCurve == null)
                        {
                            break;
                        }
                        genericAnimationCurves.Add(blendShapeAnimationCurve);
                    }
                    if (localPositionKeyframesX != null)
                    {
                        localPositionKeyframesX.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localPositionKeyframesX);
                    }
                    if (localPositionKeyframesY != null)
                    {
                        localPositionKeyframesY.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localPositionKeyframesY);
                    }
                    if (localPositionKeyframesZ != null)
                    {
                        localPositionKeyframesZ.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localPositionKeyframesZ);
                    }
                    if (localRotationKeyframesX != null)
                    {
                        localRotationKeyframesX.AnimationCurve.SetTangents(TangentMode.Linear);
                        localRotationKeyframesY.AnimationCurve.SetTangents(TangentMode.Linear);
                        localRotationKeyframesZ.AnimationCurve.SetTangents(TangentMode.Linear);
                        localRotationKeyframesW.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localRotationKeyframesX);
                        genericAnimationCurves.Add(localRotationKeyframesY);
                        genericAnimationCurves.Add(localRotationKeyframesZ);
                        genericAnimationCurves.Add(localRotationKeyframesW);
                    }
                    if (localScaleKeyframesX != null)
                    {
                        localScaleKeyframesX.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localScaleKeyframesX);
                    }
                    if (localScaleKeyframesY != null)
                    {
                        localScaleKeyframesY.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localScaleKeyframesY);
                    }
                    if (localScaleKeyframesZ != null)
                    {
                        localScaleKeyframesZ.AnimationCurve.SetTangents(TangentMode.Linear);
                        genericAnimationCurves.Add(localScaleKeyframesZ);
                    }
                    if (visibilityKeyframes != null)
                    {
                        genericAnimationCurves.Add(visibilityKeyframes);
                    }
                    if (animationStack.AnimationCurveBindings == null)
                    {
                        animationStack.AnimationCurveBindings = new List<IAnimationCurveBinding>();
                    }
                    animationStack.AnimationCurveBindings.Add(new FBXAnimationCurveBinding
                    {
                        Model = model,
                        AnimationCurves = genericAnimationCurves
                    });
                }
            }
        }

        private void ProcessCommon(
           FBXAnimationStack animationStack,
           FBXAnimatedMatrices animatedMatrices,
           FBXModel model,
           FBXBlendShapeChannels blendShapeChannels,
           FBXAnimationLayer animationLayer,
           long animationTime,
           long animationStart,
           ref int curvesCount,
           ref FBXGenericAnimationCurve visibilityKeyframes
           )
        {
            animatedMatrices.Reset(model);
            blendShapeChannels.Reset();
            if (animationLayer.CurveNodesDictionary != null && animationLayer.CurveNodesDictionary.TryGetValue(model, out var animationCurveNodes))
            {
                for (var animationCurveNodeIndex = 0; animationCurveNodeIndex < animationCurveNodes.Count; animationCurveNodeIndex++)
                {
                    var animationCurveNode = animationCurveNodes[animationCurveNodeIndex];
                    if (animationCurveNode.AnimationCurves == null)
                    {
                        continue;
                    }
                    for (var animationCurveIndex = 0; animationCurveIndex < animationCurveNode.AnimationCurves.Count; animationCurveIndex++)
                    {
                        var animationCurve = animationCurveNode.AnimationCurves[animationCurveIndex];
                        if (animationCurve.KeyTimes.Count == 0 || animationCurve.AnimationCurveNodeBinding.FieldIndex < 0)
                        {
                            continue;
                        }
                        if (
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType == FBXMatrixType.LclRotation ||
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType == FBXMatrixType.LclTranslation ||
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType == FBXMatrixType.LclScaling
                            )
                        {
                            continue;
                        }
                        animationCurve.GetKeyframe(animationTime, out _, out var keyValue, out _, out var tangents);
                        UpdateMatrix(
                            animatedMatrices,
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType,
                            animationCurve.AnimationCurveNodeBinding.FieldIndex,
                            animationLayer,
                            keyValue,
                            tangents);
                    }
                }
            }
            if (Reader.AssetLoaderContext.Options.ImportBlendShapes && animationLayer.GeometryCurveNodesDictionary != null && animationLayer.GeometryCurveNodesDictionary.TryGetValue(model, out var geometryAnimationCurveNodes))
            {
                for (var animationCurveNodeIndex = 0; animationCurveNodeIndex < geometryAnimationCurveNodes.Count; animationCurveNodeIndex++)
                {
                    var animationCurveNode = geometryAnimationCurveNodes[animationCurveNodeIndex];
                    if (animationCurveNode.AnimationCurves == null)
                    {
                        continue;
                    }
                    var destinationProperty = string.Format(Constants.BlendShapePathFormat, animationCurveNode.AnimationCurveBlendShapeBinding.BlendShapeSubDeformer.Geometry.Name);
                    for (var animationCurveIndex = 0; animationCurveIndex < animationCurveNode.AnimationCurves.Count; animationCurveIndex++)
                    {
                        var nodeAnimationCurves = animationCurveNode.AnimationCurves;
                        if (nodeAnimationCurves != null)
                        {
                            for (var curveIndex = 0; curveIndex < nodeAnimationCurves.Count; curveIndex++)
                            {
                                var animationCurve = nodeAnimationCurves[curveIndex];
                                animationCurve.GetKeyframe(animationTime, out _, out var keyValue, out _, out var keyTangents);
                                UpdateBlendShapeChannels(blendShapeChannels, destinationProperty, animationLayer, keyValue, keyTangents);
                            }
                        }
                    }
                }
            }
            var finalTime = Document.ConvertFromFBXTime(animationTime - animationStart);
            if (finalTime < 0f)
            {
                return;
            }
            ProcessVisibility(animationStack, animatedMatrices, model, finalTime, ref curvesCount, ref visibilityKeyframes, false);
        }

        private void ProcessResampled(
            FBXAnimationStack animationStack,
            FBXAnimationLayer animationLayer,
            FBXAnimatedMatrices animatedMatrices,
            FBXBlendShapeChannels blendShapeChannels,
            FBXModel model,
            long animationTime,
            long animationStart,
            ref int curvesCount,
            ref FBXGenericAnimationCurve localRotationKeyframesX,
            ref FBXGenericAnimationCurve localRotationKeyframesY,
            ref FBXGenericAnimationCurve localRotationKeyframesZ,
            ref FBXGenericAnimationCurve localRotationKeyframesW,
            ref FBXGenericAnimationCurve localPositionKeyframesX,
            ref FBXGenericAnimationCurve localPositionKeyframesY,
            ref FBXGenericAnimationCurve localPositionKeyframesZ,
            ref FBXGenericAnimationCurve localScaleKeyframesX,
            ref FBXGenericAnimationCurve localScaleKeyframesY,
            ref FBXGenericAnimationCurve localScaleKeyframesZ,
            IList<FBXGenericAnimationCurve> blendShapeAnimationCurves)
        {
            var finalTime = Document.ConvertFromFBXTime(animationTime - animationStart);
            if (finalTime < 0f)
            {
                return;
            }
            animatedMatrices.Reset(model);
            if (animationLayer.CurveNodesDictionary != null && animationLayer.CurveNodesDictionary.TryGetValue(model, out var animationCurveNodes))
            {
                for (var animationCurveNodeIndex = 0; animationCurveNodeIndex < animationCurveNodes.Count; animationCurveNodeIndex++)
                {
                    var animationCurveNode = animationCurveNodes[animationCurveNodeIndex];
                    if (animationCurveNode.AnimationCurves == null)
                    {
                        continue;
                    }
                    for (var animationCurveIndex = 0; animationCurveIndex < animationCurveNode.AnimationCurves.Count; animationCurveIndex++)
                    {
                        var animationCurve = animationCurveNode.AnimationCurves[animationCurveIndex];
                        if (animationCurve.KeyTimes.Count == 0 || animationCurve.AnimationCurveNodeBinding.FieldIndex < 0)
                        {
                            continue;
                        }
                        if (animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType != FBXMatrixType.LclRotation &&
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType != FBXMatrixType.LclTranslation &&
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType != FBXMatrixType.LclScaling)
                        {
                            continue;
                        }
                        var value = animationCurve.Evaluate(animationTime, out _);
                        UpdateMatrix(
                            animatedMatrices,
                            animationCurveNode.AnimationCurveModelBinding.PropertyMatrixType,
                            animationCurve.AnimationCurveNodeBinding.FieldIndex,
                            animationLayer,
                            value,
                            null);
                    }
                }
            }
            ProcessMatrices(animatedMatrices, model, out var localPosition, out var localRotation, out var localScale);
            ProcessRotation(
                animationStack,
                animatedMatrices,
                finalTime,
                localRotation,
                ref curvesCount,
                ref localRotationKeyframesX,
                ref localRotationKeyframesY,
                ref localRotationKeyframesZ,
                ref localRotationKeyframesW, true);
            ProcessTranslationAndScale(
                animationStack,
                animatedMatrices,
                finalTime,
                localPosition,
                localScale,
                ref curvesCount,
                ref localPositionKeyframesX,
                ref localPositionKeyframesY,
                ref localPositionKeyframesZ,
                ref localScaleKeyframesX,
                ref localScaleKeyframesY,
                ref localScaleKeyframesZ, true);
            ProcessBlendShapes(animationStack, blendShapeChannels, blendShapeAnimationCurves, finalTime, true);
        }

        private void ProcessBlendShapes(
            FBXAnimationStack animationStack,
            FBXBlendShapeChannels blendShapeChannels,
            IList<FBXGenericAnimationCurve> blendShapeAnimationCurves,
            float finalTime,
            bool resampled)
        {
            foreach (var kvp in blendShapeChannels.FieldIndices)
            {
                if (blendShapeAnimationCurves[kvp.Value] == null)
                {
                    if (Reader.AssetLoaderContext.Options.BlendShapeMapper != null)
                    {
                        var propertyName = Reader.AssetLoaderContext.Options.BlendShapeMapper.MapAnimationCurve(Reader.AssetLoaderContext, kvp.Value);
                        blendShapeAnimationCurves[kvp.Value] = new FBXGenericAnimationCurve(propertyName, Reader.AssetLoaderContext.Options.BlendShapeMapper.AnimationCurveSourceType) { Name = propertyName };
                    }
                    else
                    {
                        blendShapeAnimationCurves[kvp.Value] = new FBXGenericAnimationCurve(kvp.Key, typeof(SkinnedMeshRenderer)) { Name = kvp.Key };
                    }
                }
                var defaultTangent = FBXAnimationCurve.KeyTangent.GetDefault();
                AddKeyToCurve(animationStack, blendShapeAnimationCurves[kvp.Value], finalTime, blendShapeChannels.GetMatrix(kvp.Value), defaultTangent, resampled, false);
            }
        }

        private void ProcessMatrices(FBXMatrices matrices, FBXModel model, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
        {
            matrices.TransformMatrices(Reader.AssetLoaderContext, out localPosition, out localRotation, out localScale);
            Document.ConvertMatrix(ref localPosition, ref localRotation, ref localScale, model);
            if (model.ObjectType == FBXObjectType.Camera && ((FBXCamera)model).LookAtProperty == null || model.ObjectType == FBXObjectType.Light)
            {
                localRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
        }

        private void ProcessRotation(FBXAnimationStack animationStack,
            FBXAnimatedMatrices matrices,
            float finalTime,
            Quaternion localRotation,
            ref int curvesCount,
            ref FBXGenericAnimationCurve localRotationKeyframesX,
            ref FBXGenericAnimationCurve localRotationKeyframesY,
            ref FBXGenericAnimationCurve localRotationKeyframesZ,
            ref FBXGenericAnimationCurve localRotationKeyframesW,
            bool resampled)
        {
            if (matrices.HasMatrix(FBXMatrixType.LclRotation))
            {
                if (localRotationKeyframesX == null)
                {
                    localRotationKeyframesX = new FBXGenericAnimationCurve(Constants.LocalRotationXProperty, typeof(Transform));
                    localRotationKeyframesY = new FBXGenericAnimationCurve(Constants.LocalRotationYProperty, typeof(Transform));
                    localRotationKeyframesZ = new FBXGenericAnimationCurve(Constants.LocalRotationZProperty, typeof(Transform));
                    localRotationKeyframesW = new FBXGenericAnimationCurve(Constants.LocalRotationWProperty, typeof(Transform));
                    curvesCount += 4;
                }
                if (localRotation.IsInvalid())
                {
                    localRotation = Quaternion.identity;
                }
                var defaultTangent = FBXAnimationCurve.KeyTangent.GetDefault();
                AddRotationKeysToCurves(
                    animationStack,
                    finalTime,
                    localRotationKeyframesX,
                    localRotationKeyframesY,
                    localRotationKeyframesZ,
                    localRotationKeyframesW,
                    localRotation,
                    defaultTangent
                );
            }
        }

        private void AddRotationKeysToCurves(
            FBXAnimationStack animationStack,
            float finalTime,
            FBXGenericAnimationCurve localRotationKeyframesX,
            FBXGenericAnimationCurve localRotationKeyframesY,
            FBXGenericAnimationCurve localRotationKeyframesZ,
            FBXGenericAnimationCurve localRotationKeyframesW,
            Quaternion localRotation,
            FBXAnimationCurve.KeyTangent tangentsAndWeights)
        {
            var keyframeX = new Keyframe(finalTime, localRotation.x, tangentsAndWeights.SlopeLeft, tangentsAndWeights.SlopeRight, tangentsAndWeights.WeightLeft, tangentsAndWeights.WeightRight);
            keyframeX.weightedMode = WeightedMode.None;

            var keyframeY = new Keyframe(finalTime, localRotation.y, tangentsAndWeights.SlopeLeft, tangentsAndWeights.SlopeRight, tangentsAndWeights.WeightLeft, tangentsAndWeights.WeightRight);
            keyframeY.weightedMode = WeightedMode.None;

            var keyframeZ = new Keyframe(finalTime, localRotation.z, tangentsAndWeights.SlopeLeft, tangentsAndWeights.SlopeRight, tangentsAndWeights.WeightLeft, tangentsAndWeights.WeightRight);
            keyframeZ.weightedMode = WeightedMode.None;

            var keyframeW = new Keyframe(finalTime, localRotation.w, tangentsAndWeights.SlopeLeft, tangentsAndWeights.SlopeRight, tangentsAndWeights.WeightLeft, tangentsAndWeights.WeightRight);
            keyframeW.weightedMode = WeightedMode.None;

            AnimationCurveExtensions.AddQuaternionKeyframe(
                Reader.AssetLoaderContext,
                localRotationKeyframesX.AnimationCurve,
                localRotationKeyframesY.AnimationCurve,
                localRotationKeyframesZ.AnimationCurve,
                localRotationKeyframesW.AnimationCurve,
                keyframeX,
                keyframeY,
                keyframeZ,
                keyframeW);
        }

        private void ProcessVisibility(FBXAnimationStack animationStack,
            FBXAnimatedMatrices matrices,
            FBXModel model,
            float finalTime,
            ref int curvesCount,
            ref FBXGenericAnimationCurve visibilityKeyframes,
            bool resampled)
        {
            if (Reader.AssetLoaderContext.Options.ImportVisibility && model.GeometryGroup != null && matrices.HasMatrix(FBXMatrixType.Visibility))
            {
                if (visibilityKeyframes == null)
                {
                    visibilityKeyframes = new FBXGenericAnimationCurve(Constants.EnabledProperty, typeof(Renderer));
                    curvesCount++;
                }
                var visibility = matrices.GetField(FBXMatrixType.Visibility, 0);
                AddKeyToCurve(animationStack, visibilityKeyframes, finalTime, visibility, matrices.GetTangentsAndWeights(FBXMatrixType.Visibility, 0), resampled, false);
            }
        }

        private void ProcessTranslationAndScale(FBXAnimationStack animationStack,
            FBXAnimatedMatrices matrices,
            float finalTime,
            Vector3 localPosition,
            Vector3 localScale,
            ref int curvesCount,
            ref FBXGenericAnimationCurve localPositionKeyframesX,
            ref FBXGenericAnimationCurve localPositionKeyframesY,
            ref FBXGenericAnimationCurve localPositionKeyframesZ,
            ref FBXGenericAnimationCurve localScaleKeyframesX,
            ref FBXGenericAnimationCurve localScaleKeyframesY,
            ref FBXGenericAnimationCurve localScaleKeyframesZ,
            bool resampled)
        {
            var defaultTangent = FBXAnimationCurve.KeyTangent.GetDefault();
            if (matrices.HasMatrix(FBXMatrixType.LclTranslation))
            {
                if (localPositionKeyframesX == null)
                {
                    localPositionKeyframesX = new FBXGenericAnimationCurve(Constants.LocalPositionXProperty, typeof(Transform));
                    localPositionKeyframesY = new FBXGenericAnimationCurve(Constants.LocalPositionYProperty, typeof(Transform));
                    localPositionKeyframesZ = new FBXGenericAnimationCurve(Constants.LocalPositionZProperty, typeof(Transform));
                    curvesCount += 3;
                }
                AddKeyToCurve(animationStack, localPositionKeyframesX, finalTime, localPosition.x, defaultTangent, resampled, false);
                AddKeyToCurve(animationStack, localPositionKeyframesY, finalTime, localPosition.y, defaultTangent, resampled, false);
                AddKeyToCurve(animationStack, localPositionKeyframesZ, finalTime, localPosition.z, defaultTangent, resampled, false);
            }
            if (matrices.HasMatrix(FBXMatrixType.LclScaling))
            {
                if (localScaleKeyframesX == null)
                {
                    localScaleKeyframesX = new FBXGenericAnimationCurve(Constants.LocalScaleXProperty, typeof(Transform));
                    localScaleKeyframesY = new FBXGenericAnimationCurve(Constants.LocalScaleYProperty, typeof(Transform));
                    localScaleKeyframesZ = new FBXGenericAnimationCurve(Constants.LocalScaleZProperty, typeof(Transform));
                    curvesCount += 3;
                }
                AddKeyToCurve(animationStack, localScaleKeyframesX, finalTime, localScale.x, defaultTangent, resampled, true);
                AddKeyToCurve(animationStack, localScaleKeyframesY, finalTime, localScale.y, defaultTangent, resampled, true);
                AddKeyToCurve(animationStack, localScaleKeyframesZ, finalTime, localScale.z, defaultTangent, resampled, true);
            }
        }

        private void AddKeyToCurve(
            FBXAnimationStack animationStack,
            FBXGenericAnimationCurve animationCurve,
            float finalTime,
            float finalValue,
            FBXAnimationCurve.KeyTangent tangentsAndWeights,
            bool resampled,
            bool scale)
        {
            if (!resampled)
            {
                tangentsAndWeights.SlopeLeft = Document.Scale(
                    tangentsAndWeights.SlopeLeft,
                    Reader.AssetLoaderContext.Options.UseFileScale,
                    Reader.AssetLoaderContext.Options.ScaleFactor);
                tangentsAndWeights.SlopeRight = Document.Scale(
                    tangentsAndWeights.SlopeRight,
                    Reader.AssetLoaderContext.Options.UseFileScale,
                    Reader.AssetLoaderContext.Options.ScaleFactor);
            }
            var keyframe = new Keyframe(
                finalTime,
                finalValue,
                tangentsAndWeights.SlopeLeft,
                tangentsAndWeights.SlopeRight,
                tangentsAndWeights.WeightLeft,
                tangentsAndWeights.WeightRight);
            keyframe.weightedMode = WeightedMode.None;
            if (resampled)
            {
                animationCurve.AnimationCurve.AddVectorKeyframe(Reader.AssetLoaderContext, keyframe, scale);
            }
            else
            {
                animationCurve.AnimationCurve.AddKey(keyframe);
            }
        }

        private FBXAnimationLayer ProcessAnimationLayer(FBXNode node, long objectId, string name, string objectClass)
        {
            var layer = new FBXAnimationLayer(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AnimationLayers.Add(layer);
            }
            var properties = node?.GetNodeByName(PropertiesName);
            if (properties != null && properties.HasSubNodes)
            {
                foreach (var property in properties)
                {
                    var propertyName = property.Properties.GetStringHashValue(0);
                    switch (propertyName)
                    {
                        case _BlendMode_token:
                            layer.BlendMode = (FBXAnimationLayerBlendMode)property.Properties.GetIntValue(4);
                            break;
                        case _RotationAccumulationMode_token:
                            layer.RotationAccumulationMode = (FBXRotationAccumulationOrder)property.Properties.GetIntValue(4);
                            break;
                        case _ScaleAccumulationMode_token:
                            layer.ScaleAccumulationMode = (FBXScaleAccumulationMode)property.Properties.GetIntValue(4);
                            break;
                        case _Weight_token:
                            layer.Weight = property.Properties.GetFloatValue(4);
                            break;
                        case _Mute_token:
                            layer.Mute = property.Properties.GetBoolValue(4);
                            break;
                        case _Solo_token:
                            layer.Solo = property.Properties.GetBoolValue(4);
                            break;
                        case _Lock_token:
                            layer.Lock = property.Properties.GetBoolValue(4);
                            break;
                        case _mLayerID_token:
                            layer.LayerId = property.Properties.GetIntValue(4);
                            break;
                        case _BlendModeByPass_token:
                        case _MutedForParent_token:
                        case _LockedByParent_token:
                        case _MutedForSolo_token:
                        case _MultiTake_token:
                            break;
                    }
                }
            }
            return layer;
        }

        private FBXAnimationStack ProcessAnimationStack(FBXNode node, long objectId, string name, string objectClass)
        {
            var animationStack = new FBXAnimationStack(Document, name, objectId, objectClass);
            if (objectId == -1)
            {
                node = node?.GetNodeByName(PropertiesTemplateName);
            }
            else
            {
                Document.AllAnimations.Add(animationStack);
            }
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
                            case _Description_token:
                                animationStack.Description = property.Properties.GetStringValue(4, false);
                                break;
                            case _LocalStart_token:
                                animationStack.LocalStart = property.Properties.GetLongValue(4);
                                break;
                            case _LocalStop_token:
                                animationStack.LocalStop = property.Properties.GetLongValue(4);
                                break;
                            case _ReferenceStart_token:
                                animationStack.ReferenceStart = property.Properties.GetLongValue(4);
                                break;
                            case _ReferenceStop_token:
                                animationStack.ReferenceStop = property.Properties.GetLongValue(4);
                                break;
                        }
                    }
                }
            }
            return animationStack;
        }

        private static void ModelAnimationSetup(
            FBXAnimationStack animationStack,
            FBXModel model,
            FBXAnimationLayer animationLayer,
            FBXAnimationCurveNode animationCurveNode,
            FBXAnimationCurveModelBinding animationCurveModelBinding,
            FBXAnimationCurveBlendShapeBinding animationCurveBlendShapeBinding
            )
        {
            if (model != null)
            {
                if (animationStack.AnimatedModels == null)
                {
                    animationStack.AnimatedModels = new HashSet<FBXModel>();
                    animationStack.AnimatedTimes = new HashSet<long>();
                }
                animationStack.AnimatedModels.Add(model);
                if (animationCurveNode.AnimationCurves != null)
                {
                    for (var animationCurveIndex = 0; animationCurveIndex < animationCurveNode.AnimationCurves.Count; animationCurveIndex++)
                    {
                        var animationCurve = animationCurveNode.AnimationCurves[animationCurveIndex];
                        animationCurve.Initialize();
                        animationStack.AnimatedTimes.UnionWith(animationCurve.KeyTimes);
                    }
                }
                if (animationCurveModelBinding != null)
                {
                    if (animationLayer.CurveNodesDictionary == null)
                    {
                        animationLayer.CurveNodesDictionary = new Dictionary<FBXModel, IList<FBXAnimationCurveNode>>(animationLayer.CurveNodesCount);
                    }
                    if (!animationLayer.CurveNodesDictionary.TryGetValue(model, out var animationCurveNodes))
                    {
                        animationCurveNodes = new List<FBXAnimationCurveNode>();
                        animationLayer.CurveNodesDictionary.Add(model, animationCurveNodes);
                    }
                    animationCurveNodes.Add(animationCurveNode);
                }
                else if (animationCurveBlendShapeBinding != null)
                {
                    if (animationLayer.GeometryCurveNodesDictionary == null)
                    {
                        animationLayer.GeometryCurveNodesDictionary = new Dictionary<FBXModel, IList<FBXAnimationCurveNode>>(animationLayer.CurveNodesCount);
                    }
                    if (!animationLayer.GeometryCurveNodesDictionary.TryGetValue(model, out var animationCurveNodes))
                    {
                        animationCurveNodes = new List<FBXAnimationCurveNode>();
                        animationLayer.GeometryCurveNodesDictionary.Add(model, animationCurveNodes);
                    }
                    animationCurveNodes.Add(animationCurveNode);
                }
            }
        }

        private void PostProcessAnimationCurveNodes()
        {
            if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None)
            {
                for (var i = 0; i < Document.AnimationCurveNodes.Count; i++)
                {
                    var animationCurveNode = Document.AnimationCurveNodes[i];
                    var animationLayer = animationCurveNode.AnimationLayer;
                    if (animationLayer != null)
                    {
                        for (var a = 0; a < animationLayer.AnimationStacks.Count; a++)
                        {
                            var animationStack = animationLayer.AnimationStacks[a];
                            animationStack.AnimationCurveBindingsCount++;
                        }
                        for (var a = 0; a < animationLayer.AnimationStacks.Count; a++)
                        {
                            var animationStack = animationLayer.AnimationStacks[a];
                            var animationCurveModelBinding = animationCurveNode.AnimationCurveModelBinding;
                            if (animationCurveModelBinding != null)
                            {
                                var model = animationCurveModelBinding.Model;
                                if (model != null)
                                {
                                    ModelAnimationSetup(animationStack,
                                        model,
                                        animationLayer,
                                        animationCurveNode,
                                        animationCurveModelBinding,
                                        null);
                                }
                            }
                            var animationCurveBlendShapeBinding = animationCurveNode.AnimationCurveBlendShapeBinding;
                            if (animationCurveBlendShapeBinding != null)
                            {
                                var model = animationCurveBlendShapeBinding.BlendShapeSubDeformer.BaseDeformer?.Geometry?.Model;
                                if (model != null)
                                {
                                    ModelAnimationSetup(animationStack,
                                        model,
                                        animationLayer,
                                        animationCurveNode,
                                        animationCurveModelBinding,
                                        animationCurveBlendShapeBinding);
                                }
                            }
                        }
                    }
                }
            }
            if (Reader.AssetLoaderContext.Options.AnimationType != AnimationType.None || Reader.AssetLoaderContext.Options.ImportBlendShapes)
            {
                for (var i = 0; i < Document.SubDeformers.Count; i++)
                {
                    var subDeformer = Document.SubDeformers[i];
                    if (subDeformer is FBXBlendShapeSubDeformer)
                    {
                        if (subDeformer.BaseDeformer.Geometry != null)
                        {
                            subDeformer.BaseDeformer.Geometry.BlendShapeGeometryBindingsCount++;
                        }
                    }
                    else
                    {
                        if (subDeformer?.BaseDeformer?.Geometry?.Model != null)
                        {
                            subDeformer.BaseDeformer.Geometry.Model.BonesCount++;
                        }
                    }
                }
            }
        }
    }
}