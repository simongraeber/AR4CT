using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXCamera : FBXModel, ICamera
    {
        public bool HasTarget { get; set; }

        public enum EAspectRatioMode
        {
            eWindowSize,
            eFixedRatio,
            eFixedResolution,
            eFixedWidth,
            eFixedHeight
        }

        public enum EApertureMode
        {
            eHorizAndVert,
            eHorizontal,
            eVertical,
            eFocalLength
        }

        public enum EApertureFormat
        {
            eCustomAperture,
            e16mmTheatrical,
            eSuper16mm,
            e35mmAcademy,
            e35mmTVProjection,
            e35mmFullAperture,
            e35mm185Projection,
            e35mmAnamorphic,
            e70mmProjection,
            eVistaVision,
            eDynaVision,
            eIMAX
        }

        public enum EGateFit
        {
            eFitNone,
            eFitVertical,
            eFitHorizontal,
            eFitFill,
            eFitOverscan,
            eFitStretch
        }

        public enum EProjectionType
        {
            ePerspective,
            eOrthogonal
        }

        public Vector3 UpVector;
        public EAspectRatioMode AspectRatioMode;
        public Vector3 InterestPosition;
        public Vector3 Position;
        public float AspectWidth;
        public float AspectHeight;
        public float FieldOfViewX;
        public float FieldOfViewY = 60f;
        public EApertureMode ApertureMode;
        public bool ViewCameraToLookAt;
        public float Roll;//PROPERTY
        public float PixelAspectRatio = 1f;//PROPERTY
        public float FieldOfViewDefault;//PROPERTY
        public Color BackgroundColor;//PROPERTY
        public Rect ViewportRect;//PROPERTY
        public Camera.FieldOfViewAxis FieldOfViewAxis;//PROPERTY
        public FBXModel LookAtProperty;//PROPERTY
        public EApertureFormat ApertureFormat;//PROPERTY
        public float FilmWidth;//PROPERTY
        public float FilmHeight;//PROPERTY
        public float FilmAspectRatio;//PROPERTY

        public float AspectRatio { get; set; } = 1.333333f;
        public bool Ortographic { get; set; } = false;
        public float OrtographicSize { get; set; } = 1f;
        public float FieldOfView { get; set; }
        public float NearClipPlane { get; set; } = 100f;
        public float FarClipPlane { get; set; } = 40000f;
        public float FocalLength { get; set; }
        public Vector2 SensorSize { get; set; }
        public Vector2 LensShift { get; set; } //todo: check default value
        public Camera.GateFitMode GateFitMode { get; set; } = Camera.GateFitMode.None;
        public bool PhysicalCamera { get; set; }


        //todo: filmaspectratio
        //http://docs.autodesk.com/FBX/2014/ENU/FBX-SDK-Documentation/index.html?url=cpp_ref/class_fbx_camera.html,topicNumber=cpp_ref_class_fbx_camera_htmld363a59a-a768-4ab3-b3a6-d806f23a2f8b
        public FBXCamera(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.Camera;
        }

        public void Setup(AssetLoaderContext assetLoaderContext)
        {
            NearClipPlane /= 100f;
            FarClipPlane /= 100f;
            FieldOfView = FieldOfViewY;
            //FocalLength = ComputeFocalLength(40f); todo
            Vector2 filmSize;
            float squeezeRatio;
            float aspectRatio;
            switch (ApertureFormat)
            {
                default:
                    filmSize = new Vector2(FilmWidth, FilmHeight);
                    squeezeRatio = 1f;
                    aspectRatio = FilmAspectRatio;
                    break;
                case EApertureFormat.e16mmTheatrical:
                    filmSize = new Vector2(0.404f, 0.295f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.369f;
                    break;
                case EApertureFormat.eSuper16mm:
                    filmSize = new Vector2(0.493f, 0.292f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.688f;
                    break;
                case EApertureFormat.e35mmAcademy:
                    filmSize = new Vector2(0.864f, 0.630f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.371f;
                    break;
                case EApertureFormat.e35mmTVProjection:
                    filmSize = new Vector2(0.816f, 0.612f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.333f;
                    break;
                case EApertureFormat.e35mmFullAperture:
                    filmSize = new Vector2(0.980f, 0.735f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.333f;
                    break;
                case EApertureFormat.e35mm185Projection:
                    filmSize = new Vector2(0.825f, 0.446f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.850f;
                    break;
                case EApertureFormat.e35mmAnamorphic:
                    filmSize = new Vector2(0.864f, 0.732f);
                    squeezeRatio = 2.0f;
                    aspectRatio = 0.732f;
                    break;
                case EApertureFormat.e70mmProjection:
                    filmSize = new Vector2(2.066f, 0.906f);
                    squeezeRatio = 2.0f;
                    aspectRatio = 2.280f;
                    break;
                case EApertureFormat.eVistaVision:
                    filmSize = new Vector2(1.485f, 0.991f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.498f;
                    break;
                case EApertureFormat.eDynaVision:
                    filmSize = new Vector2(2.080f, 1.480f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.405f;
                    break;
                case EApertureFormat.eIMAX:
                    filmSize = new Vector2(2.772f, 2.772f);
                    squeezeRatio = 1.0f;
                    aspectRatio = 1.338f;
                    break;
            }
            const float inchToMM = 25.4f;
            SensorSize = filmSize * inchToMM;
        }

        //todo: implement
        //private float ComputeFocalLength(float f)
        //{
        //    return 2f * Mathf.Atan((SensorSize.x / SensorSize.y) / 2 * 1 / f);
        //}
    }
}
