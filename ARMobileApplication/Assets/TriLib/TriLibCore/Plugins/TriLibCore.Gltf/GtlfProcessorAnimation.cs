using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private GltfAnimation ConvertAnimation(int a)
        {
            var animation = animations.GetArrayValueAtIndex(a);
            var animationChannels = animation.GetChildWithKey(_channels_token);
            var animationSamplers = animation.GetChildWithKey(_samplers_token);
            var gltfAnimation = new GltfAnimation
            {
                Name = animation.GetChildValueAsString(_name_token, _temporaryString),
                AnimationCurveBindings = new List<IAnimationCurveBinding>(animationChannels.Count),
                FrameRate = 60f
            };
            for (var c = 0; c < animationChannels.Count; c++)
            {
                var animationChannel = animationChannels.GetArrayValueAtIndex(c);
                if (animationChannel.TryGetChildWithKey(_target_token, out var channelTarget))
                {
                    var animationCurveBinding = new GltfAnimationCurveBinding { Model = _models[channelTarget.GetChildValueAsInt(_node_token, _temporaryString, 0)] };
                    var targetPath = ConvertTargetPath(channelTarget.GetChildValueAsString(_path_token, _temporaryString));
                    var animationSampler = animationSamplers.GetArrayValueAtIndex(animationChannel.GetChildValueAsInt(_sampler_token, _temporaryString, 0));
                    var samplerInterpolation = ConvertSamplerInterpolation(animationSampler.GetChildValueAsString(_interpolation_token, _temporaryString));
                    TangentMode tangentMode;
                    switch (samplerInterpolation)
                    {
                        case LINEAR:
                            tangentMode = TangentMode.Linear;
                            break;
                        case STEP:
                            tangentMode = TangentMode.Stepped;
                            break;
                        default:
                            tangentMode = TangentMode.Smooth;
                            break;
                    }

                    var timeAccessor = accessors.GetArrayValueAtIndex(animationSampler.GetChildValueAsInt(_input_token, _temporaryString, 0));
                    var valuesAccessor = accessors.GetArrayValueAtIndex(animationSampler.GetChildValueAsInt(_output_token, _temporaryString, 0));
                    var time = ConvertAccessorDataFloat(timeAccessor);
                    switch (targetPath)
                    {
                        case TRANSLATION:
                        case SCALE:
                            {
                                var values = ConvertAccessorDataFloat(valuesAccessor, true);
                                var xValues = new AnimationCurve();
                                var yValues = new AnimationCurve();
                                var zValues = new AnimationCurve();
                                var stepSize = samplerInterpolation == CUBICSPLINE ? 9 : 3;
                                var valuesGroupCount = values.Count / stepSize;
                                for (var i = 0; i < valuesGroupCount; i++)
                                {
                                    float x;
                                    float xIn;
                                    float xOut;
                                    float y;
                                    float yIn;
                                    float yOut;
                                    float z;
                                    float zIn;
                                    float zOut;
                                    var baseIndex = i * stepSize;
                                    if (samplerInterpolation == CUBICSPLINE)
                                    {
                                        xIn = values[baseIndex];
                                        x = values[baseIndex + 3];
                                        xOut = values[baseIndex + 6];
                                        yIn = values[baseIndex + 1];
                                        y = values[baseIndex + 3 + 1];
                                        yOut = values[baseIndex + 6 + 1];
                                        zIn = values[baseIndex + 2];
                                        z = values[baseIndex + 3 + 2];
                                        zOut = values[baseIndex + 6 + 2];
                                    }
                                    else
                                    {
                                        xIn = 0f;
                                        x = values[baseIndex];
                                        xOut = 0f;
                                        yIn = 0f;
                                        y = values[baseIndex + 1];
                                        yOut = 0f;
                                        zIn = 0f;
                                        z = values[baseIndex + 2];
                                        zOut = 0f;
                                    }

                                    if (targetPath == TRANSLATION)
                                    {
                                        x *= _reader.AssetLoaderContext.Options.ScaleFactor;
                                        y *= _reader.AssetLoaderContext.Options.ScaleFactor;
                                        z *= _reader.AssetLoaderContext.Options.ScaleFactor;
                                        x = -x;
                                        xIn = -xIn;
                                        xOut = -xOut;
                                    }

                                    var t = time[i];
                                    var xValue = new Keyframe();
                                    xValue.value = x;
                                    xValue.inTangent = xIn;
                                    xValue.outTangent = xOut;
                                    xValue.time = t;
                                    var yValue = new Keyframe();
                                    yValue.value = y;
                                    yValue.inTangent = yIn;
                                    yValue.outTangent = yOut;
                                    yValue.time = t;
                                    var zValue = new Keyframe();
                                    zValue.value = z;
                                    zValue.inTangent = zIn;
                                    zValue.outTangent = zOut;
                                    zValue.time = t;
                                    xValues.AddVectorKeyframe(_reader.AssetLoaderContext, xValue, targetPath == SCALE);
                                    yValues.AddVectorKeyframe(_reader.AssetLoaderContext, yValue, targetPath == SCALE);
                                    zValues.AddVectorKeyframe(_reader.AssetLoaderContext, zValue, targetPath == SCALE);
                                }

                                GltfAnimationCurve animationCurveX;
                                GltfAnimationCurve animationCurveY;
                                GltfAnimationCurve animationCurveZ;
                                if (targetPath == TRANSLATION)
                                {
                                    animationCurveX = new GltfAnimationCurve { Property = Constants.LocalPositionXProperty, AnimationCurve = xValues, TangentMode = tangentMode };
                                    animationCurveY = new GltfAnimationCurve { Property = Constants.LocalPositionYProperty, AnimationCurve = yValues, TangentMode = tangentMode };
                                    animationCurveZ = new GltfAnimationCurve { Property = Constants.LocalPositionZProperty, AnimationCurve = zValues, TangentMode = tangentMode };
                                }
                                else
                                {
                                    animationCurveX = new GltfAnimationCurve { Property = Constants.LocalScaleXProperty, AnimationCurve = xValues, TangentMode = tangentMode };
                                    animationCurveY = new GltfAnimationCurve { Property = Constants.LocalScaleYProperty, AnimationCurve = yValues, TangentMode = tangentMode };
                                    animationCurveZ = new GltfAnimationCurve { Property = Constants.LocalScaleZProperty, AnimationCurve = zValues, TangentMode = tangentMode };
                                }

                                AddCurve(animationCurveBinding, animationCurveX);
                                AddCurve(animationCurveBinding, animationCurveY);
                                AddCurve(animationCurveBinding, animationCurveZ);
                                break;
                            }
                        case ROTATION:
                            {
                                var values = ConvertAccessorDataFloat(valuesAccessor, true);

                                var xValues = new AnimationCurve();
                                var yValues = new AnimationCurve();
                                var zValues = new AnimationCurve();
                                var wValues = new AnimationCurve();
                                var stepSize = samplerInterpolation == CUBICSPLINE ? 12 : 4;
                                var valuesGroupCount = values.Count / stepSize;
                                for (var i = 0; i < valuesGroupCount; i++)
                                {
                                    float x;
                                    float xIn;
                                    float xOut;
                                    float y;
                                    float yIn;
                                    float yOut;
                                    float z;
                                    float zIn;
                                    float zOut;
                                    float w;
                                    float wIn;
                                    float wOut;
                                    var baseIndex = i * stepSize;
                                    if (samplerInterpolation == CUBICSPLINE)
                                    {
                                        xIn = values[baseIndex];
                                        x = values[baseIndex + 4];
                                        xOut = values[baseIndex + 8];
                                        yIn = values[baseIndex + 1];
                                        y = values[baseIndex + 4 + 1];
                                        yOut = values[baseIndex + 8 + 1];
                                        zIn = values[baseIndex + 2];
                                        z = values[baseIndex + 4 + 2];
                                        zOut = values[baseIndex + 8 + 2];
                                        wIn = values[baseIndex + 3];
                                        w = values[baseIndex + 4 + 3];
                                        wOut = values[baseIndex + 8 + 3];
                                    }
                                    else
                                    {
                                        xIn = 0f;

                                        x = values[baseIndex];
                                        xOut = 0f;
                                        yIn = 0f;
                                        y = values[baseIndex + 1];
                                        yOut = 0f;
                                        zIn = 0f;
                                        z = values[baseIndex + 2];
                                        zOut = 0f;
                                        wIn = 0f;
                                        w = values[baseIndex + 3];
                                        wOut = 0f;
                                    }

                                    y = -y;
                                    yIn = -yIn;
                                    yOut = -yOut;
                                    z = -z;
                                    zIn = -zIn;
                                    zOut = -zOut;

                                    var t = time[i];

                                    if (animationCurveBinding.Model is ICamera || animationCurveBinding.Model is ILight)
                                    {
                                        var rotation = new Quaternion(x, y, z, w) * Quaternion.Euler(0f, 180f, 0f);
                                        x = rotation.x;
                                        y = rotation.y;
                                        z = rotation.z;
                                        w = rotation.w;
                                    }

                                    var xValue = new Keyframe();
                                    xValue.value = x;
                                    xValue.inTangent = xIn;
                                    xValue.outTangent = xOut;
                                    xValue.time = t;
                                    var yValue = new Keyframe();
                                    yValue.value = y;
                                    yValue.inTangent = yIn;
                                    yValue.outTangent = yOut;
                                    yValue.time = t;
                                    var zValue = new Keyframe();
                                    zValue.value = z;
                                    zValue.inTangent = zIn;
                                    zValue.outTangent = zOut;
                                    zValue.time = t;
                                    var wValue = new Keyframe();
                                    wValue.value = w;
                                    wValue.inTangent = wIn;
                                    wValue.outTangent = wOut;
                                    wValue.time = t;

                                    AnimationCurveExtensions.AddQuaternionKeyframe(
                                        _reader.AssetLoaderContext,
                                        xValues,
                                        yValues,
                                        zValues,
                                        wValues,
                                        xValue,
                                        yValue,
                                        zValue,
                                        wValue);
                                }

                                var animationCurveX = new GltfAnimationCurve { Property = Constants.LocalRotationXProperty, AnimationCurve = xValues, TangentMode = tangentMode };
                                var animationCurveY = new GltfAnimationCurve { Property = Constants.LocalRotationYProperty, AnimationCurve = yValues, TangentMode = tangentMode };
                                var animationCurveZ = new GltfAnimationCurve { Property = Constants.LocalRotationZProperty, AnimationCurve = zValues, TangentMode = tangentMode };
                                var animationCurveW = new GltfAnimationCurve { Property = Constants.LocalRotationWProperty, AnimationCurve = wValues, TangentMode = tangentMode };
                                AddCurve(animationCurveBinding, animationCurveX);
                                AddCurve(animationCurveBinding, animationCurveY);
                                AddCurve(animationCurveBinding, animationCurveZ);
                                AddCurve(animationCurveBinding, animationCurveW);
                                break;
                            }
                        case WEIGHTS when _reader.AssetLoaderContext.Options.ImportBlendShapes:
                            {
                                var blendShapeGeometryBindings = animationCurveBinding.Model.GeometryGroup.BlendShapeKeys;
                                var values = ConvertAccessorDataFloat(valuesAccessor, true);


                                var stepSize = samplerInterpolation == CUBICSPLINE ? blendShapeGeometryBindings.Count * 3 : blendShapeGeometryBindings.Count;
                                var valuesGroupCount = values.Count / stepSize;

                                var curves = new List<GltfAnimationCurve>(valuesGroupCount * blendShapeGeometryBindings.Count);

                                for (var i = 0; i < valuesGroupCount; i++)
                                {
                                    for (var b = 0; b < blendShapeGeometryBindings.Count; b++)
                                    {
                                        float x;
                                        float xIn;
                                        float xOut;
                                        var baseIndex = (i * stepSize) + b;
                                        if (samplerInterpolation == CUBICSPLINE)
                                        {
                                            xIn = values[baseIndex];
                                            x = values[baseIndex + 1];
                                            xOut = values[baseIndex + 2];
                                        }
                                        else
                                        {
                                            xIn = 0f;
                                            x = values[baseIndex];
                                            xOut = 0f;
                                        }

                                        var t = time[i];
                                        AnimationCurve keyframes;
                                        if (curves.Count <= b)
                                        {
                                            keyframes = new AnimationCurve();
                                            var blendShapeGeometryBinding = blendShapeGeometryBindings[b];
                                            GltfAnimationCurve curve;
                                            if (_reader.AssetLoaderContext.Options.BlendShapeMapper != null)
                                            {
                                                var property = _reader.AssetLoaderContext.Options.BlendShapeMapper.MapAnimationCurve(_reader.AssetLoaderContext, b);
                                                curve = new GltfAnimationCurve { TangentMode = tangentMode, AnimationCurve = keyframes, Property = property, AnimatedType = _reader.AssetLoaderContext.Options.BlendShapeMapper.AnimationCurveSourceType };
                                            }
                                            else
                                            {
                                                var property = string.Format(Constants.BlendShapePathFormat, blendShapeGeometryBinding.Name);
                                                curve = new GltfAnimationCurve { TangentMode = tangentMode, AnimationCurve = keyframes, Property = property, AnimatedType = typeof(SkinnedMeshRenderer) };
                                            }
                                            curves.Add(curve);
                                        }
                                        else
                                        {
                                            keyframes = curves[b].AnimationCurve;
                                        }

                                        var keyframe = new Keyframe();
                                        keyframe.value = x;
                                        keyframe.inTangent = xIn;
                                        keyframe.outTangent = xOut;
                                        keyframe.time = t;
                                        keyframes.AddKey(keyframe);
                                    }
                                }

                                for (var i = 0; i < curves.Count; i++)
                                {
                                    var t = curves[i];
                                    AddCurve(animationCurveBinding, t);
                                }

                                break;
                            }
                        default:
                            {
                                ConvertAccessorDataFloat(valuesAccessor);
                                if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                                {
                                    Debug.Log("Found unknown animation channel.");
                                }

                                break;
                            }
                    }
                    gltfAnimation.AnimationCurveBindings.Add(animationCurveBinding);
                }
            }

            return gltfAnimation;
        }

        private int ConvertTargetPath(string targetPath)
        {
            switch (targetPath)
            {
                case "translation":
                    return TRANSLATION;
                case "rotation":
                    return ROTATION;
                case "scale":
                    return SCALE;
                case "weights":
                    return WEIGHTS;
            }

            return 0;
        }

        private int ConvertSamplerInterpolation(string samplerInterpolation)
        {
            switch (samplerInterpolation)
            {
                case "LINEAR":
                    return LINEAR;
                case "STEP":
                    return STEP;
                case "CUBICSPLINE":
                    return CUBICSPLINE;
            }

            return 0;
        }

        private static void AddCurve(GltfAnimationCurveBinding animationCurveBinding, GltfAnimationCurve animationCurve)
        {
            animationCurve.AnimationCurve.SetTangents(animationCurve.TangentMode);
            animationCurveBinding.AnimationCurves.Add(animationCurve);
        }
    }
}