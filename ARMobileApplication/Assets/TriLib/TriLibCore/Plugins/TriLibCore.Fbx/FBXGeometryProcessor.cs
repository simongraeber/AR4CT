using System;
using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXGeometryProcessor
    {
        private readonly float[] _elements;
        private readonly int _elementsCount;
        private readonly PropertyAccessorFloat _values;
        private readonly PropertyAccessorFloat _weights;
        private readonly PropertyAccessorInt _indices;
        private readonly bool _byIndexMapping;
        private readonly FBXMeshMappingType _mappingType;

        public FBXGeometryProcessor(PropertyAccessorFloat values, PropertyAccessorInt indices, PropertyAccessorFloat weights, int elementsCount, bool byIndexMapping, FBXMeshMappingType mappingType)
        {
            _values = values;
            _indices = indices;
            _weights = weights;
            _elementsCount = elementsCount;
            _elements = new float[_elementsCount];
            _byIndexMapping = byIndexMapping;
            _mappingType = mappingType;
        }

        public int ElementsCount => _values.Count / _elementsCount;

        public IList<float> GetValues(int vertexIndex, int polygonIndex, int polygonVertexIndex)
        {
            switch (_mappingType)
            {
                case FBXMeshMappingType.ByVertex:
                    return GetValuesByVertex(vertexIndex);
                case FBXMeshMappingType.ByPolygon:
                    return GetValuesByPolygon(polygonIndex);
                case FBXMeshMappingType.ByPolygonVertex:
                    return GetValuesByPolygonVertex(polygonVertexIndex);
                case FBXMeshMappingType.AllSame:
                    return GetValuesAllSame();
                default:
                    throw new Exception("Invalid FBX mesh layer mapping type.");
            }
        }

        public float GetWeight(int vertexIndex, int polygonIndex, int polygonVertexIndex)
        {
            switch (_mappingType)
            {
                case FBXMeshMappingType.ByVertex:
                    return GetWeightByVertex(vertexIndex);
                case FBXMeshMappingType.ByPolygon:
                    return GetWeightByPolygon(polygonIndex);
                case FBXMeshMappingType.ByPolygonVertex:
                    return GetWeightByPolygonVertex(polygonVertexIndex);
                case FBXMeshMappingType.AllSame:
                    return GetWeightAllSame();
                default:
                    throw new Exception("Invalid FBX mesh layer mapping type.");
            }
        }

        private int FixIndex(int index)
        {
            return Mathf.Clamp(index, 0, _values.Count - 1);
        }

        private int FixWeightIndex(int index)
        {
            return Mathf.Clamp(index, 0, _weights.Count - 1);
        }

        private IList<float> GetValuesByVertex(int vertexIndex)
        {
            Array.Clear(_elements, 0, _elementsCount);
            var baseIndex = _byIndexMapping ? _indices[vertexIndex] * _elementsCount : vertexIndex * _elementsCount;
            for (var i = 0; i < _elementsCount; i++)
            {
                var index = baseIndex + i;
                index = FixIndex(index);
                _elements[i] = _values[index];
            }
            return _elements;
        }

        private IList<float> GetValuesAllSame()
        {
            Array.Clear(_elements, 0, _elementsCount);
            var baseIndex = 0;
            for (var i = 0; i < _elementsCount; i++)
            {
                var index = baseIndex + i;
                index = FixIndex(index);
                _elements[i] = _values[index];
            }
            return _elements;
        }

        private IList<float> GetValuesByPolygon(int polygonIndex)
        {
            Array.Clear(_elements, 0, _elementsCount);
            var baseIndex = _byIndexMapping ? _indices[polygonIndex] * _elementsCount : polygonIndex * _elementsCount;
            for (var i = 0; i < _elementsCount; i++)
            {
                if (baseIndex < 0)
                {
                    _elements[i] = default;
                }
                else
                {
                    var index = baseIndex + i;
                    index = FixIndex(index);
                    _elements[i] = _values[index];
                }
            }
            return _elements;
        }

        private IList<float> GetValuesByPolygonVertex(int polygonVertexIndex)
        {
            Array.Clear(_elements, 0, _elementsCount);
            var baseIndex = _byIndexMapping ? _indices[polygonVertexIndex] * _elementsCount : polygonVertexIndex * _elementsCount;
            for (var i = 0; i < _elementsCount; i++)
            {
                if (baseIndex < 0)
                {
                    _elements[i] = default;
                }
                else
                {
                    var index = baseIndex + i;
                    index = FixIndex(index);
                    _elements[i] = _values[index];
                }
            }
            return _elements;
        }

        private float GetWeightByVertex(int vertexIndex)
        {
            float element;
            var baseIndex = vertexIndex;
            var index = baseIndex;
            index = FixWeightIndex(index);
            element = _weights[index];
            return element;
        }

        private float GetWeightAllSame()
        {
            float element;
            var baseIndex = 0;
            var index = baseIndex;
            index = FixWeightIndex(index);
            element = _weights[index];
            return element;
        }

        private float GetWeightByPolygon(int polygonIndex)
        {
            float element;
            var baseIndex = _byIndexMapping ? _indices[polygonIndex] : polygonIndex;
            if (baseIndex < 0)
            {
                element = default;
            }
            else
            {
                var index = baseIndex;
                index = FixWeightIndex(index);
                element = _weights[index];
            }
            return element;
        }

        private float GetWeightByPolygonVertex(int polygonVertexIndex)
        {
            float element;
            var baseIndex = _byIndexMapping ? _indices[polygonVertexIndex] : polygonVertexIndex;
            if (baseIndex < 0)
            {
                element = default;
            }
            else
            {
                var index = baseIndex;
                index = FixWeightIndex(index);
                element = _weights[index];
            }
            return element;
        }
    }
}