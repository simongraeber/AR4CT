using TriLibCore.Geometries;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public static class FBXGeometryGroupFactory
    {
        public static IGeometryGroup Get(AssetLoaderContext assetLoaderContext,
            bool hasNormal,
            bool hasTangent,
            bool hasColor,
            bool hasUv0,
            bool hasUv1,
            bool hasUv2,
            bool hasUv3,
            bool hasSkin)
        {
            var innerGeometryGroup = CommonGeometryGroup.Create(
                hasNormal,
                hasTangent,
                hasColor,
                hasUv0,
                hasUv1,
                hasUv2,
                hasUv3,
                hasSkin);
            return innerGeometryGroup;
        }
    }
}