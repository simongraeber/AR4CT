using System.Collections.Generic;
using System.Linq;
using TriLibCore.Extensions;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXModel : FBXObject, IModel
    {
        public FBXRotationOrder RotationOrder; 

        public FBXInheritType InheritType; 

        public bool Visibility { get; set; } 

        public bool VisibilityInheritance; 

        public readonly FBXMatrices Matrices;

        private Vector3 _localPosition;

        public Vector3 Pivot { get; set; }

        public Vector3 LocalPosition
        {
            get => _localPosition;
            set => _localPosition = value;
        }

        private Quaternion _localRotation;

        public Quaternion LocalRotation
        {
            get => _localRotation;
            set => _localRotation = value;
        }

        private Vector3 _localScale;

        public Vector3 LocalScale
        {
            get => _localScale;
            set => _localScale = value;
        }

        public bool IsBone { get; set; }

        public IGeometryGroup GeometryGroup
        {
            get
            {
                return Mesh?.InnerGeometryGroup;
            }
            set
            {
                if (Mesh != null) Mesh.InnerGeometryGroup = value;
            }
        }

        private IModel _parent;

        public IModel Parent
        {
            get => _parent;
            set
            {
                var fbxModel = (FBXModel)value;
                if (fbxModel.Children == null)
                {
                    fbxModel.Children = new List<IModel>(fbxModel.ChildrenCount);
                }
                fbxModel.Children.Add(this);
                _parent = fbxModel;
            }
        }

        public List<IModel> Children { get; set; }
        public int ChildrenCount;

        public List<IModel> Bones { get; set; }
        public int BonesCount;

        public IList<Matrix4x4> BindPosesList;

        public Matrix4x4[] BindPoses { get; set; }

        public int[] MaterialIndices { get; set; }

        public Dictionary<string, object> UserProperties { get; set; }
        public bool HasCustomPivot { get; set; }
        public Matrix4x4 OriginalGlobalMatrix { get; set; }
        public IFBXMesh Mesh { get; set; }

        public List<int> AllMaterialIndices;

        public int AllMaterialIndicesCount;

        public FBXTexture DiffuseTexture;

        public virtual Matrix4x4 GetLocalMatrix()
        {
            return ModelExtensions.GetLocalMatrix(this);
        }

        public Matrix4x4 GetGlobalRotationMatrix(AssetLoaderContext assetLoaderContext)
        {
            var rotation = Matrix4x4.identity;
            var parent = (FBXModel)Parent;
            while (parent != Document)
            {
                rotation = FBXMatrices.ProcessRotationMatrix4x4(assetLoaderContext, parent.Matrices.GetMatrix(FBXMatrixType.LclRotation), RotationOrder) * rotation;
                parent = (FBXModel)parent.Parent;
            }
            return rotation;
        }
        
        public Matrix4x4 GetGlobalParentMatrix(AssetLoaderContext assetLoaderContext)
        {
            return Parent == Document ? Matrix4x4.identity : ((FBXModel)Parent).GetGlobalMatrix(assetLoaderContext);
        }

        public Matrix4x4 GetGlobalMatrix(AssetLoaderContext assetLoaderContext)
        {
            var globalMatrix = Matrices.ComputeMatrix(assetLoaderContext);
            var parent = Parent as FBXModel;
            while (parent != null && parent != Document)
            {
                var localMatrix = parent.Matrices.ComputeMatrix(assetLoaderContext);
                globalMatrix = localMatrix * globalMatrix;
                parent = parent.Parent as FBXModel;
            }
            return globalMatrix;
        }

        public FBXModel() : base()
        {

        }

        public FBXModel(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Model;
            Matrices = new FBXMatrices(this);
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        //todo: other properties?
        public sealed override void LoadDefinition()
        {
            if (Document.ModelDefinition != null)
            {
                RotationOrder = Document.ModelDefinition.RotationOrder;
                InheritType = Document.ModelDefinition.InheritType;
                Visibility = Document.ModelDefinition.Visibility;
            }
        }

        public void TransformMatrices(AssetLoaderContext assetLoaderContext)
        {
            Matrices.TransformMatrices(assetLoaderContext, out _localPosition, out _localRotation, out _localScale);
        }

        public void SetupBindPoses()
        {
            if (BindPosesList != null)
            {
                BindPoses = BindPosesList.ToArray();
                BindPosesList = null;
            }
        }

        public bool IsGeometryCompatible(FBXModel model)
        {
            return Matrices.GetMatrix(FBXMatrixType.GeometricTranslation) == model.Matrices.GetMatrix(FBXMatrixType.GeometricTranslation) &&
                   Matrices.GetMatrix(FBXMatrixType.GeometricRotation) == model.Matrices.GetMatrix(FBXMatrixType.GeometricRotation) &&
                   Matrices.GetMatrix(FBXMatrixType.GeometricScaling) == model.Matrices.GetMatrix(FBXMatrixType.GeometricScaling);
        }
    }
}