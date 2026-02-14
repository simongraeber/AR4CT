// Copyright 2017 The Draco Authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
using System;
using System.Runtime.InteropServices;
using TriLibCore.Utils;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace TriLibCore.Gltf.Draco
{
    public unsafe class DracoMeshLoader
    {
        // These values must be exactly the same as the values in draco_types.h.
        // Attribute data type.
        private enum DataType
        {
            DT_INVALID = 0,
            DT_INT8,
            DT_UINT8,
            DT_INT16,
            DT_UINT16,
            DT_INT32,
            DT_UINT32,
            DT_INT64,
            DT_UINT64,
            DT_FLOAT32,
            DT_FLOAT64,
            DT_BOOL
        };

        // These values must be exactly the same as the values in
        // geometry_attribute.h.
        // Attribute type.
        private enum AttributeType
        {
            INVALID = -1,
            POSITION = 0,
            NORMAL,
            COLOR,
            TEX_COORD,

            // A special id used to mark attributes that are not assigned to any known
            // predefined use case. Such attributes are often used for a shader specific
            // data.
            GENERIC
        };

        [StructLayout(LayoutKind.Sequential)]
        public struct DracoMesh
        {
            public int numFaces;
            public int numVertices;
            public int numAttributes;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DracoData
        {
            public int dataType;
            public IntPtr data;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct DracoAttribute
        {
            public int attributeType;
            public int dataType;
            public int numComponents;
            public int uniqueId;
        }

        private struct DracoAttributeReader : IDisposable
        {
            private NativeArray<byte> _data;
            private DataType _dataType;
            private int _dataTypeSize;
            private int _elementSize;
            private int _elementIndex;

            public int ElementCount { get; }
            public int NumComponents { get; }

            public DracoAttributeReader(DracoMesh* dracoMesh, AttributeType attributeType, int index = 0)
            {
                _data = default;
                _dataType = default;
                _dataTypeSize = 0;
                NumComponents = 0;
                _elementSize = 0;
                _elementIndex = 0;
                ElementCount = 0;
                DracoAttribute* dracoAttribute;
                if (GetAttributeByType(dracoMesh, attributeType, index, &dracoAttribute))
                {
                    DracoData* dracoData;
                    if (GetAttributeData(dracoMesh, dracoAttribute, &dracoData))
                    {
                        _dataType = (DracoMeshLoader.DataType)dracoData->dataType;
                        _dataTypeSize = DataTypeSize(_dataType);
                        NumComponents = dracoAttribute->numComponents;
                        _elementSize = _dataTypeSize * NumComponents;
                        ElementCount = dracoMesh->numVertices;
                        _data = new NativeArray<byte>(ElementCount * _elementSize, Allocator.Persistent);
                        UnsafeUtility.MemCpy(_data.GetUnsafePtr(), dracoData->data.ToPointer(), _data.Length);
                        ReleaseDracoData(&dracoData);
                    }
                    ReleaseDracoAttribute(&dracoAttribute);
                }
            }

            public float ReadFloat()
            {
                switch (_dataType)
                {
                    case DataType.DT_INT8:
                        {
                            var dataPointer = (sbyte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / sbyte.MaxValue;
                        }
                    case DataType.DT_UINT8:
                        {
                            var dataPointer = (byte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / byte.MaxValue;
                        }
                    case DataType.DT_INT16:
                        {
                            var dataPointer = (short*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / short.MaxValue;
                        }
                    case DataType.DT_UINT16:
                        {
                            var dataPointer = (ushort*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / ushort.MaxValue;
                        }
                    case DataType.DT_INT32:
                        {
                            var dataPointer = (int*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / int.MaxValue;
                        }
                    case DataType.DT_UINT32:
                        {
                            var dataPointer = (uint*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / uint.MaxValue;
                        }
                    case DataType.DT_INT64:
                        {
                            var dataPointer = (long*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / long.MaxValue;
                        }
                    case DataType.DT_UINT64:
                        {
                            var dataPointer = (ulong*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer / ulong.MaxValue;
                        }
                    case DataType.DT_FLOAT32:
                        {
                            var dataPointer = (float*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return *dataPointer;
                        }
                    case DataType.DT_FLOAT64:
                        {
                            var dataPointer = (double*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (float)*dataPointer;
                        }
                    case DataType.DT_BOOL:
                        {
                            var dataPointer = (byte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return *dataPointer == 0 ? 0f : 1f;
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                }
            }

            public void Dispose()
            {
                if (_data.IsCreated)
                {
                    _data.Dispose();
                }
            }
        }

        private struct DracoIndicesReader : IDisposable
        {
            private NativeArray<byte> _data;
            private DataType _dataType;
            private int _dataTypeSize;
            private int _elementIndex;

            public int ElementCount { get; }

            public DracoIndicesReader(DracoMesh* dracoMesh)
            {
                _elementIndex = 0;
                DracoData* dracoData;
                GetMeshIndices(dracoMesh, &dracoData);
                _dataType = (DracoMeshLoader.DataType)dracoData->dataType;
                _dataTypeSize = DataTypeSize(_dataType);
                ElementCount = dracoMesh->numFaces * 3;
                _data = new NativeArray<byte>(ElementCount * _dataTypeSize, Allocator.Persistent);
                UnsafeUtility.MemCpy(_data.GetUnsafePtr(), dracoData->data.ToPointer(), _data.Length);
                ReleaseDracoData(&dracoData);
            }

            public int ReadInt()
            {
                switch (_dataType)
                {
                    case DataType.DT_INT8:
                        {
                            var dataPointer = (sbyte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_UINT8:
                        {
                            var dataPointer = (byte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_INT16:
                        {
                            var dataPointer = (short*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_UINT16:
                        {
                            var dataPointer = (ushort*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_INT32:
                        {
                            var dataPointer = (int*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_UINT32:
                        {
                            var dataPointer = (uint*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_INT64:
                        {
                            var dataPointer = (long*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_UINT64:
                        {
                            var dataPointer = (ulong*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_FLOAT32:
                        {
                            var dataPointer = (float*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_FLOAT64:
                        {
                            var dataPointer = (double*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    case DataType.DT_BOOL:
                        {
                            var dataPointer = (byte*)_data.GetUnsafeReadOnlyPtr() + _elementIndex++;
                            return (int)*dataPointer;
                        }
                    default:
                        {
                            throw new ArgumentOutOfRangeException();
                        }
                }
            }

            public void Dispose()
            {
                if (_data.IsCreated)
                {
                    _data.Dispose();
                }
            }
        }

        #region DllImport
#if ((UNITY_IOS || UNITY_WEBGL) && !UNITY_EDITOR)                                             
        private const string DllPath = "__Internal";
#else
        private const string DllPath = "dracodec_unity";
#endif
        #endregion

        // Release data associated with DracoMesh.
        [DllImport(DllPath)]
        private static extern void ReleaseDracoMesh(DracoMesh** mesh);

        // Release data associated with DracoAttribute.
        [DllImport(DllPath)]
        private static extern void ReleaseDracoAttribute(DracoAttribute** attr);

        // Release attribute data.
        [DllImport(DllPath)]
        private static extern void ReleaseDracoData(DracoData** data);

        // Decodes compressed Draco::Mesh in buffer to mesh. On input, mesh
        // must be null. The returned mesh must released with ReleaseDracoMesh.
        [DllImport(DllPath)]
        private static extern int DecodeDracoMesh(byte[] buffer, int length, DracoMesh** mesh);

        // Returns the DracoAttribute at index in mesh. On input, attribute must be
        // null. The returned attr must be released with ReleaseDracoAttribute.
        [DllImport(DllPath)]
        private static extern bool GetAttribute(DracoMesh* mesh, int index, DracoAttribute** attr);

        // Returns the DracoAttribute of type at index in mesh. On input, attribute
        // must be null. E.g. If the mesh has two texture coordinates then
        // GetAttributeByType(mesh, AttributeType.TEX_COORD, 1, &attr); will return
        // the second TEX_COORD attribute. The returned attr must be released with
        // ReleaseDracoAttribute.
        [DllImport(DllPath)]
        private static extern bool GetAttributeByType(DracoMesh* mesh, AttributeType type, int index, DracoAttribute** attr);

        // Returns the DracoAttribute with unique_id in mesh. On input, attribute
        // must be null.The returned attr must be released with
        // ReleaseDracoAttribute.
        [DllImport(DllPath)]
        private static extern bool GetAttributeByUniqueId(DracoMesh* mesh, int unique_id, DracoAttribute** attr);

        // Returns an array of indices as well as the type of data in data_type. On
        // input, indices must be null. The returned indices must be released with
        // ReleaseDracoData.
        [DllImport(DllPath)]
        private static extern bool GetMeshIndices(DracoMesh* mesh, DracoData** indices);

        // Returns an array of attribute data as well as the type of data in
        // data_type. On input, data must be null. The returned data must be
        // released with ReleaseDracoData.
        [DllImport(DllPath)]
        private static extern bool GetAttributeData(DracoMesh* mesh, DracoAttribute* attr, DracoData** data);

        // Creates a Unity mesh from the decoded Draco mesh.
        public GltfTempGeometryGroup CreateUnityMesh(DracoMesh* dracoMesh)
        {
            int[] newTriangles;
            using (var indicesReader = new DracoIndicesReader(dracoMesh))
            {
                newTriangles = new int[indicesReader.ElementCount];
                for (var i = 0; i < indicesReader.ElementCount; i++)
                {
                    newTriangles[i] = indicesReader.ReadInt();
                }
            }

            Vector3[] newVertices = null;
            using (var positionAttributeReader = new DracoAttributeReader(dracoMesh, AttributeType.POSITION))
            {
                if (positionAttributeReader.ElementCount > 0)
                {
                    newVertices = new Vector3[positionAttributeReader.ElementCount];
                    for (var i = 0; i < positionAttributeReader.ElementCount; i++)
                    {
                        newVertices[i] = new Vector3(positionAttributeReader.ReadFloat(), positionAttributeReader.ReadFloat(), positionAttributeReader.ReadFloat());
                    }
                }
            }

            Vector3[] newNormals = null;
            using (var normalAttributeReader = new DracoAttributeReader(dracoMesh, AttributeType.NORMAL))
            {
                if (normalAttributeReader.ElementCount > 0)
                {
                    newNormals = new Vector3[normalAttributeReader.ElementCount];
                    for (var i = 0; i < normalAttributeReader.ElementCount; i++)
                    {
                        newNormals[i] = new Vector3(normalAttributeReader.ReadFloat(), normalAttributeReader.ReadFloat(), normalAttributeReader.ReadFloat());
                    }
                }
            }

            Vector2[] newUVs = null;
            using (var texCoordAttributeReader = new DracoAttributeReader(dracoMesh, AttributeType.TEX_COORD))
            {
                if (texCoordAttributeReader.ElementCount > 0)
                {
                    newUVs = new Vector2[texCoordAttributeReader.ElementCount];
                    for (var i = 0; i < texCoordAttributeReader.ElementCount; i++)
                    {
                        newUVs[i] = new Vector2(texCoordAttributeReader.ReadFloat(), texCoordAttributeReader.ReadFloat());
                    }
                }
            }

            Color[] newColors = null;
            using (var colorAttributeReader = new DracoAttributeReader(dracoMesh, AttributeType.COLOR))
            {
                if (colorAttributeReader.ElementCount > 0)
                {
                    newColors = new Color[colorAttributeReader.ElementCount];
                    for (var i = 0; i < colorAttributeReader.ElementCount; i++)
                    {
                        newColors[i] = new Color(colorAttributeReader.ReadFloat(), colorAttributeReader.ReadFloat(), colorAttributeReader.ReadFloat(), colorAttributeReader.NumComponents == 4 ? colorAttributeReader.ReadFloat() : 1f);
                    }
                }
            }

            var mesh = new GltfTempGeometryGroup();

            if (newVertices != null)
            {
                for (var i = 0; i < newVertices.Length; i++)
                {
                    var vertex = newVertices[i];
                    vertex = RightHandToLeftHandConverter.ConvertVector(vertex);
                    newVertices[i] = vertex;
                }
                mesh.Vertices = newVertices;
            }

            if (newUVs != null)
            {
                for (var i = 0; i < newUVs.Length; i++)
                {
                    var uv = newUVs[i];
                    uv.y = 1f - uv.y;
                    newUVs[i] = uv;
                }
                mesh.UVsList = newUVs;
            }

            if (newNormals != null)
            {
                for (var i = 0; i < newNormals.Length; i++)
                {
                    var normal = newNormals[i];
                    normal = RightHandToLeftHandConverter.ConvertVector(normal);
                    newNormals[i] = normal;
                }
                mesh.NormalsList = newNormals;
            }

            if (newColors != null)
            {
                mesh.ColorsList = newColors;
            }

            Array.Reverse(newTriangles);
            mesh.IndicesList = newTriangles;

            return mesh;
        }

        private static int DataTypeSize(DataType dt)
        {
            switch (dt)
            {
                case DataType.DT_INT8:
                case DataType.DT_UINT8:
                    return 1;
                case DataType.DT_INT16:
                case DataType.DT_UINT16:
                    return 2;
                case DataType.DT_INT32:
                case DataType.DT_UINT32:
                    return 4;
                case DataType.DT_INT64:
                case DataType.DT_UINT64:
                    return 8;
                case DataType.DT_FLOAT32:
                    return 4;
                case DataType.DT_FLOAT64:
                    return 8;
                case DataType.DT_BOOL:
                    return 1;
                default:
                    return -1;
            }
        }

        // Decodes a Draco mesh, creates a GLTF2 Geometry Group from the decoded data and adds the Unity mesh to meshes. encodedData is the compressed Draco mesh.
        public int ConvertDracoMeshToGltfGeometryGroup(byte[] encodedData, out GltfTempGeometryGroup geometryGroup)
        {
            DracoMesh* mesh = null;
            if (DecodeDracoMesh(encodedData, encodedData.Length, &mesh) <= 0)
            {
                geometryGroup = null;
                return -1;
            }
            geometryGroup = CreateUnityMesh(mesh);
            var numFaces = mesh->numFaces;
            ReleaseDracoMesh(&mesh);
            return numFaces;
        }

        /// <summary>
        /// Creates the DracoMeshLoader and proceeds to decode the mesh contained in "encodedData".
        /// </summary>
        /// <param name="encodedData">The mesh data encoded as Draco.</param>
        /// <returns>The decoded mesh data as a temporary GeometryGroup.</returns>
        public static GltfTempGeometryGroup DracoDecompressorCallback(byte[] encodedData)
        {
            var dracoMeshLoader = new DracoMeshLoader();
            dracoMeshLoader.ConvertDracoMeshToGltfGeometryGroup(encodedData, out var geometryGroup);
            return geometryGroup;
        }
    }
}