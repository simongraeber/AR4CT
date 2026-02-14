using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXAnimatedMatrices : FBXMatrices
    {
        private readonly FBXAnimationCurve.KeyTangent[,] _tangentsAndWeights = new FBXAnimationCurve.KeyTangent[(int)FBXMatrixType.Max, 3];

        public FBXAnimationCurve.KeyTangent GetTangentsAndWeights(FBXMatrixType matrixType, int fieldIndex)
        {
            var type = (int)matrixType;
            return _tangentsAndWeights[type, fieldIndex];
        }

        public void UpdateField(FBXMatrixType matrixType, int fieldIndex, float value, FBXAnimationCurve.KeyTangent tangentsAndWeights, bool includeModel = true)
        {
            UpdateField(matrixType, fieldIndex, value, includeModel);
            _tangentsAndWeights[(int)matrixType, fieldIndex] = tangentsAndWeights;
        }

        public void CopyTo(FBXAnimatedMatrices animatedMatrices)
        {
            base.CopyTo(animatedMatrices);
            for (var i = 0; i < _tangentsAndWeights.GetLength(0); i++)
            {
                for (var j = 0; j < _tangentsAndWeights.GetLength(1); j++)
                {
                    animatedMatrices._tangentsAndWeights[i, j] = _tangentsAndWeights[i, j];
                }
            }
        }

        public override void Reset(FBXModel model)
        {
            base.Reset(model);
            for (var i = 0; i < (int)FBXMatrixType.Max; i++)
            {
                for (var j = 0; j < 3; j++)
                {
                    _tangentsAndWeights[i, j] = default;
                }
            }
        }
    }
}