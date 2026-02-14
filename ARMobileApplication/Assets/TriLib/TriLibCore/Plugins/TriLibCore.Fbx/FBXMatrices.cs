using System;
using System.Text;
using TriLibCore.Extensions;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXMatrices
    {
        private readonly Vector3[] _matrices = new Vector3[(int)FBXMatrixType.Max];
        private readonly bool[,] _fieldsWithValues = new bool[(int)FBXMatrixType.Max, 3];

        public FBXModel Model { get; private set; }

        public FBXMatrices()
        {

        }

        public FBXMatrices(FBXModel model)
        {
            Model = model;
        }

        public void Update(FBXMatrixType matrixType, Vector3 matrix)
        {
            _fieldsWithValues[(int)matrixType, 0] = true;
            _fieldsWithValues[(int)matrixType, 1] = true;
            _fieldsWithValues[(int)matrixType, 2] = true;
            _matrices[(int)matrixType] = matrix;
        }

        public float GetField(FBXMatrixType matrixType, int fieldIndex, bool includeModel = true)
        {
            var matrix = GetMatrix(matrixType, includeModel);
            return matrix[fieldIndex];
        }

        public void UpdateField(FBXMatrixType matrixType, int fieldIndex, float value, bool includeModel = true)
        {
            var matrix = GetMatrix(matrixType, includeModel);
            matrix[fieldIndex] = value;
            var type = (int)matrixType;
            _matrices[type] = matrix;
            _fieldsWithValues[type, fieldIndex] = true;
        }

        public bool HasField(FBXMatrixType matrixType, int fieldIndex)
        {
            return _fieldsWithValues[(int)matrixType, fieldIndex];
        }

        public bool HasMatrix(FBXMatrixType matrixType)
        {
            for (var i = 0; i < 3; i++)
            {
                if (HasField(matrixType, i))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 DefaultMatrixValue(FBXMatrixType matrixType)
        {
            switch (matrixType)
            {
                case FBXMatrixType.LclScaling:
                case FBXMatrixType.GeometricScaling:
                    return Vector3.one;
                default:
                    return Vector3.zero;
            }
        }

        public Vector3 GetMatrix(FBXMatrixType matrixType, bool includeModel = true)
        {
            var hasLocalMatrix = HasMatrix(matrixType);
            var localMatrix = _matrices[(int)matrixType];
            var defaultMatrix = DefaultMatrixValue(matrixType);
            if (includeModel)
            {
                var hasModelMatrix = Model.Matrices.HasMatrix(matrixType);
                var modelMatrix = Model.Matrices._matrices[(int)matrixType];
                var x =
                    hasLocalMatrix && HasField(matrixType, 0) ? localMatrix.x :
                    hasModelMatrix && Model.Matrices.HasField(matrixType, 0) ? modelMatrix.x :
                    defaultMatrix.x;
                var y =
                    hasLocalMatrix && HasField(matrixType, 1) ? localMatrix.y :
                    hasModelMatrix && Model.Matrices.HasField(matrixType, 1) ? modelMatrix.y :
                    defaultMatrix.y;
                var z =
                    hasLocalMatrix && HasField(matrixType, 2) ? localMatrix.z :
                    hasModelMatrix && Model.Matrices.HasField(matrixType, 2) ? modelMatrix.z :
                    defaultMatrix.z;
                return new Vector3(x, y, z);
            }
            return hasLocalMatrix ? localMatrix : defaultMatrix;
        }

        public void TransformMatrices(AssetLoaderContext assetLoaderContext, out Vector3 localPosition, out Quaternion localRotation, out Vector3 localScale)
        {
            var matrix = ComputeMatrix(assetLoaderContext);
            matrix.Decompose(out localPosition, out localRotation, out localScale);
        }

        public Matrix4x4 ComputeMatrix(AssetLoaderContext assetLoaderContext)
        {
            var lTranslation = GetMatrix(FBXMatrixType.LclTranslation);
            var lTranslationM = Matrix4x4.Translate(lTranslation);
            var lRotation = GetMatrix(FBXMatrixType.LclRotation);
            var lPreRotation = GetMatrix(FBXMatrixType.PreRotation);
            var lPostRotation = GetMatrix(FBXMatrixType.PostRotation);
            var lRotationM = ProcessRotationMatrix4x4(assetLoaderContext, lRotation, Model.RotationOrder);
            var lPreRotationM = ProcessRotationMatrix4x4(assetLoaderContext, lPreRotation, FBXRotationOrder.OrderXYZ);
            var lPostRotationMInverse = ProcessRotationMatrix4x4(assetLoaderContext, lPostRotation, FBXRotationOrder.OrderXYZ).inverse;
            var lScaling = GetMatrix(FBXMatrixType.LclScaling);
            var lScalingM = Matrix4x4.Scale(lScaling);
            var lScalingOffset = GetMatrix(FBXMatrixType.ScalingOffset);
            var lScalingPivot = GetMatrix(FBXMatrixType.ScalingPivot);
            var lRotationOffset = GetMatrix(FBXMatrixType.RotationOffset);
            var lRotationPivot = GetMatrix(FBXMatrixType.RotationPivot);
            var lScalingOffsetM = Matrix4x4.Translate(lScalingOffset);
            var lScalingPivotM = Matrix4x4.Translate(lScalingPivot);
            var lScalingPivotMinverse = Matrix4x4.Translate(-lScalingPivot);
            var lRotationOffsetM = Matrix4x4.Translate(lRotationOffset);
            var lRotationPivotM = Matrix4x4.Translate(lRotationPivot);
            var lRotationPivotMinverse = Matrix4x4.Translate(-lRotationPivot);

            var lTransform = Matrix4x4.identity
                * (lTranslationM)
                * (lRotationOffsetM)
                * (lRotationPivotM)
                * (lPreRotationM)
                * (lRotationM)
                * (lPostRotationMInverse)
                * (lRotationPivotMinverse)
                * (lScalingOffsetM)
                * (lScalingPivotM)
                * (lScalingM)
                * (lScalingPivotMinverse);
            return lTransform;
        }

        private static Matrix4x4 ProcessMatrix4x4(Quaternion q)
        {
            if (float.IsNaN(q.x) || float.IsNaN(q.y) || float.IsNaN(q.z) || float.IsNaN(q.w))
            {
                return Matrix4x4.identity;
            }
            return Matrix4x4.Rotate(q);
        }

        // todo: replace with matrix buffer
        public static Matrix4x4 ProcessRotationMatrix4x4(AssetLoaderContext assetLoaderContext, Vector3 matrix, FBXRotationOrder rotationOrder)
        {
            var rotationX = Quaternion.Euler(matrix.x, 0f, 0f);
            var rotationY = Quaternion.Euler(0f, matrix.y, 0f);
            var rotationZ = Quaternion.Euler(0f, 0f, matrix.z);
            switch (rotationOrder)
            {
                case FBXRotationOrder.OrderXZY:
                    return ProcessMatrix4x4(rotationY * rotationZ * rotationX);
                case FBXRotationOrder.OrderYZX:
                    return ProcessMatrix4x4(rotationX * rotationZ * rotationY);
                case FBXRotationOrder.OrderYXZ:
                    return ProcessMatrix4x4(rotationZ * rotationX * rotationY);
                case FBXRotationOrder.OrderZXY:
                    return ProcessMatrix4x4(rotationY * rotationX * rotationZ);
                case FBXRotationOrder.OrderZYX:
                    return ProcessMatrix4x4(rotationX * rotationY * rotationZ);
                case FBXRotationOrder.OrderXYZ:
                    return ProcessMatrix4x4(rotationZ * rotationY * rotationX);
                default:
                    if (assetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        Debug.LogWarning($"Matrix rotation type not supported:{rotationOrder}");
                    }
                    return Matrix4x4.identity;
            }
        }

        public float[] ProcessRotationMatrix(AssetLoaderContext assetLoaderContext, Vector3 matrix, FBXRotationOrder rotationOrder)
        {
            var rotationX = Quaternion.Euler(matrix.x, 0f, 0f);
            var rotationY = Quaternion.Euler(0f, matrix.y, 0f);
            var rotationZ = Quaternion.Euler(0f, 0f, matrix.z);
            switch (rotationOrder)
            {
                case FBXRotationOrder.OrderXZY:
                    return Model.Document.MatrixBuffer.Rotate(rotationY * rotationZ * rotationX);
                case FBXRotationOrder.OrderYZX:
                    return Model.Document.MatrixBuffer.Rotate(rotationX * rotationZ * rotationY);
                case FBXRotationOrder.OrderYXZ:
                    return Model.Document.MatrixBuffer.Rotate(rotationZ * rotationX * rotationY);
                case FBXRotationOrder.OrderZXY:
                    return Model.Document.MatrixBuffer.Rotate(rotationY * rotationX * rotationZ);
                case FBXRotationOrder.OrderZYX:
                    return Model.Document.MatrixBuffer.Rotate(rotationX * rotationY * rotationZ);
                case FBXRotationOrder.OrderXYZ:
                    return Model.Document.MatrixBuffer.Rotate(rotationZ * rotationY * rotationX);
                default:
                    if (assetLoaderContext.Options.ShowLoadingWarnings)
                    {
                        Debug.LogWarning($"Matrix rotation type not supported:{rotationOrder}");
                    }
                    return Model.Document.MatrixBuffer.Identity;
            }
        }

        public void Load(FBXMatrices matrices)
        {
            Array.Copy(matrices._matrices, 0, _matrices, 0, (int)FBXMatrixType.Max);
        }

        public virtual void Reset(FBXModel model)
        {
            Model = model;
            for (var i = 0; i < (int)FBXMatrixType.Max; i++)
            {
                _matrices[i] = DefaultMatrixValue((FBXMatrixType)i);
                _fieldsWithValues[i, 0] = false;
                _fieldsWithValues[i, 1] = false;
                _fieldsWithValues[i, 2] = false;
            }
        }

        public void CopyTo(FBXMatrices matrices)
        {
            for (var i = 0; i < _matrices.Length; i++)
            {
                matrices._matrices[i] = _matrices[i];
            }
            for (var i = 0; i < _fieldsWithValues.GetLength(0); i++)
            {
                for (var j = 0; j < _fieldsWithValues.GetLength(1); j++)
                {
                    matrices._fieldsWithValues[i, j] = _fieldsWithValues[i, j];
                }
            }
        }

        public override string ToString()
        {
            var result = new StringBuilder();
            for (var i = 0; i < (int)FBXMatrixType.Max; i++)
            {
                var matrixType = (FBXMatrixType)i;
                result.Append(matrixType.ToString()).Append(":").AppendLine(_matrices[i].ToString());
            }
            return result.ToString();
        }
    }
}