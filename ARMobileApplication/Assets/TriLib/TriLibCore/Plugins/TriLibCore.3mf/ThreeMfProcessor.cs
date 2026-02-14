using System.Collections.Generic;
using System.IO;
using System.Linq;
using IxMilia.ThreeMf;
using TriLibCore.Extensions;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using TriLibCore.ThreeMf.Reader;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.ThreeMf
{
    public class ThreeMfProcessor
    {
        private const int ColorGeometriesBegin = int.MaxValue / 3 * 2;
        private const int TextureGeometriesBegin = int.MaxValue / 3;
        private Dictionary<ThreeMfColorGroup, IList<Color>> _allConvertedColorGroups;
        private Dictionary<ThreeMfTexture2DGroup, IList<Vector2>> _allConvertedCoordinates;
        private Dictionary<ThreeMfBase, ThreeMfMaterial> _allMaterials;
        private Dictionary<ThreeMfTexture2D, ThreeMfMaterial> _allTextureMaterials;
        private Dictionary<ThreeMfTexture2D, ThreeMfTexture> _allTextures;
        private ThreeMfReader _reader;
        private ThreeMfRootModel _rootModel;

        public IRootModel Process(ThreeMfReader reader, Stream stream)
        {
            _reader = reader;
            var file = ThreeMfFile.Load(stream);
            reader.UpdateLoadingPercentage(1f, (int)ThreeMfReader.ProcessingSteps.Parsing);
            var createdModels = new List<IModel>(file.Models.Count);
            _rootModel = new ThreeMfRootModel();
            _rootModel.Name = "Root";
            _rootModel.LocalScale = Vector3.one;
            _rootModel.LocalRotation = Quaternion.identity;
            _rootModel.Visibility = true;
            _rootModel.Children = createdModels;
            _rootModel.AllModels = _rootModel.Children;
            _rootModel.File = file;
            _reader.ModelCount = file.Models.Count;
            for (var i = 0; i < file.Models.Count; i++)
            {
                var originalModel = file.Models[i];
                _allMaterials = new Dictionary<ThreeMfBase, ThreeMfMaterial>();
                _allConvertedColorGroups = new Dictionary<ThreeMfColorGroup, IList<Color>>();
                _allConvertedCoordinates = new Dictionary<ThreeMfTexture2DGroup, IList<Vector2>>();
                _allTextureMaterials = new Dictionary<ThreeMfTexture2D, ThreeMfMaterial>();
                _allTextures = new Dictionary<ThreeMfTexture2D, ThreeMfTexture>();
                if (reader.AssetLoaderContext.Options.ImportMaterials)
                {
                    var originalMaterials = originalModel.Resources.Where(x => x.GetType() == typeof(ThreeMfBaseMaterials)).Select(x => x as ThreeMfBaseMaterials);
                    
                    foreach (var originalMaterial in originalMaterials)
                    {
                        
                        foreach (var originalBase in originalMaterial.Bases)
                        {
                            ConvertMaterial(originalBase);
                        }
                    }
                }
                reader.UpdateLoadingPercentage(1, i + 1, 5);
                if (reader.AssetLoaderContext.Options.ImportColors)
                {
                    var originalColorGroups = originalModel.Resources.Where(x => x.GetType() == typeof(ThreeMfColorGroup)).Select(x => x as ThreeMfColorGroup);
                    
                    foreach (var colorGroup in originalColorGroups)
                    {
                        var colors = new Color[colorGroup.Colors.Count];
                        for (var j = 0; j < colors.Length; j++)
                        {
                            colors[j] = ConvertColor(colorGroup.Colors[j].Color);
                        }
                        _allConvertedColorGroups.Add(colorGroup, colors);
                    }
                }
                reader.UpdateLoadingPercentage(2, i + 1, 5);
                if (reader.AssetLoaderContext.Options.ImportTextures)
                {
                    var originalTextures = originalModel.Resources.Where(x => x.GetType() == typeof(ThreeMfTexture2D)).Select(x => x as ThreeMfTexture2D);
                    
                    foreach (var originalTexture in originalTextures)
                    {
                        ConvertTexture(originalTexture);
                    }
                }
                reader.UpdateLoadingPercentage(3, i + 1, 5);
                if (reader.AssetLoaderContext.Options.ImportTextures || reader.AssetLoaderContext.Options.ImportMaterials)
                {
                    var originalTexture2DGroups = originalModel.Resources.Where(x => x.GetType() == typeof(ThreeMfTexture2DGroup)).Select(x => x as ThreeMfTexture2DGroup);
                    
                    foreach (var texture2DGroup in originalTexture2DGroups)
                    {
                        var coordinates = new Vector2[texture2DGroup.Coordinates.Count];
                        for (var j = 0; j < coordinates.Length; j++)
                        {
                            coordinates[j] = ConvertUV(texture2DGroup.Coordinates[j]);
                        }
                        _allConvertedCoordinates.Add(texture2DGroup, coordinates);
                    }
                }
                reader.UpdateLoadingPercentage(4, i + 1, 5);
                var parentModel = new ThreeMfModel();
                parentModel.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData() { ModelName = originalModel.ToString() }, parentModel, reader.Name);
                parentModel.LocalScale = Vector3.one;
                parentModel.Visibility = true;
                parentModel.LocalRotation = Quaternion.Euler(270f, 0f, 0f);
                for (var j = 0; j < originalModel.Items.Count; j++)
                {
                    var item = originalModel.Items[j];
                    if (item.Object is ThreeMfObject threeMfObject)
                    {
                        ConvertObject(threeMfObject, item.Transform, parentModel);
                    }
                }
                reader.UpdateLoadingPercentage(5, i + 1, 5);
                createdModels.Add(parentModel);
            }
            return _rootModel;
        }

        private static Color ConvertColor(ThreeMfsRGBColor threeMfColor)
        {
            var color = new Color(threeMfColor.R / 255f, threeMfColor.G / 255f, threeMfColor.B / 255f, threeMfColor.A / 255f);
            return color;
        }

        private static Matrix4x4 ConvertMatrix(ThreeMfMatrix transform)
        {
            var matrix = new Matrix4x4
            {
                m00 = ConvertSingle(transform.M00),
                m01 = ConvertSingle(transform.M10),
                m02 = ConvertSingle(transform.M20),
                m03 = ConvertSingle(transform.M30),

                m10 = ConvertSingle(transform.M01),
                m11 = ConvertSingle(transform.M11),
                m12 = ConvertSingle(transform.M21),
                m13 = ConvertSingle(transform.M31),

                m20 = ConvertSingle(transform.M02),
                m21 = ConvertSingle(transform.M12),
                m22 = ConvertSingle(transform.M22),
                m23 = ConvertSingle(transform.M32),

                m30 = 0f,
                m31 = 0f,
                m32 = 0f,
                m33 = 1f,
            };
            return matrix;
        }

        private static float ConvertSingle(double value)
        {
            return (float)(value * ThreeMfReader.ThreeMfConversionPrecision);
        }

        private static Vector2 ConvertUV(ThreeMfTexture2DCoordinate threeMfTexture2DCoordinate)
        {
            var vector = new Vector2(ConvertSingle(threeMfTexture2DCoordinate.U), ConvertSingle(threeMfTexture2DCoordinate.V));
            return vector;
        }

        private static Vector3 ConvertVertex(ThreeMfVertex threeMfVertex)
        {
            var vector = new Vector3(ConvertSingle(threeMfVertex.X), ConvertSingle(threeMfVertex.Y), ConvertSingle(threeMfVertex.Z));
            vector = RightHandToLeftHandConverter.ConvertVector(vector);
            return vector;
        }

        private void ConvertMaterial(ThreeMfBase originalBase)
        {
            var material = new ThreeMfMaterial();
            material.Name = originalBase.Name;
            material.Index = _allMaterials.Count;
            var baseColor = originalBase.Color;
            var displayColor = ConvertColor(baseColor);
            material.AddProperty("displayColor", displayColor, false);
            _allMaterials.Add(originalBase, material);
            _rootModel.AllMaterials.Add(material);
        }

        private ThreeMfModel ConvertModel(ThreeMfObject originalObject, ThreeMfMatrix transform, ThreeMfModel parentModel)
        {
            IGeometryGroup geometryGroup;
            if (_reader.AssetLoaderContext.Options.ImportMeshes && originalObject.Mesh != null && originalObject.Mesh.Triangles.Count > 0)
            {
                var hasUV = false;
                var hasColors = false;
                var vertexCount = 0;
                for (var triangleIndex = 0; triangleIndex < originalObject.Mesh.Triangles.Count; triangleIndex++)
                {
                    var threeMfTriangle = originalObject.Mesh.Triangles[triangleIndex];
                    if (threeMfTriangle.PropertyResource != null)
                    {
                        if (_reader.AssetLoaderContext.Options.ImportMaterials && _reader.AssetLoaderContext.Options.ImportTextures && threeMfTriangle.PropertyResource is ThreeMfTexture2DGroup)
                        {
                            hasUV = true;
                        }
                        else if (_reader.AssetLoaderContext.Options.ImportColors && threeMfTriangle.PropertyResource is ThreeMfColorGroup)
                        {
                            hasColors = true;
                        }
                    }
                    vertexCount += 3;
                }
                geometryGroup = CommonGeometryGroup.Create(false, false, hasColors, hasUV, true, false, false, false);
                geometryGroup.Name = originalObject.Name;
                geometryGroup.Setup(_reader.AssetLoaderContext, vertexCount, 1);
                _rootModel.AllGeometryGroups.Add(geometryGroup);
                for (var triangleIndex = 0; triangleIndex < originalObject.Mesh.Triangles.Count; triangleIndex++)
                {
                    var threeMfTriangle = originalObject.Mesh.Triangles[triangleIndex];
                    ThreeMfBase threeMfBase = null;
                    ThreeMfTexture2D threeMfTexture2D = null;
                    var geometryIndex = 0;
                    Vector2 uv1 = default;
                    Vector2 uv2 = default;
                    Vector2 uv3 = default;
                    Color color1 = default;
                    Color color2 = default;
                    Color color3 = default;
                    if (threeMfTriangle.PropertyResource != null)
                    {
                        if (_reader.AssetLoaderContext.Options.ImportMaterials && threeMfTriangle.PropertyResource is ThreeMfBaseMaterials baseMaterials)
                        {
                            geometryIndex = threeMfTriangle.V1PropertyIndex.GetValueOrDefault();
                            threeMfBase = baseMaterials.Bases[geometryIndex];
                        }
                        else if (_reader.AssetLoaderContext.Options.ImportMaterials && _reader.AssetLoaderContext.Options.ImportTextures && threeMfTriangle.PropertyResource is ThreeMfTexture2DGroup texture2DGroup)
                        {
                            threeMfTexture2D = texture2DGroup.Texture;
                            var vertexUvIndexA = threeMfTriangle.V1PropertyIndex.GetValueOrDefault();
                            var vertexUvIndexB = threeMfTriangle.V2PropertyIndex.GetValueOrDefault();
                            var vertexUvIndexC = threeMfTriangle.V3PropertyIndex.GetValueOrDefault();
                            var convertedCoordinates = _allConvertedCoordinates[texture2DGroup];
                            uv1 = convertedCoordinates[vertexUvIndexA];
                            uv2 = convertedCoordinates[vertexUvIndexB];
                            uv3 = convertedCoordinates[vertexUvIndexC];
                            geometryIndex = TextureGeometriesBegin + threeMfTexture2D.Id;
                        }
                        else if (_reader.AssetLoaderContext.Options.ImportColors && threeMfTriangle.PropertyResource is ThreeMfColorGroup colorGroup)
                        {
                            var vertexColorIndexA = threeMfTriangle.V1PropertyIndex.GetValueOrDefault();
                            var convertedColors = _allConvertedColorGroups[colorGroup];
                            color1 = convertedColors[vertexColorIndexA];
                            if (threeMfTriangle.V2PropertyIndex.HasValue)
                            {
                                var vertexColorIndexB = threeMfTriangle.V2PropertyIndex.Value;
                                color2 = convertedColors[vertexColorIndexB];
                            }
                            else
                            {
                                color2 = color1;
                            }
                            if (threeMfTriangle.V3PropertyIndex.HasValue)
                            {
                                var vertexColorIndexC = threeMfTriangle.V3PropertyIndex.Value;
                                color3 = convertedColors[vertexColorIndexC];
                            }
                            else
                            {
                                color3 = color1;
                            }
                            geometryIndex = ColorGeometriesBegin;
                        }
                    }
                    var vertex1 = ConvertVertex(threeMfTriangle.V1) * _reader.AssetLoaderContext.Options.ScaleFactor;
                    var vertex2 = ConvertVertex(threeMfTriangle.V2) * _reader.AssetLoaderContext.Options.ScaleFactor;
                    var vertex3 = ConvertVertex(threeMfTriangle.V3) * _reader.AssetLoaderContext.Options.ScaleFactor;
                    var geometry = GetActiveGeometry(geometryGroup, geometryIndex, false);
                    geometry.ThreeMfBase = threeMfBase;
                    geometry.ThreeMfTexture2D = threeMfTexture2D;
                    geometry.AddVertex(_reader.AssetLoaderContext,
                        geometryGroup.VerticesDataCount,
                        position: vertex3,
                        normal: default,
                        tangent: default,
                        color: color3,
                        uv0: uv3, uv1: new Vector2(triangleIndex, 0f));
                    geometry.AddVertex(_reader.AssetLoaderContext,
                        geometryGroup.VerticesDataCount,
                        position: vertex2,
                        normal: default,
                        tangent: default,
                        color: color2,
                        uv0: uv2, uv1: new Vector2(triangleIndex, 0f));
                    geometry.AddVertex(_reader.AssetLoaderContext,
                        geometryGroup.VerticesDataCount,
                        position: vertex1,
                        normal: default,
                        tangent: default,
                        color: color1,
                        uv0: uv1, uv1: new Vector2(triangleIndex, 0f));
                }
            }
            else
            {
                geometryGroup = null;
            }
            var model = new ThreeMfModel();
            model.Name = _reader.MapName(_reader.AssetLoaderContext, new ModelNamingData() { ModelName = originalObject.Name, PartNumber = originalObject.PartNumber, Id = originalObject.Id.ToString() }, model, _reader.Name);
            model.Visibility = true;
            var matrix = ConvertMatrix(transform);
            matrix = RightHandToLeftHandConverter.ConvertMatrix(matrix);
            matrix.Decompose(out var localPosition, out var localRotation, out var localScale);
            model.LocalPosition = localPosition * _reader.AssetLoaderContext.Options.ScaleFactor;
            model.LocalRotation = localRotation;
            model.LocalScale = localScale;
            model.Parent = parentModel;
            if (geometryGroup != null)
            {
                var materialIndices = new int[geometryGroup.GeometriesData.Count];
                foreach (ThreeMfGeometry geometry in geometryGroup.GeometriesData.Values)
                {
                    var geometryIndex = geometry.Index;
                    if (geometry.ThreeMfBase != null)
                    {
                        materialIndices[geometryIndex] = _allMaterials[geometry.ThreeMfBase].Index;
                    }
                    else if (geometry.ThreeMfTexture2D != null)
                    {
                        if (!_allTextureMaterials.TryGetValue(geometry.ThreeMfTexture2D, out var material))
                        {
                            material = ConvertTextureMaterial(geometry);
                        }
                        materialIndices[geometryIndex] = material.Index;
                    }
                    else
                    {
                        materialIndices[geometryIndex] = -1;
                    }
                    geometry.Index = geometryIndex;
                }
                model.GeometryGroup = geometryGroup;
                model.MaterialIndices = materialIndices;
            }
            parentModel.Children.Add(model);
            return model;
        }

        private void ConvertObject(ThreeMfObject threeMfObject, ThreeMfMatrix transform, ThreeMfModel parentModel)
        {
            var model = ConvertModel(threeMfObject, transform, parentModel);
            if (threeMfObject.Components != null)
            {
                for (var i = 0; i < threeMfObject.Components.Count; i++)
                {
                    var component = threeMfObject.Components[i];
                    if (component.Object is ThreeMfObject childThreeMfObject)
                    {
                        ConvertObject(childThreeMfObject, component.Transform, model);
                    }
                }
            }
        }

        private void ConvertTexture(ThreeMfTexture2D originalTexture)
        {
            var texture = new ThreeMfTexture();
            texture.Name = originalTexture.Id.ToString();
            texture.Data = originalTexture.TextureBytes;
            texture.WrapModeU = originalTexture.TileStyleU == ThreeMfTileStyle.Clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            texture.WrapModeV = originalTexture.TileStyleV == ThreeMfTileStyle.Clamp ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
            _allTextures.Add(originalTexture, texture);
            _rootModel.AllTextures.Add(texture);
        }

        private ThreeMfMaterial ConvertTextureMaterial(ThreeMfGeometry geometry)
        {
            var material = new ThreeMfMaterial();
            material.Name = geometry.ThreeMfTexture2D.Id.ToString();
            material.Index = _rootModel.AllMaterials.Count;
            material.AddProperty("diffuseTex", _allTextures[geometry.ThreeMfTexture2D], true);
            _allTextureMaterials.Add(geometry.ThreeMfTexture2D, material);
            _rootModel.AllMaterials.Add(material);
            return material;
        }

        private ThreeMfGeometry GetActiveGeometry(IGeometryGroup geometryGroup, int finalIndex, bool isQuad)
        {
            var geometry = geometryGroup.GetGeometry<ThreeMfGeometry>(_reader.AssetLoaderContext, finalIndex, isQuad, false);
            return geometry;
        }
    }
}