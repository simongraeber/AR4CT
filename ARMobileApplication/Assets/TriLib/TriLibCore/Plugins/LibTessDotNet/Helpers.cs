using System;
using System.Collections.Generic;
using TriLibCore;
using TriLibCore.Geometries;
using TriLibCore.Interfaces;
using UnityEngine;

namespace LibTessDotNet
{
    public static class Helpers
    {
        //private static Vec3 ToVec3(Vector3 vertex)
        //{
        //    return new Vec3(vertex.x, vertex.y, vertex.z);
        //}

        //private static Vector3 ToVector3(Vec3 vertex)
        //{
        //    return new Vector3(vertex.X, vertex.Y, vertex.Z);
        //}

        public static void Tesselate(IList<ContourVertex> contourVertices, AssetLoaderContext assetLoaderContext, IGeometry geometry, IGeometryGroup geometryGroup, bool counterClockwise = false)
        {
            if (assetLoaderContext.Options.DisableTesselation)
            {
                return;
            }
            var tess = new Tess();
            tess.AddContour(contourVertices);
            tess.Tessellate(WindingRule.EvenOdd, ElementType.Polygons, 3, CombineCallback);
            for (var j = 0; j < tess.ElementCount; j++)
            {
                var baseIndex = j * 3;
                var v1 = (IVertexData)tess.Vertices[tess.Elements[baseIndex]].Data;
                var v2 = (IVertexData)tess.Vertices[tess.Elements[baseIndex + 1]].Data;
                var v3 = (IVertexData)tess.Vertices[tess.Elements[baseIndex + 2]].Data;
                if (counterClockwise)
                {
                    geometry.AddVertex(assetLoaderContext,
                        originalVertexIndex: v3.GetVertexIndex(geometryGroup),
                        position: v3.GetPosition(geometryGroup),
                        normal: v3.GetNormal(geometryGroup),
                        tangent: v3.GetTangent(geometryGroup),
                        color: v3.GetColor(geometryGroup),
                        uv0: v3.GetUV1(geometryGroup),
                        uv1: v3.GetUV2(geometryGroup),
                        uv2: v3.GetUV3(geometryGroup),
                        uv3: v3.GetUV4(geometryGroup), boneWeight: v3.GetBoneWeight(geometryGroup));
                    geometry.AddVertex(assetLoaderContext,
                        originalVertexIndex: v2.GetVertexIndex(geometryGroup),
                        position: v2.GetPosition(geometryGroup),
                        normal: v2.GetNormal(geometryGroup),
                        tangent: v2.GetTangent(geometryGroup),
                        color: v2.GetColor(geometryGroup),
                        uv0: v2.GetUV1(geometryGroup),
                        uv1: v2.GetUV2(geometryGroup),
                        uv2: v2.GetUV3(geometryGroup),
                        uv3: v2.GetUV4(geometryGroup), boneWeight: v2.GetBoneWeight(geometryGroup));
                    geometry.AddVertex(assetLoaderContext,
                        originalVertexIndex: v1.GetVertexIndex(geometryGroup),
                        position: v1.GetPosition(geometryGroup),
                        normal: v1.GetNormal(geometryGroup),
                        tangent: v1.GetTangent(geometryGroup),
                        color: v1.GetColor(geometryGroup),
                        uv0: v1.GetUV1(geometryGroup),
                        uv1: v1.GetUV2(geometryGroup),
                        uv2: v1.GetUV3(geometryGroup),
                        uv3: v1.GetUV4(geometryGroup), boneWeight: v1.GetBoneWeight(geometryGroup));
                }
                else
                {
                    geometry.AddVertex(assetLoaderContext,
                       originalVertexIndex: v1.GetVertexIndex(geometryGroup),
                       position: v1.GetPosition(geometryGroup),
                       normal: v1.GetNormal(geometryGroup),
                       tangent: v1.GetTangent(geometryGroup),
                       color: v1.GetColor(geometryGroup),
                       uv0: v1.GetUV1(geometryGroup),
                       uv1: v1.GetUV2(geometryGroup),
                       uv2: v1.GetUV3(geometryGroup),
                       uv3: v1.GetUV4(geometryGroup), boneWeight: v1.GetBoneWeight(geometryGroup));
                    geometry.AddVertex(assetLoaderContext,
                        originalVertexIndex: v2.GetVertexIndex(geometryGroup),
                        position: v2.GetPosition(geometryGroup),
                        normal: v2.GetNormal(geometryGroup),
                        tangent: v2.GetTangent(geometryGroup),
                        color: v2.GetColor(geometryGroup),
                        uv0: v2.GetUV1(geometryGroup),
                        uv1: v2.GetUV2(geometryGroup),
                        uv2: v2.GetUV3(geometryGroup),
                        uv3: v2.GetUV4(geometryGroup), boneWeight: v2.GetBoneWeight(geometryGroup));
                    geometry.AddVertex(assetLoaderContext,
                        originalVertexIndex: v3.GetVertexIndex(geometryGroup),
                        position: v3.GetPosition(geometryGroup),
                        normal: v3.GetNormal(geometryGroup),
                        tangent: v3.GetTangent(geometryGroup),
                        color: v3.GetColor(geometryGroup),
                        uv0: v3.GetUV1(geometryGroup),
                        uv1: v3.GetUV2(geometryGroup),
                        uv2: v3.GetUV3(geometryGroup),
                        uv3: v3.GetUV4(geometryGroup), boneWeight: v3.GetBoneWeight(geometryGroup));
                }
            }
        }

        private static object CombineCallback(Vec3 position, object[] data, float[] weights)
        {
            Vector3 finalPosition = default;
            Vector3 finalNormal = default;
            Vector4 finalTangent = default;
            Color finalColor = default;
            Vector2 finalUV0 = default;
            Vector2 finalUV1 = default;
            Vector2 finalUV2 = default;
            Vector2 finalUV3 = default;
            int finalVertexIndex = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var vertexData = (IVertexData)data[i];
                if (i == 0)
                {
                    finalVertexIndex = vertexData.GetVertexIndex(null);
                }
                finalPosition += weights[i] * vertexData.GetPosition(null);
                finalNormal += weights[i] * vertexData.GetNormal(null);
                finalTangent += weights[i] * vertexData.GetTangent(null);
                finalColor += weights[i] * vertexData.GetColor(null);
                finalUV0 += weights[i] * vertexData.GetUV1(null);
                finalUV1 += weights[i] * vertexData.GetUV2(null);
                finalUV2 += weights[i] * vertexData.GetUV3(null);
                finalUV3 += weights[i] * vertexData.GetUV4(null);
            }
            var interpolatedVertex = new InterpolatedVertex(finalPosition);
            interpolatedVertex.SetNormal(finalNormal, null);
            interpolatedVertex.SetTangent(finalTangent, null);
            interpolatedVertex.SetColor(finalColor, null);
            interpolatedVertex.SetUV1(finalUV0, null);
            interpolatedVertex.SetUV2(finalUV1, null);
            interpolatedVertex.SetUV3(finalUV2, null);
            interpolatedVertex.SetUV4(finalUV3, null);
            interpolatedVertex.SetVertexIndex(finalVertexIndex, null);
            return interpolatedVertex;
        }
    }
}
