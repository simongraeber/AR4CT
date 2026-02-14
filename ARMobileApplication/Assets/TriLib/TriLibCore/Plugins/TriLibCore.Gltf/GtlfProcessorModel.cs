using System.Collections.Generic;
using System.IO;
using TriLibCore.Extensions;
using TriLibCore.General;
using TriLibCore.Gltf.Reader;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        
        public IRootModel ParseModel(Stream stream)
        {
            _stream = stream;

            var gltf = LoadModel(stream, this);
            _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.Parsing);

            if (gltf.TryGetChildWithKey(_buffers_token, out buffers))
            {
                _buffersData = new StreamChunk[buffers.Count];
                for (var i = 0; i < buffers.Count; ++i)
                {
                    _buffersData[i] = LoadBinaryBuffer(buffers, i);

                }
                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.LoadBuffers);
            }

            if (gltf.TryGetChildWithKey(_extensionsRequired_token, out var extensionsRequired))
            {
                
                foreach (var extension in extensionsRequired)
                {
                    var extensionName = extension.ToString();
                    switch (extensionName)
                    {
                        case "KHR_mesh_quantization":
                            _quantitized = true;
                            break;
                        case "KHR_draco_mesh_compression":
                            _usesDraco = true;
                            break;
                        case "KHR_lights_punctual":
                            _usesLights = true;
                            break;
                    }
                }
            }

            nodes = gltf.GetChildWithKey(_nodes_token);
            accessors = gltf.GetChildWithKey(_accessors_token);
            bufferViews = gltf.GetChildWithKey(_bufferViews_token);
            textures = gltf.GetChildWithKey(_textures_token);
            images = gltf.GetChildWithKey(_images_token);
            samplers = gltf.GetChildWithKey(_samplers_token);

            if (_reader.AssetLoaderContext.Options.ImportTextures &&
                textures.Valid &&
                images.Valid)
            {
                _textures = new List<ITexture>(textures.Count);
                for (var i = 0; i < textures.Count; ++i)
                {
                    var gltfTexture = ConvertTexture(i);
                    _textures.Add(gltfTexture);

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertTextures);
            }

            if (_reader.AssetLoaderContext.Options.ImportMaterials && gltf.TryGetChildWithKey(_materials_token, out materials))
            {
                _materials = new List<IMaterial>(materials.Count);
                for (var i = 0; i < materials.Count; i++)
                {
                    var gltfMaterial = ConvertMaterial(i);
                    _materials.Add(gltfMaterial);

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertMaterials);
            }

            if (_reader.AssetLoaderContext.Options.ImportMeshes && gltf.TryGetChildWithKey(_meshes_token, out meshes))
            {
                _geometryGroups = new List<IGeometryGroup>(meshes.Count);
                for (var i = 0; i < meshes.Count; i++)
                {
                    var geometryGroup = ConvertGeometryGroup(i);
                    _geometryGroups.Add(geometryGroup);

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertGeometryGroups);
            }

            if (_reader.AssetLoaderContext.Options.ImportCameras && gltf.TryGetChildWithKey(_cameras_token, out cameras))
            {
                _cameras = new List<ICamera>(cameras.Count);
            }

            if (_reader.AssetLoaderContext.Options.ImportLights && gltf.TryGetChildWithKey(_extensions_token, out JsonParser.JsonValue extensions) &&
                extensions.TryGetChildWithKey(_KHR_lights_punctual_token, out JsonParser.JsonValue lightsPunctual) &&
                lightsPunctual.TryGetChildWithKey(_lights_token, out lights))
            {
                _lights = new List<ILight>(lights.Count);
            }

            var rootModel = new GltfRootModel { Visibility = true };
            if (gltf.TryGetChildWithKey(_scenes_token, out scenes))
            {
                _models = new Dictionary<int, GltfModel>(); //todo: per scene
                rootModel.Children = new List<IModel>(scenes.Count);
                for (var i = 0; i < scenes.Count; i++)
                {
                    var gltfScene = ConvertScene(i);
                    rootModel.Children.Add(gltfScene);

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertScenes);
            }


            if (_reader.AssetLoaderContext.Options.AnimationType != AnimationType.None && gltf.TryGetChildWithKey(_skins_token, out skins))
            {
                _skins = new Dictionary<int, Matrix4x4>[skins.Count];
                for (var i = 0; i < skins.Count; i++)
                {
                    var bindPoses = ConvertBindPoses(i);
                    _skins[i] = bindPoses;

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertBindPoses);
            }

            if (scenes.Valid && nodes.Valid)
            {
                for (var i = 0; i < nodes.Count; i++)
                {
                    PostProcessGltfModel(i);
                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.PostProcessModels);
            }

            if (_reader.AssetLoaderContext.Options.AnimationType != AnimationType.None && gltf.TryGetChildWithKey(_animations_token, out animations))
            {
                _animations = new List<IAnimation>(animations.Count);
                for (var a = 0; a < animations.Count; a++)
                {
                    var gltfAnimation = ConvertAnimation(a);
                    _animations.Add(gltfAnimation);

                }

                _reader.UpdateLoadingPercentage(1f, (int)GltfReader.ProcessingSteps.ConvertAnimations);
            }

            rootModel.AllAnimations = _animations;
            rootModel.AllMaterials = _materials;
            rootModel.AllTextures = _textures;
            rootModel.AllGeometryGroups = _geometryGroups;
            rootModel.AllModels = new List<IModel>(_models.Values);
            rootModel.AllCameras = _cameras;
            rootModel.AllLights = _lights;

            if (_quantitized)
            {
                rootModel.LocalScale = new Vector3(100f, 100f, 100f);
            }

            return rootModel;
        }

        private float CandelaToLux(float candelas, float distance)
        {
            var illuminance = candelas / (distance * distance);
            return illuminance;
        }

        private IModel ConvertModel(IModel parent, int nodeIndex)
        {
            var node = nodes.GetArrayValueAtIndex(nodeIndex);
            var cameraIndex = 0;
            var lightIndex = 0;
            var hasCamera = _reader.AssetLoaderContext.Options.ImportCameras && node.TryGetChildValueAsInt(_camera_token, out cameraIndex, _temporaryString);
            var hasLight = _reader.AssetLoaderContext.Options.ImportLights && node.TryGetChildWithKey(_extensions_token, out JsonParser.JsonValue extensions) &&
                           extensions.TryGetChildWithKey(_KHR_lights_punctual_token, out JsonParser.JsonValue lightsPunctual) &&
                           lightsPunctual.TryGetChildValueAsInt(_light_token, out lightIndex, _temporaryString);
            Matrix4x4 matrix;
            Vector3 localPosition;
            Quaternion localRotation;
            Vector3 localScale;
            if (node.TryGetChildWithKey(_matrix_token, out var rawMatrix))
            {
                matrix = ConvertMatrix(rawMatrix);
            }
            else
            {
                localPosition = node.TryGetChildWithKey(_translation_token, out var translation)
                    ? ConvertVector3(translation)
                    : Vector3.zero;
                localRotation = node.TryGetChildWithKey(_rotation_token, out var rotation)
                    ? ConvertRotation(rotation)
                    : Quaternion.identity;
                localScale = node.TryGetChildWithKey(_scale_token, out var scale) ? ConvertVector3(scale) : Vector3.one;
                matrix = Matrix4x4.TRS(
                    localPosition,
                    localRotation,
                    localScale
                );
            }
            matrix = RightHandToLeftHandConverter.ConvertMatrix(matrix);
            matrix.Decompose(out localPosition, out localRotation, out localScale);
            localPosition = TransformVector(localPosition);
            if (hasCamera || hasLight)
            {
                localRotation *= Quaternion.Euler(0f, 180f, 0f);
            }
            var model = hasCamera ? new GltfCamera() : hasLight ? new GltfLight() : new GltfModel();
            model.Name = node.GetChildValueAsString(_name_token, _temporaryString);
            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.Name = hasCamera ? $"Camera{cameraIndex}" : hasLight ? $"Light{lightIndex}" : "Model";
            }

            model.LocalPosition = localPosition;
            model.LocalRotation = localRotation;
            model.LocalScale = localScale;
            model.Visibility = true;
            model.Parent = parent;
            if (_reader.AssetLoaderContext.Options.UserPropertiesMapper != null && node.TryGetChildWithKey(_extras_token, out var extras))
            {
                if (model.UserProperties == null)
                {
                    model.UserProperties = new Dictionary<string, object>(extras.Count);
                }
                var enumerator = new JsonParser.JsonValue.JsonKeyValueEnumerator(extras);
                while (enumerator.MoveNext())
                {
                    var keyValuePair = enumerator.Current;
                    model.UserProperties.Add(keyValuePair.Item1, keyValuePair.Item2);
                }
            }
            if (node.TryGetChildValueAsInt(_mesh_token, out var geometryMesh, _temporaryString) && _geometryGroups != null)
            {
                model.GeometryGroup = _geometryGroups[geometryMesh];
                var geometries = model.GeometryGroup.GeometriesData.Values;
                if (_materials != null && geometries.Count > 0)
                {
                    var materialIndices = new int[geometries.Count];
                    foreach (var gltfGeometry in geometries)
                    {
                        var geometryIndex = gltfGeometry.Index;
                        var materialIndex = gltfGeometry.MaterialIndex;
                        if (materialIndex >= 0 && materialIndex < _materials.Count)
                        {
                            materialIndices[geometryIndex] = materialIndex;
                        }
                        else
                        {
                            materialIndices[geometryIndex] = -1;
                        }
                    }

                    model.MaterialIndices = materialIndices;
                }
            }

            if (hasCamera && hasLight)
            {
                if (_reader.AssetLoaderContext.Options.ShowLoadingWarnings)
                {
                    Debug.LogWarning("TriLib can't load gLTF2 objects containing both lights and cameras.");
                }
            }
            else
            {
                if (hasCamera)
                {
                    var gltfCamera = (GltfCamera)model;
                    var camera = cameras.GetArrayValueAtIndex(cameraIndex);
                    var cameraType = camera.GetChildValueAsString(_type_token, _temporaryString);
                    if (cameraType == "perspective")
                    {
                        gltfCamera.Ortographic = false;
                        if (camera.TryGetChildWithKey(_perspective_token, out var perspective))
                        {
                            if (perspective.TryGetChildValueAsFloat(_aspectRatio_token, out var aspectRatio, _temporaryString))
                            {
                                gltfCamera.AspectRatio = aspectRatio;
                            }

                            if (perspective.TryGetChildValueAsFloat(_yfov_token, out var yFov, _temporaryString))
                            {
                                gltfCamera.FieldOfView = yFov * Mathf.Rad2Deg;
                            }

                            if (perspective.TryGetChildValueAsFloat(_znear_token, out var zNear, _temporaryString))
                            {
                                gltfCamera.NearClipPlane = zNear / 100f;
                            }

                            if (perspective.TryGetChildValueAsFloat(_zfar_token, out var zFar, _temporaryString))
                            {
                                gltfCamera.FarClipPlane = zFar / 100f;
                            }
                        }
                    }
                    else if (cameraType == "orthographic")
                    {
                        gltfCamera.Ortographic = true;
                        if (camera.TryGetChildWithKey(_orthographic_token, out var ortographic))
                        {
                            if (ortographic.TryGetChildValueAsFloat(_xmag_token, out var xMag, _temporaryString))
                            {
                                gltfCamera.XMag = xMag;
                            }

                            if (ortographic.TryGetChildValueAsFloat(_ymag_token, out var yMag, _temporaryString))
                            {
                                gltfCamera.YMag = yMag;
                            }

                            if (ortographic.TryGetChildValueAsFloat(_znear_token, out var zNear, _temporaryString))
                            {
                                gltfCamera.NearClipPlane = zNear / 100f;
                            }

                            if (ortographic.TryGetChildValueAsFloat(_zfar_token, out var zFar, _temporaryString))
                            {
                                gltfCamera.FarClipPlane = zFar / 100f;
                            }
                        }

                        gltfCamera.OrtographicSize = gltfCamera.XMag / gltfCamera.YMag; //todo: check this
                    }

                    _cameras.Add(gltfCamera);
                }
                else if (hasLight)
                {
                    var gltfLight = (GltfLight)model;
                    var light = lights.GetArrayValueAtIndex(lightIndex);
                    if (light.TryGetChildWithKey(_color_token, out JsonParser.JsonValue colorToken))
                    {
                        gltfLight.Color = ConvertColor(colorToken);
                    }

                    var intensitySet = false;

                    if (light.TryGetChildValueAsFloat(_intensity_token, out var intensity, _temporaryString))
                    {
                        gltfLight.Intensity = intensity;
                        intensitySet = true;
                    }

                    if (light.TryGetChildValueAsFloat(_range_token, out var range, _temporaryString))
                    {
                        gltfLight.Range = range;
                    }

                    if (light.TryGetChildWithKey(_spot_token, out JsonParser.JsonValue spotToken))
                    {
                        if (spotToken.TryGetChildValueAsFloat(_innerConeAngle_token, out var innerConeAngle, _temporaryString))
                        {
                            gltfLight.InnerSpotAngle = innerConeAngle * Mathf.Rad2Deg * 2f;
                        }

                        if (spotToken.TryGetChildValueAsFloat(_outerConeAngle_token, out var outerConeAngle, _temporaryString))
                        {
                            gltfLight.OuterSpotAngle = outerConeAngle * Mathf.Rad2Deg * 2f;
                        }
                    }

                    if (light.TryGetChildValueAsString(_type_token, out var lightType, _temporaryString))
                    {
                        switch (lightType)
                        {
                            case "directional":
                                gltfLight.LightType = LightType.Directional;
                                if (intensitySet)
                                    gltfLight.Intensity = CandelaToLux(gltfLight.Intensity, gltfLight.Range == 0f ? GltfReader.SpotLightDistance : gltfLight.Range);
                                break;
                            case "spot":
                                gltfLight.LightType = LightType.Spot;
                                break;
                            case "point":
                                gltfLight.LightType = LightType.Point;
                                if (intensitySet)
                                    gltfLight.Intensity = CandelaToLux(gltfLight.Intensity, gltfLight.Range == 0f ? GltfReader.SpotLightDistance : gltfLight.Range);
                                break;
                        }
                    }

                    _lights.Add(gltfLight);
                }
            }

            if (node.TryGetChildWithKey(_children_token, out var nodeChildren))
            {
                model.Children = new List<IModel>(nodeChildren.Count);
                for (var j = 0; j < nodeChildren.Count; j++)
                {
                    var childNodeIndex = nodeChildren.GetArrayValueAtIndex(j).GetValueAsInt(_temporaryString);
                    model.Children.Add(ConvertModel(model, childNodeIndex));
                }
            }

            _models.Add(nodeIndex, model);
            return model;
        }

        private void PostProcessGltfModel(int index)
        {
            var node = nodes.GetArrayValueAtIndex(index);
            if (_models.TryGetValue(index, out var model))
            {
                if (node.TryGetChildValueAsInt(_skin_token, out var skin, _temporaryString) && _skins != null)
                {
                    var bindPoses = _skins[skin];
                    var bones = new List<IModel>(bindPoses.Count);
                    var matrices = new Matrix4x4[bindPoses.Count];
                    var i = 0;
                    foreach (var bindPose in bindPoses)
                    {
                        var skinModel = _models[bindPose.Key];
                        skinModel.IsBone = true;
                        bones.Add(skinModel);
                        matrices[i] = bindPose.Value;
                        i++;
                    }

                    model.Bones = bones;
                    model.BindPoses = matrices;
                }
            }
        }
    }
}