using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public partial class FBXProcessor
    {
        private static void VerticesByVertexBlendShape(PropertyAccessorFloat values, int count, FBXBlendShapeGeometryGroup geometryGroup)
        {
            var size = count / 3;
            var output = new List<Vector3>(size);
            for (var i = 0; i < size; i++)
            {
                var x = values[i * 3 + 0];
                var y = values[i * 3 + 1];
                var z = values[i * 3 + 2];
                var value = new Vector3(x, y, z);
                output.Add(value);
            }

            geometryGroup.Vertices = output;
        }

        private static void NormalsByVertexBlendShape(PropertyAccessorFloat values, int count, FBXBlendShapeGeometryGroup geometryGroup)
        {
            var size = count / 3;
            var output = new List<Vector3>(size);
            for (var i = 0; i < size; i++)
            {
                var x = values[i * 3 + 0];
                var y = values[i * 3 + 1];
                var z = values[i * 3 + 2];
                var value = new Vector3(x, y, z);
                output.Add(value);
            }

            geometryGroup.Normals = output;
        }
    }
}