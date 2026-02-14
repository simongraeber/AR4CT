using System;
using System.Collections.Generic;
using TriLibCore.Collections;
using TriLibCore.General;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public partial class GtlfProcessor
    {
        private Dictionary<int, int> _minIntValues = new Dictionary<int, int>();
        private Dictionary<int, int> _maxIntValues = new Dictionary<int, int>();
        private Dictionary<int, float> _minFloatValues = new Dictionary<int, float>();
        private Dictionary<int, float> _maxFloatValues = new Dictionary<int, float>();

        private int GetAccessorCount(JsonParser.JsonValue accessor)
        {
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out _, _temporaryString) && accessor.TryGetChildWithKey(_sparse_token, out var sparse))
            {
                var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                return sparseCount;
            }
            return accessor.GetChildValueAsInt(_count_token, _temporaryString, 0);
        }

        private bool GetAccessorData(JsonParser.JsonValue accessor, bool rawData, int? typeOverride, int? countOverride, out int accessorType, out int accessorComponentType, out int accessorByteOffset, out int accessorCount, out int subDataCount, out int dataCount, out JsonParser.JsonValue min, out JsonParser.JsonValue max, out int calculatedStride, out bool accessorNormalized)
        {
            accessorType = typeOverride ?? ConvertAccessorType(accessor.GetChildValueAsString(_type_token, _temporaryString));
            accessorComponentType = accessor.GetChildValueAsInt(_componentType_token, _temporaryString, FLOAT);
            accessorByteOffset = accessor.GetChildValueAsInt(_byteOffset_token, _temporaryString, 0);
            accessorCount = countOverride ?? accessor.GetChildValueAsInt(_count_token, _temporaryString, 0);
            subDataCount = GetAccessorSubDataCount(accessorType);
            dataCount = rawData ? accessorCount * subDataCount : accessorCount;
            accessor.TryGetChildWithKey(_min_token, out min);
            accessor.TryGetChildWithKey(_max_token, out max);
            if (min.Valid)
            {
                var hashCode = min.GetHashCode();
                if (!_minIntValues.ContainsKey(hashCode))
                {
                    _minIntValues.Add(hashCode, min.GetValueAsInt(_temporaryString));
                    _minFloatValues.Add(hashCode, min.GetValueAsFloat(_temporaryString));
                }
            }
            if (max.Valid)
            {
                var hashCode = max.GetHashCode();
                if (!_maxIntValues.ContainsKey(hashCode))
                {
                    _maxIntValues.Add(hashCode, max.GetValueAsInt(_temporaryString));
                    _maxFloatValues.Add(hashCode, max.GetValueAsFloat(_temporaryString));
                }
            }
            calculatedStride = CalculateStride(accessorComponentType, accessorType);
            accessorNormalized = accessor.GetChildValueAsBool(_normalized_token, _temporaryString, false);
            if (dataCount == 0)
            {
                return false;
            }
            return true;
        }

        #region generated

        private IList<float> ConvertAccessorDataFloat(JsonParser.JsonValue accessor, bool rawData = false, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, rawData, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<float>(dataCount);
            var sparseValues = new Dictionary<int, float>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataFloat(sparse.GetChildWithKey(_values_token), false, accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                if (rawData)
                {
                    for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                    {
                        var initialIndex = index;
                        for (var j = 0; j < subDataCount; j++)
                        {
                            var elementData = sparseValues.TryGetValue(elementIndex, out var value) ? value : ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, j);
                            data.Add(elementData);
                        }

                        var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                        index += stride - (index - initialIndex);
                    }
                }
                else
                {
                    for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                    {
                        var initialIndex = index;
                        switch (accessorType)
                        {
                            case SCALAR:
                                {
                                    var elementData = sparseValues.TryGetValue(elementIndex, out var value) ? value : ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    if (min.Valid || max.Valid)
                                    {
                                        elementData = ClampValueFloat(elementData, min, max, 0, accessorComponentType, accessorNormalized);
                                    }

                                    data.Add(elementData);
                                    break;
                                }
                        }

                        var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                        index += stride - (index - initialIndex);
                    }
                }
            }

            return data;
        }

        private IList<int> ConvertAccessorDataInt(JsonParser.JsonValue accessor, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<int>(dataCount);
            var sparseValues = new Dictionary<int, int>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataInt(sparse.GetChildWithKey(_values_token), accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    var elementData = sparseValues.TryGetValue(elementIndex, out var value) ? value : ReadAccessorElementInt(bufferData, accessorComponentType, ref index, 0);
                    if (min.Valid || max.Valid)
                    {
                        elementData = ClampValueInt(elementData, min, max, 0);
                    }

                    data.Add(elementData);
                    if (bufferViewByteStride != 0)
                    {
                        index += bufferViewByteStride - (index - initialIndex);
                    }
                }
            }

            return data;
        }

        private IList<Color> ConvertAccessorDataColor(JsonParser.JsonValue accessor, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<Color>(dataCount);
            var sparseValues = new Dictionary<int, Color>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataColor(sparse.GetChildWithKey(_values_token), accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = new Vector2(value.r, value.g);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    elementData = new Vector2(x, y);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }

                                data.Add(new Color(elementData.x, elementData.y, 0f));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = new Vector3(value.r, value.g, value.b);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    elementData = new Vector3(x, y, z);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }

                                data.Add(new Color(elementData.x, elementData.y, elementData.z));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    var w = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 3);
                                    elementData = new Vector4(x, y, z, w);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                    elementData.w = ClampValueFloat(elementData.w, min, max, 3, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Color)elementData);
                                break;
                            }
                    }

                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }

            return data;
        }

        private IList<Vector2> ConvertAccessorDataVector2(JsonParser.JsonValue accessor, bool isUV = false, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<Vector2>(dataCount);
            var sparseValues = new Dictionary<int, Vector2>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataVector2(sparse.GetChildWithKey(_values_token), false, accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector2)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    elementData = new Vector2(x, y);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }

                                if (isUV)
                                {
                                    elementData.y = 1f - elementData.y;
                                }

                                data.Add((Vector2)(elementData));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector3)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    elementData = new Vector3(x, y, z);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector2)(elementData));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    var w = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 3);
                                    elementData = new Vector4(x, y, z, w);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                    elementData.w = ClampValueFloat(elementData.w, min, max, 3, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector2)elementData);
                            }
                            break;
                    }

                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }

            return data;
        }

        private List<Vector3> ConvertAccessorDataVector3(JsonParser.JsonValue accessor, bool convertHand = false, bool scale = false, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<Vector3>(dataCount);
            var sparseValues = new Dictionary<int, Vector3>(dataCount);

            if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
            {
                var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                var values = ConvertAccessorDataVector3(sparse.GetChildWithKey(_values_token), false, false, accessorType, sparseCount);
                for (var i = 0; i < indices.Count; i++)
                {
                    sparseValues[indices[i]] = values[i];
                }
            }

            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector2)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    elementData = new Vector2(x, y);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector3)(elementData));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector3)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    elementData = new Vector3(x, y, z);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }

                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertVector(elementData);
                                }

                                if (scale)
                                {
                                    var scaleFactor = _reader.AssetLoaderContext.Options.ScaleFactor;
                                    elementData *= scaleFactor;
                                }

                                data.Add((Vector3)(elementData));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    var w = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 3);
                                    elementData = new Vector4(x, y, z, w);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                    elementData.w = ClampValueFloat(elementData.w, min, max, 3, accessorComponentType, accessorNormalized);
                                }

                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertRotation(elementData);
                                }

                                data.Add((Vector3)elementData);
                                break;
                            }
                    }

                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }
            else
            {
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector2)(value);
                                }
                                else
                                {
                                    var x = 0f;
                                    var y = 0f;
                                    elementData = new Vector2(x, y);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector3)(elementData));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector3)(value);
                                }
                                else
                                {
                                    var x = 0f;
                                    var y = 0f;
                                    var z = 0f;
                                    elementData = new Vector3(x, y, z);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }

                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertVector(elementData);
                                }

                                if (scale)
                                {
                                    var scaleFactor = _reader.AssetLoaderContext.Options.ScaleFactor;
                                    elementData *= scaleFactor;
                                }

                                data.Add((Vector3)(elementData));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4)(value);
                                }
                                else
                                {
                                    var x = 0f;
                                    var y = 0f;
                                    var z = 0f;
                                    var w = 0f;
                                    elementData = new Vector4(x, y, z, w);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                    elementData.w = ClampValueFloat(elementData.w, min, max, 3, accessorComponentType, accessorNormalized);
                                }

                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertRotation(elementData);
                                }

                                data.Add((Vector3)elementData);
                                break;
                            }
                    }
                }
            }

            return data;
        }

        private IList<Vector4Int> ConvertAccessorDataVector4Int(JsonParser.JsonValue accessor, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<Vector4Int>(dataCount);
            var sparseValues = new Dictionary<int, Vector4Int>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataVector4Int(sparse.GetChildWithKey(_values_token), accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector2)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    elementData = new Vector2(x, y);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector4Int)(elementData));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector3)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    elementData = new Vector3(x, y, z);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }

                                data.Add((Vector4Int)(elementData));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4Int elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4Int)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementInt(bufferData, accessorComponentType, ref index, 0);
                                    var y = ReadAccessorElementInt(bufferData, accessorComponentType, ref index, 1);
                                    var z = ReadAccessorElementInt(bufferData, accessorComponentType, ref index, 2);
                                    var w = ReadAccessorElementInt(bufferData, accessorComponentType, ref index, 3);
                                    elementData = new Vector4Int(x, y, z, w);
                                }

                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueInt(elementData.x, min, max, 0);
                                    elementData.y = ClampValueInt(elementData.y, min, max, 1);
                                    elementData.z = ClampValueInt(elementData.z, min, max, 2);
                                    elementData.w = ClampValueInt(elementData.w, min, max, 3);
                                }

                                data.Add((Vector4Int)(elementData));
                                break;
                            }
                    }

                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }

            return data;
        }

        private IList<Matrix4x4> ConvertAccessorDataMatrix4x4(JsonParser.JsonValue accessor, bool convertHand = false, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }

            var data = new List<Matrix4x4>(dataCount);
            var sparseValues = new Dictionary<int, Matrix4x4>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataMatrix4x4(sparse.GetChildWithKey(_values_token), false, accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }

                GetBufferViewData(bufferViewIndex, accessorByteOffset, out var bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case MAT4:
                            {
                                Matrix4x4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Matrix4x4)(value);
                                }
                                else
                                {
                                    var m0 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var m1 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var m2 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    var m3 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 3);
                                    var m4 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 4);
                                    var m5 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 5);
                                    var m6 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 6);
                                    var m7 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 7);
                                    var m8 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 8);
                                    var m9 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 9);
                                    var m10 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 10);
                                    var m11 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 11);
                                    var m12 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 12);
                                    var m13 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 13);
                                    var m14 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 14);
                                    var m15 = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 15);
                                    elementData = new Matrix4x4
                                    {
                                        [0] = m0,
                                        [1] = m1,
                                        [2] = m2,
                                        [3] = m3,
                                        [4] = m4,
                                        [5] = m5,
                                        [6] = m6,
                                        [7] = m7,
                                        [8] = m8,
                                        [9] = m9,
                                        [10] = m10,
                                        [11] = m11,
                                        [12] = m12,
                                        [13] = m13,
                                        [14] = m14,
                                        [15] = m15
                                    };
                                }

                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertMatrix(elementData);
                                }

                                data.Add((Matrix4x4)(elementData));
                                break;
                            }
                    }

                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }

            return data;
        }

        #endregion

        private static int CalculateStride(int accessorComponentType, int accessorType)
        {
            int stride;
            int intSize;
            switch (accessorComponentType)
            {
                case BYTE:
                case UNSIGNED_BYTE:
                    intSize = 1;
                    break;
                case SHORT:
                case UNSIGNED_SHORT:
                    intSize = 2;
                    break;
                default:
                    intSize = 4;
                    break;
            }

            switch (accessorType)
            {
                case SCALAR:
                    stride = 1 * intSize;
                    break;
                case VEC2:
                    stride = 2 * intSize;
                    break;
                case VEC3:
                    stride = 3 * intSize;
                    break;
                case VEC4:
                    stride = 4 * intSize;
                    break;
                case MAT2:
                    stride = 4 * intSize;
                    break;
                case MAT3:
                    stride = 9 * intSize;
                    break;
                default:
                    stride = 16 * intSize;
                    break;
            }

            return stride;
        }

        private int GetAccessorSubDataCount(int accessorType)
        {
            int subDataCount;
            switch (accessorType)
            {
                case SCALAR:
                    subDataCount = 1;
                    break;
                case VEC2:
                    subDataCount = 2;
                    break;
                case VEC3:
                    subDataCount = 3;
                    break;
                case VEC4:
                    subDataCount = 4;
                    break;
                case MAT2:
                    subDataCount = 4;
                    break;
                case MAT3:
                    subDataCount = 9;
                    break;
                default:
                    subDataCount = 16;
                    break;
            }

            return subDataCount;
        }

        private int ConvertAccessorType(string type)
        {
            switch (type)
            {
                case "SCALAR":
                    return SCALAR;
                case "VEC2":
                    return VEC2;
                case "VEC3":
                    return VEC3;
                case "VEC4":
                    return VEC4;
                case "MAT2":
                    return MAT2;
                case "MAT3":
                    return MAT3;
                case "MAT4":
                    return MAT4;
            }

            return 0;
        }

        private int ReadAccessorElementInt(StreamChunk bufferData, int accessorComponentType, ref int index, int elementIndex)
        {
            int data;
            switch (accessorComponentType)
            {
                case BYTE:
                    data = (sbyte)bufferData[index];
                    index++;
                    break;
                case UNSIGNED_BYTE:
                    data = bufferData[index];
                    index++;
                    break;
                case SHORT:
                    data = BitConverter.ToInt16(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(short);
                    break;
                case UNSIGNED_SHORT:
                    data = BitConverter.ToUInt16(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(ushort);
                    break;
                case UNSIGNED_INT:
                    data = (int)BitConverter.ToUInt32(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(uint);
                    break;
                default:
                    data = (int)BitConverter.ToSingle(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(float);
                    break;
            }

            return data;
        }

        private float ReadAccessorElementFloat(StreamChunk bufferData, int accessorComponentType, bool accessorNormalized, ref int index, int elementIndex)
        {
            float data;
            switch (accessorComponentType)
            {
                case BYTE:
                    data = accessorNormalized ? Mathf.Max((sbyte)bufferData[index] / (float)sbyte.MaxValue, -1f) : (sbyte)bufferData[index];
                    index++;
                    break;
                case UNSIGNED_BYTE:
                    data = accessorNormalized ? bufferData[index] / (float)byte.MaxValue : bufferData[index];
                    index++;
                    break;
                case SHORT:
                    data = accessorNormalized ? Mathf.Max(BitConverter.ToInt16(bufferData.ToBytes(_tempBytes8, index), 0) / (float)short.MaxValue, -1f) : BitConverter.ToInt16(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(short);
                    break;
                case UNSIGNED_SHORT:
                    data = accessorNormalized ? BitConverter.ToUInt16(bufferData.ToBytes(_tempBytes8, index), 0) / (float)ushort.MaxValue : BitConverter.ToUInt16(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(ushort);
                    break;
                case UNSIGNED_INT:
                    data = BitConverter.ToUInt32(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(uint);
                    break;
                default:
                    data = BitConverter.ToSingle(bufferData.ToBytes(_tempBytes8, index), 0);
                    index += sizeof(float);
                    break;
            }

            return data;
        }

        private IList<Vector4> ConvertAccessorDataVector4(JsonParser.JsonValue accessor, bool convertHand = false, int? typeOverride = null, int? countOverride = null)
        {
            if (!GetAccessorData(accessor, false, typeOverride, countOverride, out var accessorType, out var accessorComponentType, out var accessorByteOffset, out var accessorCount, out var subDataCount, out var dataCount, out var min, out var max, out var calculatedStride, out var accessorNormalized))
            {
                return null;
            }
            var data = new List<Vector4>(dataCount);
            var sparseValues = new Dictionary<int, Vector4>(dataCount);
            if (accessor.TryGetChildValueAsInt(_bufferView_token, out var bufferViewIndex, _temporaryString))
            {
                if (accessor.TryGetChildWithKey(_sparse_token, out var sparse))
                {
                    var sparseCount = sparse.GetChildValueAsInt(_count_token, _temporaryString, 0);
                    var indices = ConvertAccessorDataInt(sparse.GetChildWithKey(_indices_token), SCALAR, sparseCount);
                    var values = ConvertAccessorDataVector4(sparse.GetChildWithKey(_values_token), false, accessorType, sparseCount);
                    for (var i = 0; i < indices.Count; i++)
                    {
                        sparseValues[indices[i]] = values[i];
                    }
                }
                GetBufferViewData(bufferViewIndex, accessorByteOffset, out StreamChunk bufferData, out var index, out var bufferViewByteStride, out var bufferViewByteOffset);
                for (var elementIndex = 0; elementIndex < accessorCount; elementIndex++)
                {
                    var initialIndex = index;
                    switch (accessorType)
                    {
                        case VEC2:
                            {
                                Vector2 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector2)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    elementData = new Vector2(x, y);
                                }
                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                }
                                data.Add((Vector4)(elementData));
                                break;
                            }
                        case VEC3:
                            {
                                Vector3 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector3)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    elementData = new Vector3(x, y, z);
                                }
                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                }
                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertVector(elementData);
                                }
                                data.Add((Vector4)(elementData));
                                break;
                            }
                        case VEC4:
                            {
                                Vector4 elementData;
                                if (sparseValues.TryGetValue(elementIndex, out var value))
                                {
                                    elementData = (Vector4)(value);
                                }
                                else
                                {
                                    var x = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 0);
                                    var y = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 1);
                                    var z = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 2);
                                    var w = ReadAccessorElementFloat(bufferData, accessorComponentType, accessorNormalized, ref index, 3);
                                    elementData = new Vector4(x, y, z, w);
                                }
                                if (min.Valid || max.Valid)
                                {
                                    elementData.x = ClampValueFloat(elementData.x, min, max, 0, accessorComponentType, accessorNormalized);
                                    elementData.y = ClampValueFloat(elementData.y, min, max, 1, accessorComponentType, accessorNormalized);
                                    elementData.z = ClampValueFloat(elementData.z, min, max, 2, accessorComponentType, accessorNormalized);
                                    elementData.w = ClampValueFloat(elementData.w, min, max, 3, accessorComponentType, accessorNormalized);
                                }
                                if (convertHand)
                                {
                                    elementData = RightHandToLeftHandConverter.ConvertRotation(elementData);
                                }
                                data.Add((Vector4)elementData);
                                break;
                            }
                    }
                    var stride = bufferViewByteStride != 0 ? (int)bufferViewByteStride : calculatedStride;
                    index += stride - (index - initialIndex);
                }
            }
            return data;
        }
    }
}