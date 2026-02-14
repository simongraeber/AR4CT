using System;
using System.Collections.Generic;
using TriLibCore.Collections;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXBlendShapeChannels
    {
        public const int MaxChannels = 2048;

        private readonly float[] _matrices = new float[MaxChannels];
        private readonly bool[] _fieldsWithValues = new bool[MaxChannels];
        private readonly FBXAnimationCurve.KeyTangent[] _tangentsAndWeights = new FBXAnimationCurve.KeyTangent[MaxChannels];

        public Dictionary<string, int> FieldIndices { get; } = new Dictionary<string, int>(MaxChannels);
        
        private int _currentFieldIndex;

        public int GetChannelIndex(string channelName)
        {
            if (!FieldIndices.TryGetValue(channelName, out var fieldIndex))
            {
                fieldIndex = _currentFieldIndex++;
                FieldIndices.Add(channelName, fieldIndex);
            }
            return fieldIndex;
        }

        public FBXAnimationCurve.KeyTangent GetTangentsAndWeights(int channelIndex)
        {
            return _tangentsAndWeights[channelIndex];
        }

        public void Update(int channelIndex, float matrix, FBXAnimationCurve.KeyTangent tangentsAndWeights)
        {
            _fieldsWithValues[channelIndex] = true;
            _matrices[channelIndex] = matrix;
            _tangentsAndWeights[channelIndex] = tangentsAndWeights;
        }

        public bool HasMatrix(int channelIndex)
        {
            return _fieldsWithValues[channelIndex];
        }

        public float GetMatrix(int channelIndex)
        {
            var hasLocalMatrix = HasMatrix(channelIndex);
            var localMatrix = _matrices[channelIndex];
            return hasLocalMatrix ? localMatrix : 0f;
        }

        public void Load(FBXBlendShapeChannels matrices)
        {
            Array.Copy(matrices._matrices, 0, _matrices, 0, (int)FBXMatrixType.Max);
        }

        public void Reset()
        {
            for (var i = 0; i < MaxChannels; i++)
            {
                _matrices[i] = 0f;
                _fieldsWithValues[i] = false;
                _tangentsAndWeights[i] = default;
            }
            FieldIndices.Clear();
            _currentFieldIndex = 0;
        }
    }
}