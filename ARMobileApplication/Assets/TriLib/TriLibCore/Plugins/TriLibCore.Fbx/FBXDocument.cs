using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.Fbx.Reader;
using TriLibCore.Interfaces;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXDocument : FBXRootModel
    {
        public readonly FBXMatrixBuffer MatrixBuffer = new FBXMatrixBuffer();
        public FBXAnimationCurve AnimationCurveDefinition;
        public FBXAnimationCurveNode AnimationCurveNodeDefinition;
        public IList<FBXAnimationCurveNode> AnimationCurveNodes;
        public int AnimationCurveNodesCount;
        public IList<FBXAnimationCurve> AnimationCurves;
        public int AnimationCurvesCount;
        public FBXAnimationLayer AnimationLayerDefinition;
        public IList<FBXAnimationLayer> AnimationLayers;
        public int AnimationLayersCount;
        public FBXAnimationStack AnimationStackDefinition;
        public int AnimationStacksCount;
        public FBXBlendShapeSubDeformer BlendShapeChannelDefinition;
        public FBXBlendShapeGeometryGroup BlendShapeGeometryGroupDefinition;
        public IList<FBXBlendShapeGeometryGroup> BlendShapeGeometryGroups;
        public int CamerasCount;
        public int ConnectedGeometriesCount;
        public FBXDeformer DeformerDefinition;
        public IList<FBXDeformer> Deformers;
        public int DeformersCount;
        public int GeometriesCount;
        public IFBXMesh GeometryGroupDefinition;
        public FBXGlobalSettings GlobalSettings;
        public bool IsBinary;
        public int LayeredTexturesCount;
        public int LightsCount;
        public FBXMaterial MaterialDefinition;
        public int MaterialsCount;
        public FBXModel ModelDefinition;
        public int ModelsCount;
        public bool NewTC;
        public FBXNodeAttribute NodeAttributeDefinition;
        public Dictionary<long, IFBXObject> Objects;
        public int ObjectsCount;
        public string OriginalApplicationName;
        public FBXPose PoseDefinition;
        public FbxReader Reader;
        public FBXSubDeformer SubDeformerDefinition;
        public IList<FBXSubDeformer> SubDeformers;
        public int SubDeformersCount;
        public FBXTexture TextureDefinition;
        public int TexturesCount;
        public int Version;
        public FBXVideo VideoDefinition;
        public int VideosCount;
        private const long FBX_TC_MILLISECOND = 46186158000L;
        private const long FBX_TC_MILLISECOND_NEW = 00141120000L;
        private Matrix4x4 _documentMatrix;
        private Quaternion _documentRotation;
        private Matrix4x4 _bakeMatrix;
        private Quaternion _bakeRotation;
        private Quaternion _bakeRotationInverse;

        public Matrix4x4 BakeMatrix { get { return _bakeMatrix; } }
        public bool IsRightHanded { get; private set; }

        public FBXDocument()
        {

        }

        public FBXDocument(bool isBinary, FBXNode node)
        {
            IsBinary = isBinary;
        }
        public float ConvertFromFBXTime(long time)
        {
            return (float)time / (Version >= 7700 && NewTC ? FBX_TC_MILLISECOND_NEW : FBX_TC_MILLISECOND);
        }

        public void ConvertMatrix(ref Vector3 translation, ref Quaternion rotation, ref Vector3 scale, IModel model = null, bool applyScale = true)
        {
            ApplyDocumentOrientation(ref translation, ref rotation, model);
            translation = ConvertVector(translation, applyScale);
            rotation = ConvertRotation(rotation);
        }

        public void ApplyDocumentOrientation(ref Vector3 translation, ref Quaternion rotation, IModel model)
        {
            if (model?.Parent == this && !Reader.AssetLoaderContext.Options.BakeAxisConversion)
            {
                translation = _documentMatrix.MultiplyPoint3x4(translation);
                rotation = _documentRotation * rotation;
            }
            if (Reader.AssetLoaderContext.Options.BakeAxisConversion)
            {
                translation = _bakeMatrix.MultiplyPoint3x4(translation);
                rotation = _bakeRotation * rotation * _bakeRotationInverse;
            }
        }
        public Matrix4x4 ConvertMatrixSimple(Matrix4x4 matrix, IModel model)
        {
            matrix.DecomposeSimple(out var translation, out var rotation, out var scale);
            ConvertMatrix(ref translation, ref rotation, ref scale, model, true);
            matrix = Matrix4x4.TRS(translation, rotation, scale);
            return matrix;
        }

        public Matrix4x4 ConvertMatrixComplex(Matrix4x4 matrix, IModel model)
        {
            matrix.Decompose(out var translation, out var rotation, out var scale);
            ConvertMatrix(ref translation, ref rotation, ref scale, model, true);
            matrix = Matrix4x4.TRS(translation, rotation, scale);
            return matrix;
        }

        public Quaternion ConvertRotation(Quaternion rotation, bool applyOrientation = true)
        {
            if (applyOrientation && IsRightHanded)
            {
                rotation = RightHandToLeftHandConverter.ConvertRotation(rotation);
            }
            return rotation;
        }

        public long ConvertToFBXTime(double time)
        {
            return (long)(time * (Version >= 7700 && NewTC ? FBX_TC_MILLISECOND_NEW : FBX_TC_MILLISECOND));
        }

        public Vector3 ConvertVector(Vector3 vertex, bool applyScale = false, bool applyOrientation = true)
        {
            if (applyScale)
            {
                if (Reader.AssetLoaderContext.Options.UseFileScale)
                {
                    vertex *= GetDocumentScale();
                }
                vertex *= Reader.AssetLoaderContext.Options.ScaleFactor;
            }
            if (applyOrientation && IsRightHanded)
            {
                vertex = RightHandToLeftHandConverter.ConvertVector(vertex);
            }
            return vertex;
        }

        public float GetDocumentScale()
        {
            return GlobalSettings.UnitScaleFactor / 100f;
        }

        public override Matrix4x4 GetLocalMatrix()
        {
            return Matrix4x4.identity;
        }

        public IFBXObject GetObjectById(long objectId)
        {
            if (Objects != null)
            {
                if (Objects.TryGetValue(objectId, out var fbxObject))
                {
                    return fbxObject;
                }
            }
            return null;
        }

        public bool ObjectExists(long id)
        {
            return Objects.ContainsKey(id);
        }

        public float Scale(float vertex, bool useFileScale, float scaleFactor)
        {
            if (useFileScale)
            {
                vertex *= GetDocumentScale();
            }
            vertex *= scaleFactor;
            return vertex;
        }

        public void Setup()
        {
            Objects = new Dictionary<long, IFBXObject>(ObjectsCount);
            AllModels = new List<IModel>(ModelsCount);
            AllMaterials = new List<IMaterial>(MaterialsCount);
            AllGeometryGroups = new List<IGeometryGroup>(GeometriesCount);
            AllMeshes = new List<IFBXMesh>(GeometriesCount);
            AllAnimations = new List<IAnimation>(AnimationStacksCount);
            AllTextures = new List<ITexture>(TexturesCount + LayeredTexturesCount);
            AllCameras = new List<ICamera>(CamerasCount);
            AllLights = new List<ILight>(LightsCount);
            Deformers = new List<FBXDeformer>(DeformersCount);
            SubDeformers = new List<FBXSubDeformer>(Mathf.Max(SubDeformersCount, DeformersCount));
            AnimationCurveNodes = new List<FBXAnimationCurveNode>(AnimationCurveNodesCount);
            AnimationCurves = new List<FBXAnimationCurve>(AnimationCurvesCount);
            AnimationLayers = new List<FBXAnimationLayer>(AnimationLayersCount);
            BlendShapeGeometryGroups = new List<FBXBlendShapeGeometryGroup>(GeometriesCount);
        }

        public void SetupCoordSystem()
        {
            var upVec = MathUtils.Axis[GlobalSettings.UpAxis] * GlobalSettings.UpAxisSign;
            var frontVec = MathUtils.Axis[GlobalSettings.FrontAxis] * GlobalSettings.FrontAxisSign;
            var rightVec = MathUtils.Axis[GlobalSettings.CoordAxis] * GlobalSettings.CoordAxisSign;
            _documentMatrix = Matrix4x4.identity;
            _documentMatrix.SetColumn(0, rightVec);
            _documentMatrix.SetColumn(1, upVec);
            _documentMatrix.SetColumn(2, frontVec);
            _documentMatrix = _documentMatrix.inverse;
            _documentRotation = _documentMatrix.rotation;

            var crossProduct = Vector3.Cross(upVec, frontVec);
            IsRightHanded = Vector3.Dot(crossProduct, rightVec) > 0f;

            _bakeMatrix = Matrix4x4.identity;
            if (Reader.AssetLoaderContext.Options.BakeAxisConversion)
            {
                if (IsRightHanded)
                {
                    _bakeMatrix = Matrix4x4.Rotate(Quaternion.Euler(0f, 180f, 0f));
                }
                _bakeMatrix *= _documentMatrix;
                _bakeRotation = Quaternion.LookRotation(
                    _bakeMatrix.GetColumn(2),
                    _bakeMatrix.GetColumn(1)
                );
                _bakeRotationInverse = Quaternion.Inverse(_bakeRotation);
            }
        }
    }
}
