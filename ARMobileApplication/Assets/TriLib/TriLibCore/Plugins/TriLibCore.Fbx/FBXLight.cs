using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXLight : FBXModel, ILight
    {
        public enum EType
        {
            ePoint,
            eDirectional,
            eSpot,
            eArea,
            eVolume
        }

        public enum EDecayType
        {
            eNone,
            eLinear,
            eQuadratic,
            eCubic
        }

        public enum EAreaLightShape
        {
            eRectangle,
            eSphere
        }

        public LightType LightType { get; set; } = LightType.Point;
        public Color Color { get; set; } = Color.white;
        public float Intensity { get; set; } = 1f;
        public float Range { get; set; } = 10f;
        public float InnerSpotAngle { get; set; } = 45f;
        public float OuterSpotAngle { get; set; } = 45f;
        public float Width { get; set; }
        public float Height { get; set; }
        public bool CastShadows { get; set; }

        public FBXLight(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Light;
        }
    }
}