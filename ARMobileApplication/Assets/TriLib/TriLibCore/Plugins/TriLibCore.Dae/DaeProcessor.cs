using LibTessDotNet;
using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;
using TriLibCore.Dae.Reader;
using TriLibCore.Dae.Schema;
using TriLibCore.Extensions;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.Textures;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeProcessor
    {
        private const string DefaultName = "Default";
        private static readonly char[] SplitChars = new []{ ' ', '\t', '\r', '\n' };
        private readonly List<IAnimation> _animations = new List<IAnimation>();
        private readonly Dictionary<string, IGeometryGroup> _geometries = new Dictionary<string, IGeometryGroup>();
        private readonly Dictionary<string, DaeTexture> _images = new Dictionary<string, DaeTexture>();
        private readonly Dictionary<int, DaeMaterial> _materials = new Dictionary<int, DaeMaterial>();
        private readonly List<DaeModel> _models = new List<DaeModel>();
        private readonly Dictionary<string, DaeModel> _modelsById = new Dictionary<string, DaeModel>();
        private readonly Dictionary<string, DaeModel> _modelsBySid = new Dictionary<string, DaeModel>();
        private readonly Dictionary<string, List<float>> _processedFloatLists = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, List<Matrix4x4>> _processedMatrixLists = new Dictionary<string, List<Matrix4x4>>();
        private readonly Dictionary<string, List<string>> _processedStringLists = new Dictionary<string, List<string>>();
        private readonly Dictionary<string, List<Vector2>> _processedVector2Lists = new Dictionary<string, List<Vector2>>();
        private readonly Dictionary<string, List<Vector3>> _processedVector3Lists = new Dictionary<string, List<Vector3>>();
        private COLLADA _collada;
        private Matrix4x4 _documentMatrix;
        private Dictionary<string, accessor> _processedAccessors = new Dictionary<string, accessor>();
        private DaeReader _reader;
        private DaeRootModel _rootModel;
        private float _scaleFactor;
        private UpAxisType _upAxis;
        public bool IsRightHanded { get; private set; }

        public void ApplyDocumentOrientation(ref Vector3 localPosition, ref Quaternion localRotation)
        {
            localPosition = _documentMatrix.MultiplyVector(localPosition);
            localRotation = _documentMatrix.rotation * localRotation;
        }

        public void ConvertMatrix(
          ref Vector3 t,
          ref Quaternion r,
          ref Vector3 s,
          IModel model = null,
          bool applyScale = true)
        {
            if (model?.Parent?.Parent == _rootModel)
            {
                ApplyDocumentOrientation(ref t, ref r);
            }
            t = ConvertVector(t, applyScale);
            r = ConvertRotation(r);
        }

        public Quaternion ConvertRotation(Quaternion rotation)
        {
            if (IsRightHanded)
            {
                rotation = RightHandToLeftHandConverter.ConvertRotation(rotation);
            }
            return rotation;
        }

        public Vector3 ConvertVector(Vector3 vertex, bool applyScale = false, bool applyOrientation = true)
        {
            if (applyScale)
            {
                if (_reader.AssetLoaderContext.Options.UseFileScale)
                {
                    vertex *= _scaleFactor;
                }
                vertex *= _reader.AssetLoaderContext.Options.ScaleFactor;
            }
            if (applyOrientation && IsRightHanded)
            {
                vertex = RightHandToLeftHandConverter.ConvertVector(vertex);
            }
            return vertex;
        }

        public Matrix4x4 DecomposeAndConvertMatrix(Matrix4x4 matrix, IModel model)
        {
            matrix.Decompose(out var t, out var r, out var s);
            ConvertMatrix(ref t, ref r, ref s, model);
            matrix = Matrix4x4.TRS(t, r, s);
            return matrix;
        }
        public IRootModel Process(DaeReader daeReader, Stream stream)
        {
            _reader = daeReader;
            _collada = (COLLADA)new XmlSerializer(typeof(COLLADA)).Deserialize(stream);
            return ProcessScene();
        }

        public void SetupCoordSystem()
        {
            int upAxis;
            int frontAxis;
            float frontAxisSign;
            switch (_upAxis)
            {
                case UpAxisType.Y_UP:
                    upAxis = 1;
                    frontAxis = 2;
                    frontAxisSign = 1f;
                    break;
                case UpAxisType.Z_UP:
                    upAxis = 2;
                    frontAxis = 1;
                    frontAxisSign = -1f;
                    break;
                default:
                    upAxis = 0;
                    frontAxis = 2;
                    frontAxisSign = 1f;
                    break;
            }
            var upVec = MathUtils.Axis[upAxis];
            var frontVec = MathUtils.Axis[frontAxis] * frontAxisSign;
            var rightVec = Vector3.Cross(upVec, frontVec);
            _documentMatrix = Matrix4x4.identity;
            _documentMatrix.SetColumn(0, rightVec);
            _documentMatrix.SetColumn(1, upVec);
            _documentMatrix.SetColumn(2, frontVec);
            _documentMatrix = _documentMatrix.inverse;
            IsRightHanded = Vector3.Dot(rightVec, rightVec) > 0.0f;
        }

        private static DaeGeometryGroup CreateGeometryGroup(
          bool hasNormal,
          bool hasTangent,
          bool hasColor,
          bool hasUV0,
          bool hasUV1,
          bool hasUV2,
          bool hasUV3,
          bool hasSkin)
        {
            return new DaeGeometryGroup()
            {
                HasNormals = hasNormal,
                HasTangents = hasTangent,
                HasColors = hasColor,
                HasUv1 = hasUV0,
                HasUv2 = hasUV1,
                HasUv3 = hasUV2,
                HasUv4 = hasUV3,
                HasSkin = hasSkin
            };
        }

        private static Color ProcessColor(double[] values)
        {
            return new Color((float)(DaeReader.DaeConversionPrecision * values[0]),
                (float)(DaeReader.DaeConversionPrecision * values[1]),
                (float)(DaeReader.DaeConversionPrecision * values[2]),
                (float)(DaeReader.DaeConversionPrecision * values[3]));
        }

        private static int[] SplitToInt(string data, bool reverse, int biggestOffset)
        {
            var stringData = data.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
            var intData = new int[stringData.Length];
            if (reverse)
            {
                for (var i = 0; i < stringData.Length; i += biggestOffset)
                {
                    for (var j = 0; j < biggestOffset; j++)
                    {
                        intData[i + j] = int.Parse(stringData[(stringData.Length - i - biggestOffset) + j]);
                    }
                }
            }
            else
            {
                for (var i = 0; i < stringData.Length; i++)
                {
                    intData[i] = int.Parse(stringData[i]);
                }
            }
            return intData;
        }

        private void AddKeyToCurve(
          DaeGenericAnimationCurve animationCurve,
          float finalTime,
          float finalValue,
          bool scale)
        {
            var keyframe = new Keyframe(finalTime, finalValue);
            animationCurve.AnimationCurve.AddVectorKeyframe(_reader.AssetLoaderContext, keyframe, scale);
        }

        private void AddRotationKeysToCurves(
          float finalTime,
          DaeGenericAnimationCurve localRotationKeyframesX,
          DaeGenericAnimationCurve localRotationKeyframesY,
          DaeGenericAnimationCurve localRotationKeyframesZ,
          DaeGenericAnimationCurve localRotationKeyframesW,
          Quaternion localRotation)
        {
            var keyframeX = new Keyframe(finalTime, localRotation.x);
            var keyframeY = new Keyframe(finalTime, localRotation.y);
            var keyframeZ = new Keyframe(finalTime, localRotation.z);
            var keyframeW = new Keyframe(finalTime, localRotation.w);
            AnimationCurveExtensions.AddQuaternionKeyframe(
                _reader.AssetLoaderContext,
                localRotationKeyframesX.AnimationCurve,
                localRotationKeyframesY.AnimationCurve,
                localRotationKeyframesZ.AnimationCurve,
                localRotationKeyframesW.AnimationCurve,
                keyframeX,
                keyframeY,
                keyframeZ,
                keyframeW);
        }

        private void AddVertex(CommonGeometry daeGeometry,
          IList<int> primitives,
          int vertexIndex,
          int normalIndex,
          int texCoordIndex,
          bool hasVertices,
          bool hasNormals,
          bool hasTexCoords,
          List<Vector3> verticesList,
          List<Vector3> normalsList,
          List<Vector2> texCoordsList,
          bool hasSkin)
        {
            var primitive = primitives[vertexIndex];
            daeGeometry.AddVertex(_reader.AssetLoaderContext,
                primitive,
                hasVertices ? ListUtils.FixIndex(primitive, verticesList) : new Vector3(),
                hasNormals ? ListUtils.FixIndex(primitives[normalIndex], normalsList) : new Vector3(),
                new Vector4(),
                new Color(),
                hasTexCoords ? ListUtils.FixIndex(primitives[texCoordIndex], texCoordsList) : new Vector2());
        }

        private CommonGeometry GetGeometry(IGeometryGroup geometryGroup, string materialName, Dictionary<string, int> bindings)
        {
            materialName ??= DefaultName;
            var materialIndex = bindings.GetValueOrDefault(materialName, -1);
            return geometryGroup.GetGeometry<CommonGeometry>(_reader.AssetLoaderContext, materialIndex, false, true); //todo: blend-shapes
        }

        private DaeMaterial ParseMaterial(material mat)
        {
            var effects = _collada.Items.GetItemWithType<library_effects>();
            if (effects == null || mat.instance_effect == null)
            {
                return null;
            }
            var effect = effects.effect.GetItemWithId(mat.instance_effect.url);
            if (effect == null)
            {
                return null;
            }
            var daeMaterial = new DaeMaterial
            {
                Name = mat.name ?? mat.id,
                Index = _materials.Count
            };
            var samplers = new Dictionary<string, DaeTexture>();
            if (effect.Items != null)
            {
                foreach (var item in effect.Items)
                {
                    var items = ItemSelector.GetItems(item);
                    if (items != null)
                    {
                        foreach (var subItem in items)
                        {
                            if (subItem is common_newparam_type param && param.Item is fx_sampler2D_common sampler2D)
                            {
                                var surface = items.GetItemWithIdNoHash<fx_surface_common>(sampler2D.source);
                                if (surface != null)
                                {
                                    foreach (var initFrom in surface.init_from)
                                    {
                                        var image = _images[initFrom.Value];
                                        samplers.Add(param.sid, image);
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                foreach (var item in effect.Items) 
                {
                    var techniqueItem = ItemSelector.GetTechniqueItem(item);
                    if (techniqueItem is effectFx_profile_abstractProfile_COMMONTechniqueBlinn blinn)
                    {
                        AddColorOrTexProperty(DaeMaterial.AmbientName, blinn.ambient);
                        AddColorOrTexProperty(DaeMaterial.EmissionName, blinn.emission);
                        AddColorOrTexProperty(DaeMaterial.DiffuseName, blinn.diffuse);
                        AddColorOrTexProperty(DaeMaterial.ReflectiveName, blinn.reflective);
                        AddColorOrTexProperty(DaeMaterial.SpecularName, blinn.specular);
                        AddColorOrTexProperty(DaeMaterial.TransparentName, blinn.transparent);
                        AddFloatProperty(DaeMaterial.IndexOfRefractionName, blinn.index_of_refraction);
                        AddFloatProperty(DaeMaterial.ReflectivityName, blinn.reflectivity);
                        AddFloatProperty(DaeMaterial.ShininessName, blinn.shininess);
                        AddFloatProperty(DaeMaterial.TransparencyName, blinn.transparency);
                    }
                    else if (techniqueItem is effectFx_profile_abstractProfile_COMMONTechniqueConstant constant)
                    {
                        AddColorOrTexProperty(DaeMaterial.EmissionName, constant.emission);
                        AddColorOrTexProperty(DaeMaterial.ReflectiveName, constant.reflective);
                        AddColorOrTexProperty(DaeMaterial.TransparentName, constant.transparent);
                        AddFloatProperty(DaeMaterial.IndexOfRefractionName, constant.index_of_refraction);
                        AddFloatProperty(DaeMaterial.ReflectivityName, constant.reflectivity);
                        AddFloatProperty(DaeMaterial.TransparencyName, constant.transparency);
                    }
                    else if (techniqueItem is effectFx_profile_abstractProfile_COMMONTechniqueLambert lambert)
                    {
                        AddColorOrTexProperty(DaeMaterial.AmbientName, lambert.ambient);
                        AddColorOrTexProperty(DaeMaterial.EmissionName, lambert.emission);
                        AddColorOrTexProperty(DaeMaterial.DiffuseName, lambert.diffuse);
                        AddColorOrTexProperty(DaeMaterial.ReflectiveName, lambert.reflective);
                        AddColorOrTexProperty(DaeMaterial.TransparentName, lambert.transparent);
                        AddFloatProperty(DaeMaterial.IndexOfRefractionName, lambert.index_of_refraction);
                        AddFloatProperty(DaeMaterial.ReflectivityName, lambert.reflectivity);
                        AddFloatProperty(DaeMaterial.TransparencyName, lambert.transparency);
                    }
                    else if (techniqueItem is effectFx_profile_abstractProfile_COMMONTechniquePhong phong)
                    {
                        AddColorOrTexProperty(DaeMaterial.AmbientName, phong.ambient);
                        AddColorOrTexProperty(DaeMaterial.EmissionName, phong.emission);
                        AddColorOrTexProperty(DaeMaterial.DiffuseName, phong.diffuse);
                        AddColorOrTexProperty(DaeMaterial.ReflectiveName, phong.reflective);
                        AddColorOrTexProperty(DaeMaterial.SpecularName, phong.specular);
                        AddColorOrTexProperty(DaeMaterial.TransparentName, phong.transparent);
                        AddFloatProperty(DaeMaterial.IndexOfRefractionName, phong.index_of_refraction);
                        AddFloatProperty(DaeMaterial.ReflectivityName, phong.reflectivity);
                        AddFloatProperty(DaeMaterial.ShininessName, phong.shininess);
                        AddFloatProperty(DaeMaterial.TransparencyName, phong.transparency);
                    }
                    var techniqueExtra = ItemSelector.GetTechniqueExtra(item);
                    if (techniqueExtra != null)
                    {
                        foreach (var extra in techniqueExtra)
                        {
                            if (extra.technique != null)
                            {
                                foreach (var technique in extra.technique)
                                {
                                    if (technique.Any != null)
                                    {
                                        foreach (var xml in technique.Any)
                                        {
                                            if (xml.Name == "bump")
                                            {
                                                var texture = xml.GetElementsByTagName("texture");
                                                if (texture.Count > 0 && samplers.TryGetValue(texture[0].Attributes["texture"].Value, out var daeTexture))
                                                {
                                                    daeMaterial.AddProperty(DaeMaterial.BumpName, daeTexture, true);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return daeMaterial;

            void AddFloatProperty(string name, common_float_or_param_type property)
            {
                if (property?.Item == null)
                {
                    return;
                }
                if (property.Item is common_float_or_param_typeFloat floatParam)
                {
                    daeMaterial.AddProperty(name, floatParam.Value, false);
                }
                else if (property.Item is common_float_or_param_typeParam param)
                {

                }
            }

            void AddColorOrTexProperty(string name, common_color_or_texture_type property)
            {
                if (property?.Item == null)
                {
                    return;
                }
                if (property.Item is common_color_or_texture_typeColor color)
                {
                    daeMaterial.AddProperty(name, ProcessColor(color.Values), false);
                }
                else if (property.Item is common_color_or_texture_typeParam param)
                {

                }
                else if (property.Item is common_color_or_texture_typeTexture texture && samplers.TryGetValue(texture.texture, out var daeTexture))
                {
                    daeMaterial.AddProperty(name, daeTexture, true);
                }
            }
        }

        private void PostProcessAnimations()
        {
            var allAnimatedTimes = new SortedSet<float>();
            var animatedMatrices = new DaeAnimatedMatrices();
            for (var animationIndex = 0; animationIndex < _animations.Count; animationIndex++)
            {
                var animation = (DaeAnimation)_animations[animationIndex];
                var animationStart = animation.LocalStart;
                var step = 1f / animation.FrameRate * _reader.AssetLoaderContext.Options.ResampleFrequency;
                foreach (var model in animation.AnimatedModels)
                {
                    DaeGenericAnimationCurve localPositionKeyframesX = null;
                    DaeGenericAnimationCurve localPositionKeyframesY = null;
                    DaeGenericAnimationCurve localPositionKeyframesZ = null;
                    DaeGenericAnimationCurve localRotationKeyframesX = null;
                    DaeGenericAnimationCurve localRotationKeyframesY = null;
                    DaeGenericAnimationCurve localRotationKeyframesZ = null;
                    DaeGenericAnimationCurve localRotationKeyframesW = null;
                    DaeGenericAnimationCurve localScaleKeyframesX = null;
                    DaeGenericAnimationCurve localScaleKeyframesY = null;
                    DaeGenericAnimationCurve localScaleKeyframesZ = null;
                    DaeGenericAnimationCurve visibilityKeyframes = null;
                    var curvesCount = 0;
                    allAnimatedTimes.Clear();
                    allAnimatedTimes.UnionWith(model.AnimatedTimes);
                    var firstTime = allAnimatedTimes.Min;
                    var lastTime = allAnimatedTimes.Max;
                    for (var animationTime = firstTime; animationTime <= lastTime; animationTime += step)
                    {
                        allAnimatedTimes.Add(animationTime);
                    }
                    animatedMatrices.Reset(model);
                    foreach (var animationTime in allAnimatedTimes)
                    {
                        ProcessResampledKeyframes(animation, animatedMatrices, model, animationTime, animationStart, ref curvesCount, ref localRotationKeyframesX, ref localRotationKeyframesY, ref localRotationKeyframesZ, ref localRotationKeyframesW);
                    }
                    foreach (var animationTime in model.AnimatedTimes)
                    {
                        ProcessCommonKeyframes(animation, animatedMatrices, model, animationTime, animationStart, ref curvesCount, ref visibilityKeyframes, ref localPositionKeyframesX, ref localPositionKeyframesY, ref localPositionKeyframesZ, ref localScaleKeyframesX, ref localScaleKeyframesY, ref localScaleKeyframesZ);
                    }
                    var genericAnimationCurves = new List<IAnimationCurve>(curvesCount);
                    if (localPositionKeyframesX != null)
                    {
                        genericAnimationCurves.Add(localPositionKeyframesX);
                    }
                    if (localPositionKeyframesY != null)
                    {
                        genericAnimationCurves.Add(localPositionKeyframesY);
                    }
                    if (localPositionKeyframesZ != null)
                    {
                        genericAnimationCurves.Add(localPositionKeyframesZ);
                    }
                    if (localRotationKeyframesX != null)
                    {
                        genericAnimationCurves.Add(localRotationKeyframesX);
                        genericAnimationCurves.Add(localRotationKeyframesY);
                        genericAnimationCurves.Add(localRotationKeyframesZ);
                        genericAnimationCurves.Add(localRotationKeyframesW);
                    }
                    if (localScaleKeyframesX != null)
                    {
                        genericAnimationCurves.Add(localScaleKeyframesX);
                        genericAnimationCurves.Add(localScaleKeyframesY);
                        genericAnimationCurves.Add(localScaleKeyframesZ);
                    }
                    if (visibilityKeyframes != null)
                    {
                        genericAnimationCurves.Add(visibilityKeyframes);
                    }
                    animation.AnimationCurveBindings ??= new List<IAnimationCurveBinding>();
                    animation.AnimationCurveBindings.Add(new DaeAnimationCurveBinding
                    {
                        Model = model,
                        AnimationCurves = genericAnimationCurves
                    });
                }
            }
        }

        private void PostProcessModel(DaeModel model)
        {
            var localPosition = model.LocalPosition;
            var localRotation = model.LocalRotation;
            var localScale = model.LocalScale;
            ConvertMatrix(ref localPosition, ref localRotation, ref localScale, model);
            model.LocalPosition = localPosition;
            model.LocalRotation = localRotation;
            model.LocalScale = localScale;
        }


        private void ProcessAnimation(animation animation)
        {
            void ExtractChannelTarget(string target, out string id, out string @object, out string property, out string subProperty)
            {
                var indexOfSlash = target.IndexOf('/');
                if (indexOfSlash > -1)
                {
                    id = target.Substring(0, indexOfSlash);
                    property = target.Substring(indexOfSlash + 1);
                    var indexOfDot = property.IndexOf('.');
                    if (indexOfDot > -1)
                    {
                        @object = property.Substring(0, indexOfDot);
                        subProperty = property.Substring(indexOfDot + 1);
                    }
                    else
                    {
                        @object = property;
                        subProperty = null;
                    }
                }
                else
                {
                    id = null;
                    property = null;
                    @object = null;
                    subProperty = null;
                }
            }
            DaeAnimation daeAnimation;
            if (_animations.Count == 0)
            {
                daeAnimation = new DaeAnimation()
                {
                    Name = "Take001",
                    FrameRate = 30f
                };
                _animations.Add(daeAnimation);
            }
            else
            {
                daeAnimation = (DaeAnimation)_animations[0];
            }

            var channels = animation.Items?.GetItemsWithType<channel>();
            if (channels == null)
            {
                return;
            }

            foreach (var channel in channels)
            {
                ExtractChannelTarget(channel.target, out var id, out var @object, out var property, out var subProperty);
                if (id == null || property == null || !_modelsById.TryGetValue(id, out var model))
                {
                    return;
                }
                daeAnimation.AnimatedModels.Add(model);
                var animationSources = animation.Items.GetItemsWithType<source>();
                var animationSamplers = animation.Items.GetItemsWithType<sampler>();
                var channelSampler = animationSamplers.GetItemWithId(channel.source);
                if (channelSampler != null)
                {
                    var inInput = channelSampler.input.GetInputWithSemantic("INPUT");
                    var outInput = channelSampler.input.GetInputWithSemantic("OUTPUT");
                    accessor inputAccessor = null;
                    var inputList = inInput != null ? ProcessFloatList(animationSources, inInput.source, out inputAccessor) : null;
                    accessor outputAccessor = null;
                    var outputList = outInput != null ? ProcessFloatList(animationSources, outInput.source, out outputAccessor) : null;
                    if (inputList != null && outputList != null)
                    {
                        if (model.AnimatedTimes == null)
                        {
                            model.AnimatedTimes = new SortedSet<float>(inputList);
                        }
                        else
                        {
                            model.AnimatedTimes.UnionWith(inputList);
                        }
                        daeAnimation.LocalStart = Mathf.Min(daeAnimation.LocalStart, model.AnimatedTimes.Min);
                        daeAnimation.LocalEnd = Mathf.Max(daeAnimation.LocalEnd, model.AnimatedTimes.Max);
                        var animationCurve = new DaeAnimationCurve();
                        animationCurve.Times = inputList;
                        animationCurve.Values = outputList;
                        animationCurve.Property = property;
                        animationCurve.Object = @object;
                        animationCurve.SubProperty = subProperty;
                        animationCurve.OutputAccessor = outputAccessor;
                        if (!daeAnimation.AnimationCurveBindingsDictionary.TryGetValue(model, out var animationCurves))
                        {
                            animationCurves = new List<DaeAnimationCurve>();
                            daeAnimation.AnimationCurveBindingsDictionary.Add(model, animationCurves);
                        }
                        animationCurves.Add(animationCurve);
                    }
                }
            }
        }

        private void ProcessCommonKeyframes(
          DaeAnimation animation,
          DaeAnimatedMatrices animatedMatrices,
          DaeModel model,
          float animationTime,
          float animationStart,
          ref int curvesCount,
          ref DaeGenericAnimationCurve visibilityKeyframes,
          ref DaeGenericAnimationCurve localPositionKeyframesX,
          ref DaeGenericAnimationCurve localPositionKeyframesY,
          ref DaeGenericAnimationCurve localPositionKeyframesZ,
          ref DaeGenericAnimationCurve localScaleKeyframesX,
          ref DaeGenericAnimationCurve localScaleKeyframesY,
          ref DaeGenericAnimationCurve localScaleKeyframesZ)
        {
            if (animation.AnimationCurveBindingsDictionary.TryGetValue(model, out var animationCurves))
            {
                for (var animationCurveIndex = 0; animationCurveIndex < animationCurves.Count; animationCurveIndex++)
                {
                    var daeAnimationCurve = animationCurves[animationCurveIndex];
                    if (daeAnimationCurve.Times.Count != 0)
                    {
                        animatedMatrices.ElementNames.TryGetValue(daeAnimationCurve.Object, out var name);
                        var value = daeAnimationCurve.Evaluate(animationTime, name);
                        animatedMatrices.UpdateValues(daeAnimationCurve.Object, daeAnimationCurve.SubProperty, value);
                    }
                }
            }
            var finalTime = animationTime - animationStart;
            var finalMatrix = animatedMatrices.TransformMatrices();
            finalMatrix.Decompose(out var t, out var r, out var s);
            ConvertMatrix(ref t, ref r, ref s, model);
            if (animatedMatrices.HasMatrix(ItemsChoiceType7.translate))
            {
                if (localPositionKeyframesX == null)
                {
                    localPositionKeyframesX = new DaeGenericAnimationCurve("localPosition.x", typeof(Transform));
                    localPositionKeyframesY = new DaeGenericAnimationCurve("localPosition.y", typeof(Transform));
                    localPositionKeyframesZ = new DaeGenericAnimationCurve("localPosition.z", typeof(Transform));
                    curvesCount += 3;
                }
                AddKeyToCurve(localPositionKeyframesX, finalTime, t.x, false);
                AddKeyToCurve(localPositionKeyframesY, finalTime, t.y, false);
                AddKeyToCurve(localPositionKeyframesZ, finalTime, t.z, false);
            }
            if (animatedMatrices.HasMatrix(ItemsChoiceType7.scale))
            {
                if (localScaleKeyframesX == null)
                {
                    localScaleKeyframesX = new DaeGenericAnimationCurve("localScale.x", typeof(Transform));
                    localScaleKeyframesY = new DaeGenericAnimationCurve("localScale.y", typeof(Transform));
                    localScaleKeyframesZ = new DaeGenericAnimationCurve("localScale.z", typeof(Transform));
                    curvesCount += 3;
                }
                AddKeyToCurve(localScaleKeyframesX, finalTime, s.x, true);
                AddKeyToCurve(localScaleKeyframesY, finalTime, s.y, true);
                AddKeyToCurve(localScaleKeyframesZ, finalTime, s.z, true);
            }
        }

        private List<float> ProcessFloatList(IList<source> sources, string sourceName, out accessor accessor)
        {
            if (_processedFloatLists.TryGetValue(sourceName, out var generatedFloats))
            {
                accessor = _processedAccessors[sourceName];
                return generatedFloats;
            }
            accessor = null;
            var source = sources.GetItemWithId(sourceName);
            if (source != null)
            {
                accessor = source.technique_common.accessor;
                if (source.Item is float_array floatArray)
                {
                    if (source.technique_common != null)
                    {
                        var numItems = (int)source.technique_common.accessor.count;
                        var stride = (int)source.technique_common.accessor.stride;
                        var totalElements = numItems * stride;
                        generatedFloats = new List<float>(totalElements);
                        for (var i = 0; i < totalElements; i++)
                        {
                            var f = (float)floatArray.Values[i];
                            generatedFloats.Add(f);
                        }
                    }
                }
                _processedAccessors.Add(sourceName, accessor);
                _processedFloatLists.Add(sourceName, generatedFloats);
            }
            return generatedFloats;
        }
        private List<Matrix4x4> ProcessMatrix4x4List(
          IList<source> sources,
          string sourceName)
        {
            if (_processedMatrixLists.TryGetValue(sourceName, out var generatedMatrices))
            {
                return generatedMatrices;
            }
            var source = sources.GetItemWithId(sourceName);
            if (source?.Item is float_array floatArray && source.technique_common != null)
            {
                var strideIndex = 0;
                var numItems = (int)source.technique_common.accessor.count;
                generatedMatrices = new List<Matrix4x4>(numItems);
                var values = floatArray.Values;
                for (var i = 0; i < numItems; i++)
                {
                    var unityMatrix = new Matrix4x4
                    {
                        [0] = (float)values[strideIndex + 0],
                        [1] = (float)values[strideIndex + 4],
                        [2] = (float)values[strideIndex + 8],
                        [3] = (float)values[strideIndex + 12],
                        [4] = (float)values[strideIndex + 1],
                        [5] = (float)values[strideIndex + 5],
                        [6] = (float)values[strideIndex + 9],
                        [7] = (float)values[strideIndex + 13],
                        [8] = (float)values[strideIndex + 2],
                        [9] = (float)values[strideIndex + 6],
                        [10] = (float)values[strideIndex + 10],
                        [11] = (float)values[strideIndex + 14],
                        [12] = (float)values[strideIndex + 3],
                        [13] = (float)values[strideIndex + 7],
                        [14] = (float)values[strideIndex + 11],
                        [15] = (float)values[strideIndex + 15]
                    };
                    generatedMatrices.Add(unityMatrix);
                    strideIndex += (int)source.technique_common.accessor.stride;
                }
            }
            _processedMatrixLists.Add(sourceName, generatedMatrices);
            return generatedMatrices;
        }

        private IModel ProcessModel(node node)
        {
            var model = new DaeModel();
            if (node.instance_node != null)
            {
                var instanceWithExtra = node.instance_node[0];
                var libraryNodes = _collada.Items?.GetItemWithType<library_nodes>();
                if (libraryNodes != null)
                {
                    var itemWithId = libraryNodes.node.GetItemWithId(instanceWithExtra.url);
                    if (itemWithId.instance_camera != null)
                    {
                        node.instance_camera = itemWithId.instance_camera;
                    }
                    if (itemWithId.instance_controller != null)
                    {
                        node.instance_controller = itemWithId.instance_controller;
                    }
                    if (itemWithId.instance_geometry != null)
                    {
                        node.instance_geometry = itemWithId.instance_geometry;
                    }
                    if (itemWithId.instance_light != null)
                    {
                        node.instance_light = itemWithId.instance_light;
                    }
                }
            }
            model.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData { ModelName = node.name, Id = node.id }, model, _reader.Name);
            model.Visibility = true;
            model.Parent = _rootModel;
            var matrix = node.Items?.GetItemWithType<matrix>();
            if (matrix != null)
            {
                model.Matrices.AddMatrix(ItemsChoiceType7.matrix, matrix.sid, matrix.Values);
            }
            if (node.Items != null)
            {
                for (var i = 0; i < node.Items.Length; i++)
                {
                    var item = node.Items[i];
                    var elementName = node.ItemsElementName[i];
                    if (item is TargetableFloat3 targetableFloat3)
                    {
                        switch (elementName)
                        {
                            case ItemsChoiceType7.translate:
                                model.Matrices.AddMatrix(elementName, targetableFloat3.sid, targetableFloat3.Values);
                                break;
                            case ItemsChoiceType7.scale:
                                model.Matrices.AddMatrix(elementName, targetableFloat3.sid, targetableFloat3.Values);
                                break;
                        }
                    }
                    else if (item is rotate rotation)
                    {
                        model.Matrices.AddMatrix(elementName, rotation.sid, rotation.Values);
                    }
                }
            }
            var finalMatrix = model.Matrices.TransformMatrices();
            finalMatrix.Decompose(out var localPosition, out var localRotation, out var localScale);
            model.LocalPosition = localPosition;
            model.LocalRotation = localRotation;
            model.LocalScale = localScale;
            if (node.node1 != null)
            {
                foreach (var subNode in node.node1)
                {
                    var child = ProcessModel(subNode);
                    child.Parent = model;
                    model.Children ??= new List<IModel>();
                    model.Children.Add(child);
                }
            }
            model.Node = node;
            _rootModel.AllModels.Add(model);
            _models.Add(model);
            if (node.sid != null)
            {
                _modelsBySid.Add(node.sid, model);
            }
            if (node.id != null && !_modelsById.TryAdd(node.id, model))
                if (node.id != null && !_modelsById.TryAdd(node.id, model))
                {
                    if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        Debug.Log($"Node {node.id} has already been registered.");
                    }
                }
            return model;
        }

        private void ProcessModelGeometries(DaeModel daeModel)
        {
            if (daeModel.Node.instance_controller == null && daeModel.Node.instance_geometry == null)
            {
                return;
            }
            var bindings = new Dictionary<string, int>();
            bind_material bindMaterial = null;
            skin skin = null;
            morph morph1 = null;
            string id = null;
            if (daeModel.Node.instance_controller != null)
            {
                var instanceController = daeModel.Node.instance_controller[0];
                var itemWithType = _collada.Items.GetItemWithType<library_controllers>();
                var obj = itemWithType?.controller.GetItemWithId(instanceController.url)?.Item;
                if (obj is not skin skin2)
                {
                    if (obj is morph morph2)
                    {
                        bindMaterial = instanceController.bind_material;
                        id = morph2.source1;
                        morph1 = morph2;
                    }
                }
                else
                {
                    bindMaterial = instanceController.bind_material;
                    id = skin2.source1;
                    skin = skin2;
                }
            }
            else if (daeModel.Node.instance_geometry != null)
            {
                var instanceGeometry = daeModel.Node.instance_geometry[0];
                bindMaterial = instanceGeometry.bind_material;
                id = instanceGeometry.url;
            }
            if (bindMaterial != null)
            {
                foreach (var instanceMaterial in bindMaterial.technique_common)
                {
                    if (instanceMaterial?.symbol != null)
                    {
                        if (!bindings.ContainsKey(instanceMaterial.symbol))
                        {
                            bindings.Add(instanceMaterial.symbol, instanceMaterial.target.GetHashCode());
                        }
                    }
                }
            }
            DaeGeometryGroup geometryGroup = null;
            var libraryGeometries = _collada.Items.GetItemWithType<library_geometries>();
            if (libraryGeometries != null)
            {
                var geometry = libraryGeometries.geometry.GetItemWithId(id);
                if (geometry?.Item is mesh mesh)
                {
                    mesh.Items.GetItemsWithType<vertices>();
                    var allTriangles = mesh.Items.GetItemsWithType<triangles>();
                    var allPolyList = mesh.Items.GetItemsWithType<polylist>();
                    var allTriFans = mesh.Items.GetItemsWithType<trifans>();
                    var allTriStrips = mesh.Items.GetItemsWithType<tristrips>();
                    var allPolygons = mesh.Items.GetItemsWithType<polygons>();
                    if (allTriangles != null || allPolyList != null || allTriFans != null || allTriStrips != null || allPolygons != null)
                    {
                        var positionData = mesh.vertices?.input?.GetInputWithSemantic("POSITION");
                        var normalsData = mesh.vertices?.input?.GetInputWithSemantic("NORMAL");
                        var texCoordData = mesh.vertices?.input?.GetInputWithSemantic("TEXCOORD");
                        var geometryBindings = new List<DaeGeometryBinding>();
                        var hasPositions = positionData != null;
                        var hasNormals = normalsData != null;
                        var hasUVs = texCoordData != null;
                        if (allTriangles != null)
                        {
                            foreach (var list in allTriangles)
                            {
                                var daeGeometryBinding = ProcessInputs(list.input, ref hasPositions, ref hasNormals, ref hasUVs);
                                daeGeometryBinding.Triangles = list;
                                daeGeometryBinding.Material = list.material;
                                geometryBindings.Add(daeGeometryBinding);
                            }
                        }
                        if (allPolyList != null)
                        {
                            foreach (var list in allPolyList)
                            {
                                var daeGeometryBinding = ProcessInputs(list.input, ref hasPositions, ref hasNormals, ref hasUVs);
                                daeGeometryBinding.PolyList = list;
                                daeGeometryBinding.Material = list.material;
                                geometryBindings.Add(daeGeometryBinding);
                            }
                        }
                        if (allTriFans != null)
                        {
                            foreach (var list in allTriFans)
                            {
                                var daeGeometryBinding =
                                    ProcessInputs(list.input, ref hasPositions, ref hasNormals, ref hasUVs);

                                daeGeometryBinding.TriFans = list;
                                daeGeometryBinding.Material = list.material;

                                geometryBindings.Add(daeGeometryBinding);
                            }
                        }
                        if (allTriStrips != null)
                        {
                            foreach (var list in allTriStrips)
                            {
                                var daeGeometryBinding =
                                    ProcessInputs(list.input, ref hasPositions, ref hasNormals, ref hasUVs);

                                daeGeometryBinding.TriStrips = list;
                                daeGeometryBinding.Material = list.material;

                                geometryBindings.Add(daeGeometryBinding);
                            }
                        }
                        if (allPolygons != null)
                        {
                            foreach (var list in allPolygons)
                            {
                                var daeGeometryBinding =
                                    ProcessInputs(list.input, ref hasPositions, ref hasNormals, ref hasUVs);

                                daeGeometryBinding.Polygons = list;
                                daeGeometryBinding.Material = list.material;

                                geometryBindings.Add(daeGeometryBinding);
                            }
                        }
                        var hasSkin = skin != null || morph1 != null;
                        geometryGroup = CreateGeometryGroup(hasNormals,
                            false,
                            false,
                            hasUVs,
                            false,
                            false,
                            false,
                            hasSkin);
                        geometryGroup.Name = geometry.name ?? geometry.id;
                        geometryGroup.Setup(_reader.AssetLoaderContext, 3, 3);
                        _rootModel.AllGeometryGroups.Add(geometryGroup);
                        _geometries.TryAdd(geometry.id, geometryGroup);
                        if (skin != null)
                        {
                            if (skin.vertex_weights != null)
                            {
                                var joint = skin.vertex_weights.input.GetInputWithSemantic("JOINT");
                                var weight = skin.vertex_weights.input.GetInputWithSemantic("WEIGHT");
                                if (joint != null && weight != null)
                                {
                                    var biggestOffset2 = 0;
                                    foreach (var input in skin.vertex_weights.input)
                                    {
                                        biggestOffset2 = (int)Mathf.Max(biggestOffset2, input.offset);
                                    }
                                    var biggestOffset = biggestOffset2 + 1;
                                    var vCount = SplitToInt(skin.vertex_weights.vcount, true, 1);
                                    var vData = SplitToInt(skin.vertex_weights.v, true, biggestOffset);
                                    var weightList = ProcessFloatList(skin.source, weight.source, out _);
                                    var vIndex = 0;
                                    for (var i = 0; i < vCount.Length; i++)
                                    {
                                        var count = vCount[i];
                                        for (var j = 0; j < count; j++)
                                        {
                                            var jointValue = vData[vIndex + (int)joint.offset];
                                            var weightIndex = vData[vIndex + (int)weight.offset];
                                            var weightValue = weightList[weightIndex];
                                            geometryGroup.AddBoneWeight(vCount.Length - i - 1, new BoneWeight1()
                                            {
                                                boneIndex = jointValue,
                                                weight = weightValue
                                            });
                                            vIndex += biggestOffset;
                                        }
                                    }
                                }
                            }
                            if (skin.joints != null)
                            {
                                var joint = skin.joints.input.GetInputWithSemantic("JOINT");
                                var invBindMatrixInput = skin.joints.input.GetInputWithSemantic("INV_BIND_MATRIX");
                                if (joint != null && invBindMatrixInput != null)
                                {
                                    var jointList = ProcessStringList(skin.source, joint.source, out _);
                                    var invBindMatrixList = ProcessMatrix4x4List(skin.source, invBindMatrixInput.source);
                                    var bindPoses = new Matrix4x4[jointList.Count];
                                    var bones = new List<IModel>(jointList.Count);
                                    for (var i = 0; i < bindPoses.Length; i++)
                                    {
                                        if (_modelsBySid.TryGetValue(jointList[i], out var bone))
                                        {
                                            bone.IsBone = true;
                                            bones.Add(bone);
                                        }
                                        var invBindMatrix = DecomposeAndConvertMatrix(invBindMatrixList[i], null);
                                        bindPoses[i] = invBindMatrix;
                                    }
                                    daeModel.BindPoses = bindPoses;
                                    daeModel.Bones = bones;
                                }
                            }
                        }
                        foreach (var geometryBinding in geometryBindings)
                        {
                            var daeGeometry = GetGeometry(geometryGroup, geometryBinding.Material, bindings);
                            var verticesList = ProcessVector3List(mesh.source, positionData.source, true, true);
                            var texCoordsList = texCoordData == null ? geometryBinding.TexCoordInput == null ? null : ProcessVector2List(mesh.source, geometryBinding.TexCoordInput.source) : ProcessVector2List(mesh.source, texCoordData.source);
                            var normalsList = normalsData == null ? geometryBinding.NormalsInput == null ? null : ProcessVector3List(mesh.source, geometryBinding.NormalsInput.source, false, true) : ProcessVector3List(mesh.source, normalsData.source, false, true);
                            var verticesInputOffset = geometryBinding.VerticesInput != null ? (int)geometryBinding.VerticesInput.offset : 0;
                            var normalsInputOffset = geometryBinding.NormalsInput != null ? (int)geometryBinding.NormalsInput.offset : 0;
                            var texCoordsInputOffset = geometryBinding.TexCoordInput != null ? (int)geometryBinding.TexCoordInput.offset : 0;
                            geometryGroup.VerticesList = verticesList;
                            geometryGroup.TexCoordsList = texCoordsList;
                            geometryGroup.NormalsList = normalsList;
                            if (geometryBinding.PolyList != null)
                            {
                                var polyDataIndex = 0;
                                var vcount = SplitToInt(geometryBinding.PolyList.vcount, true, 1);
                                var polyListData = SplitToInt(geometryBinding.PolyList.p, true, geometryBinding.BiggestOffset);
                                geometryGroup.Primitives = polyListData;
                                for (var i = 0; i < vcount.Length; i++)
                                {
                                    var count = vcount[i];
                                    var contourVertices = new ContourVertex[count];
                                    for (var j = 0; j < count; j++)
                                    {
                                        var daeVector = new DaeVector();
                                        daeVector.GeometryGroup = geometryGroup;
                                        daeVector.VertexIndex = polyDataIndex + verticesInputOffset;
                                        daeVector.OriginalVertexIndex = polyListData[daeVector.VertexIndex];
                                        var vertexIndex = ListUtils.FixIndex(daeVector.VertexIndex, polyListData);
                                        if (normalsList != null && normalsList.Count > 0)
                                        {
                                            daeVector.NormalIndex = polyDataIndex + normalsInputOffset;
                                        }
                                        if (texCoordsList != null && texCoordsList.Count > 0)
                                        {
                                            daeVector.TexCoordIndex = polyDataIndex + texCoordsInputOffset;
                                        }
                                        var vertex = ListUtils.FixIndex(vertexIndex, verticesList);
                                        contourVertices[j] = new ContourVertex(new Vec3(vertex.x, vertex.y, vertex.z), daeVector);
                                        polyDataIndex += geometryBinding.BiggestOffset;
                                    }
                                    Helpers.Tesselate(contourVertices,
                                        _reader.AssetLoaderContext,
                                        daeGeometry,
                                        geometryGroup);
                                }
                            }
                            else if (geometryBinding.Polygons != null)
                            {
                                var items = geometryBinding.Polygons.Items.GetItemsWithType<string>();
                                foreach (var data in items)
                                {
                                    var polyListData = SplitToInt(data, true, geometryBinding.BiggestOffset);
                                    var count = polyListData.Length / geometryBinding.BiggestOffset;
                                    var contourVertices = new ContourVertex[count];
                                    var polyListDataIndex = 0;
                                    geometryGroup.Primitives = polyListData;
                                    for (var i = 0; i < count; i++)
                                    {
                                        var daeVector = new DaeVector();
                                        daeVector.GeometryGroup = geometryGroup;
                                        daeVector.VertexIndex = polyListDataIndex + verticesInputOffset;
                                        daeVector.OriginalVertexIndex = polyListData[daeVector.VertexIndex];
                                        var vertexIndex = ListUtils.FixIndex(daeVector.VertexIndex, polyListData);
                                        if (normalsList != null && normalsList.Count > 0)
                                        {
                                            daeVector.NormalIndex = polyListDataIndex + normalsInputOffset;
                                        }
                                        if (texCoordsList != null && texCoordsList.Count > 0)
                                        {
                                            daeVector.TexCoordIndex = polyListDataIndex + texCoordsInputOffset;
                                        }
                                        var vertex = ListUtils.FixIndex(vertexIndex, verticesList);
                                        contourVertices[i] = new ContourVertex(new Vec3(vertex.x, vertex.y, vertex.z), daeVector);
                                        polyListDataIndex += geometryBinding.BiggestOffset;
                                    }
                                    Helpers.Tesselate(contourVertices,
                                        _reader.AssetLoaderContext,
                                        (IGeometry)daeGeometry,
                                        geometryGroup);
                                }
                            }
                            else if (geometryBinding.TriFans != null)
                            {
                                foreach (var p in geometryBinding.TriFans.p)
                                {
                                    var primitives = SplitToInt(p, true, geometryBinding.BiggestOffset);
                                    var primitiveIndex = 0;
                                    var count = primitives.Length / geometryBinding.BiggestOffset;
                                    for (var j = 1; j < count - 1; j++)
                                    {
                                        AddVertexWithOffset(0,
                                            verticesInputOffset,
                                            normalsInputOffset,
                                            texCoordsInputOffset,
                                            daeGeometry,
                                            primitives,
                                            verticesList,
                                            normalsList,
                                            texCoordsList,
                                            hasSkin);
                                        AddVertexWithOffset(primitiveIndex,
                                            verticesInputOffset,
                                            normalsInputOffset,
                                            texCoordsInputOffset,
                                            daeGeometry,
                                            primitives,
                                            verticesList,
                                            normalsList,
                                            texCoordsList,
                                            hasSkin);
                                        AddVertexWithOffset(primitiveIndex + 1,
                                            verticesInputOffset,
                                            normalsInputOffset,
                                            texCoordsInputOffset,
                                            daeGeometry,
                                            primitives,
                                            verticesList,
                                            normalsList,
                                            texCoordsList,
                                            hasSkin);
                                        primitiveIndex += geometryBinding.BiggestOffset;
                                    }
                                }
                            }
                            else if (geometryBinding.TriStrips != null) //todo: test this
                            {
                                foreach (var p in geometryBinding.TriStrips.p)
                                {
                                    var primitives = SplitToInt(p, true, geometryBinding.BiggestOffset);
                                    var primitiveIndex = 0;
                                    var count = primitives.Length / geometryBinding.BiggestOffset;
                                    for (var j = 2; j < count; j++)
                                    {
                                        if (j % 2 == 0)
                                        {
                                            AddVertexWithOffset(j - 2,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                            AddVertexWithOffset(j - 1,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                            AddVertexWithOffset(j,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                        }
                                        else
                                        {
                                            AddVertexWithOffset(j - 1,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                            AddVertexWithOffset(j - 2,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                            AddVertexWithOffset(j,
                                                verticesInputOffset,
                                                normalsInputOffset,
                                                texCoordsInputOffset,
                                                daeGeometry,
                                                primitives,
                                                verticesList,
                                                normalsList,
                                                texCoordsList,
                                                hasSkin);
                                        }
                                        primitiveIndex += geometryBinding.BiggestOffset;
                                    }
                                }
                            }
                            else //triangles
                            {
                                var primitives = SplitToInt(geometryBinding.Triangles.p, true, geometryBinding.BiggestOffset);
                                var primitiveIndex = 0;
                                var count = primitives.Length / geometryBinding.BiggestOffset;
                                for (var i = 0; i < count; i++)
                                {
                                    AddVertexWithOffset(primitiveIndex,
                                        verticesInputOffset,
                                        normalsInputOffset,
                                        texCoordsInputOffset,
                                        daeGeometry,
                                        primitives,
                                        verticesList,
                                        normalsList,
                                        texCoordsList,
                                        hasSkin);
                                    primitiveIndex += geometryBinding.BiggestOffset;
                                }
                            }
                        }
                    }
                }
            }
            if (geometryGroup != null)
            {
                var materialIndices = new int[geometryGroup.GeometriesData.Count];
                foreach (var geometry in geometryGroup.GeometriesData.Values)
                {
                    if (_materials.TryGetValue(geometry.MaterialIndex, out var material))
                    {
                        var index = material.Index < 0 ? 0 : material.Index < _materials.Count ? 1 : 0;
                        materialIndices[geometry.Index] = index == 0 ? -1 : material.Index;
                    }
                    else
                    {
                        materialIndices[geometry.Index] = -1;
                    }
                }
                daeModel.GeometryGroup = geometryGroup;
                daeModel.MaterialIndices = materialIndices;
            }

            DaeGeometryBinding ProcessInputs(
              InputLocalOffset[] listInput,
              ref bool hasVertices,
              ref bool hasNormals,
              ref bool hasUVs)
            {
                var geometryBinding = new DaeGeometryBinding();
                foreach (var input in listInput)
                {
                    switch (input.semantic)
                    {
                        case "VERTEX":
                            geometryBinding.VerticesInput = input;
                            hasVertices = true;
                            break;
                        case "NORMAL":
                            geometryBinding.NormalsInput = input;
                            hasNormals = true;
                            break;
                        case "TEXCOORD":
                            geometryBinding.TexCoordInput = input;
                            hasUVs = true;
                            break;
                    }
                    geometryBinding.BiggestOffset = (int)Mathf.Max(geometryBinding.BiggestOffset, input.offset);
                }
                geometryBinding.BiggestOffset++;
                return geometryBinding;
            }

            void AddVertexWithOffset(
                  int primitiveIndex,
                  int verticesInputOffset,
                  int normalsInputOffset,
                  int texCoordsInputOffset,
                  CommonGeometry daeGeometry,
                  IList<int> primitives,
                  List<Vector3> verticesList,
                  List<Vector3> normalsList,
                  List<Vector2> texCoordsList,
                  bool hasSkin)
            {
                var hasVertices = verticesList != null && verticesList.Count > 0;
                var hasNormals = normalsList != null && normalsList.Count > 0;
                var hasTexCoords = texCoordsList != null && texCoordsList.Count > 0;
                var vertexIndex = primitiveIndex + verticesInputOffset;
                var normalIndex = hasNormals ? primitiveIndex + normalsInputOffset : -1;
                var texCoordIndex = hasTexCoords ? primitiveIndex + texCoordsInputOffset : -1;
                AddVertex(daeGeometry,
                    primitives,
                    vertexIndex,
                    normalIndex,
                    texCoordIndex,
                    hasVertices,
                    hasNormals,
                    hasTexCoords,
                    verticesList,
                    normalsList,
                    texCoordsList,
                    hasSkin);
            }
        }

        private void ProcessResampledKeyframes(
          DaeAnimation animation,
          DaeAnimatedMatrices animatedMatrices,
          DaeModel model,
          float animationTime,
          float animationStart,
          ref int curvesCount,
          ref DaeGenericAnimationCurve localRotationKeyframesX,
          ref DaeGenericAnimationCurve localRotationKeyframesY,
          ref DaeGenericAnimationCurve localRotationKeyframesZ,
          ref DaeGenericAnimationCurve localRotationKeyframesW)
        {
            if (animation.AnimationCurveBindingsDictionary.TryGetValue(model, out var animationCurves))
            {
                for (var animationCurveIndex = 0; animationCurveIndex < animationCurves.Count; animationCurveIndex++)
                {
                    var animationCurve = animationCurves[animationCurveIndex];
                    if (animationCurve.Times.Count != 0)
                    {
                        animatedMatrices.ElementNames.TryGetValue(animationCurve.Object, out var name);
                        var value = animationCurve.Evaluate(animationTime, name);
                        animatedMatrices.UpdateValues(animationCurve.Object, animationCurve.SubProperty, value);
                    }
                }
            }
            var finalTime = animationTime - animationStart;
            var finalMatrix = animatedMatrices.TransformMatrices();
            finalMatrix.Decompose(out var t, out var r, out var s);
            ConvertMatrix(ref t, ref r, ref s, model);
            if (localRotationKeyframesX == null)
            {
                localRotationKeyframesX = new DaeGenericAnimationCurve("localRotation.x", typeof(Transform));
                localRotationKeyframesY = new DaeGenericAnimationCurve("localRotation.y", typeof(Transform));
                localRotationKeyframesZ = new DaeGenericAnimationCurve("localRotation.z", typeof(Transform));
                localRotationKeyframesW = new DaeGenericAnimationCurve("localRotation.w", typeof(Transform));
                curvesCount += 4;
            }
            if (r.IsInvalid())
            {
                r = Quaternion.identity;
            }
            AddRotationKeysToCurves(finalTime,
                localRotationKeyframesX,
                localRotationKeyframesY,
                localRotationKeyframesZ,
                localRotationKeyframesW,
                r);
        }

        private IRootModel ProcessScene()
        {
            _rootModel = new DaeRootModel
            {
                LocalRotation = Quaternion.identity,
                LocalScale = Vector3.one
            };
            _scaleFactor = _collada.asset?.unit != null ? (float)(1f / _collada.asset.unit.meter) : 1f; //todo: test this
            _upAxis = _collada.asset?.up_axis ?? UpAxisType.Y_UP;
            SetupCoordSystem();
            var libraryImages = _collada.Items?.GetItemWithType<library_images>();
            if (libraryImages?.image != null)
            {
                foreach (var image in libraryImages.image)
                {
                    var daeTexture = new DaeTexture();
                    daeTexture.Filename = image.Item.ToString();
                    daeTexture.ResolveFilename(_reader.AssetLoaderContext);
                    daeTexture.Name = image.name ?? image.id;
                    _rootModel.AllTextures.Add(daeTexture);
                    _images.Add(image.id, daeTexture);
                }
            }
            var libraryMaterials = _collada.Items?.GetItemWithType<library_materials>();
            if (libraryMaterials?.material != null)
            {
                foreach (var mat in libraryMaterials.material)
                {
                    var materialKey = $"#{mat.id}".GetHashCode();
                    if (!_materials.TryGetValue(materialKey, out var material))
                    {
                        material = ParseMaterial(mat);
                        _materials.Add(materialKey, material);
                        _rootModel.AllMaterials.Add(material);
                    }
                }
            }
            var libraryVisualScenes = _collada.Items?.GetItemWithType<library_visual_scenes>();
            if (libraryVisualScenes != null)
            {
                var visualSceneNode = libraryVisualScenes.visual_scene.GetItemWithId(_collada.scene.instance_visual_scene.url);
                if (visualSceneNode != null)
                {
                    _rootModel.Children = new List<IModel>(visualSceneNode.node.Length);
                    foreach (var node in visualSceneNode.node)
                    {
                        var model = ProcessModel(node);
                        _rootModel.Children.Add(model);
                    }
                }
                foreach (var model in _models)
                {
                    ProcessModelGeometries(model);
                }
                foreach (var model in _models)
                {
                    PostProcessModel(model);
                }
            }
            var libraryAnimations = _collada.Items.GetItemWithType<library_animations>();
            if (libraryAnimations?.animation != null)
            {
                foreach (var animation in libraryAnimations.animation)
                {
                    ProcessAnimation(animation);
                }
                PostProcessAnimations();
                _rootModel.AllAnimations = _animations;
            }
            return _rootModel;
        }

        private List<string> ProcessStringList(IList<source> sources, string sourceName, out accessor accessor)
        {
            if (_processedStringLists.TryGetValue(sourceName, out var generatedStrings))
            {
                accessor = _processedAccessors[sourceName];
                return generatedStrings;
            }
            accessor = null;
            var source = sources.GetItemWithId(sourceName);
            if (source != null)
            {
                accessor = source.technique_common.accessor;
                if (source.Item is Name_array nameArray)
                {
                    if (source.technique_common != null)
                    {
                        var strideIndex = 0;
                        var numItems = (int)source.technique_common.accessor.count;
                        generatedStrings = new List<string>(numItems);
                        for (var i = 0; i < numItems; i++)
                        {
                            var s = nameArray.Values[strideIndex];
                            strideIndex += (int)source.technique_common.accessor.stride;
                            generatedStrings.Add(s);
                        }
                    }
                }
                else if (source.Item is IDREF_array idrefArray)
                {
                    if (source.technique_common != null)
                    {
                        var values = idrefArray.Value.Split(SplitChars, StringSplitOptions.RemoveEmptyEntries);
                        var strideIndex = 0;
                        var numItems = (int)source.technique_common.accessor.count;
                        generatedStrings = new List<string>(numItems);
                        for (var i = 0; i < numItems; i++)
                        {
                            var s = values[strideIndex];
                            strideIndex += (int)source.technique_common.accessor.stride;
                            generatedStrings.Add(s);
                        }
                    }
                }
                _processedAccessors.Add(sourceName, accessor);
                _processedStringLists.Add(sourceName, generatedStrings);
            }
            return generatedStrings;
        }

        private List<Vector2> ProcessVector2List(IList<source> sources, string sourceName)
        {
            if (_processedVector2Lists.TryGetValue(sourceName, out var generatedVertices))
            {
                return generatedVertices;
            }
            var source = sources.GetItemWithId(sourceName);
            if (source?.Item is float_array floatArray && source.technique_common != null)
            {
                var strideIndex = 0;
                var numItems = (int)source.technique_common.accessor.count;
                generatedVertices = new List<Vector2>(numItems);
                for (var i = 0; i < numItems; i++)
                {
                    var dataIndex = 0;
                    var vertex = new Vector2();
                    foreach (var param in source.technique_common.accessor.param)
                    {
                        switch (param.name)
                        {
                            case "U":
                            case "S":
                                vertex.x = (float)floatArray.Values[strideIndex + dataIndex++];
                                break;
                            case "V":
                            case "T":
                                vertex.y = (float)floatArray.Values[strideIndex + dataIndex++];
                                break;
                        }
                    }
                    strideIndex += (int)source.technique_common.accessor.stride;
                    generatedVertices.Add(vertex);
                }
            }
            _processedVector2Lists.Add(sourceName, generatedVertices);
            return generatedVertices;
        }

        private List<Vector3> ProcessVector3List(
          IList<source> sources,
          string sourceName,
          bool applyScale,
          bool applyOrientation)
        {
            if (_processedVector3Lists.TryGetValue(sourceName, out var generatedVertices))
            {
                return generatedVertices;
            }
            var source = sources.GetItemWithId(sourceName);
            if (source?.Item is float_array floatArray)
            {
                if (source.technique_common != null)
                {
                    var strideIndex = 0;
                    var numItems = (int)source.technique_common.accessor.count;
                    generatedVertices = new List<Vector3>(numItems);
                    for (var i = 0; i < numItems; i++)
                    {
                        var dataIndex = 0;
                        var vertex = new Vector3();
                        foreach (var param in source.technique_common.accessor.param)
                        {
                            switch (param.name)
                            {
                                case "X":
                                    vertex.x = (float)floatArray.Values[strideIndex + dataIndex++];
                                    break;
                                case "Y":
                                    vertex.y = (float)floatArray.Values[strideIndex + dataIndex++];
                                    break;
                                case "Z":
                                    vertex.z = (float)floatArray.Values[strideIndex + dataIndex++];
                                    break;
                            }
                        }
                        vertex = ConvertVector(vertex, applyScale, applyOrientation);
                        strideIndex += (int)source.technique_common.accessor.stride;
                        generatedVertices.Add(vertex);
                    }
                }
            }
            _processedVector3Lists.Add(sourceName, generatedVertices);
            return generatedVertices;
        }
    }
}
