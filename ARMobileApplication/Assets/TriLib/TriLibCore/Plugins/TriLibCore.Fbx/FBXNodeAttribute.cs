using System;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXNodeAttribute : FBXObject
    {
        private const long _Color_token = 7096547112121362046;
        private const long _LightType_token = -4289198811918504181;
        private const long _Intensity_token = -4289201221581518386;
        private const long _InnerAngle_token = -3838029518067416606;
        private const long _OuterAngle_token = -3837864745021505827;
        private const long _CastShadows_token = -8303707451906348561;
        private const long _AreaLightShape_token = -3833484089739468835;
        private const long _FarAttenuationEnd_token = 6277359518966424291;
        private const long _AspectRatioMode_token = 6598308597108883505;
        private const long _AspectWidth_token = -8304873752109230295;
        private const long _AspectHeight_token = 803330716114588708;
        private const long _Position_token = -4898811219815348178;
        private const long _UpVector_token = -4898811082199209277;
        private const long _InterestPosition_token = -6116077400726326152;
        private const long _NearPlane_token = -4289197221258171217;
        private const long _FarPlane_token = -4898811507418504054;
        private const long _BackgroundColor_token = 347846795447519280;
        private const long _FieldOfViewX_token = 921891947640297703;
        private const long _FieldOfViewY_token = 921891947640297704;
        private const long _FieldOfView_token = -8301049196267529839;
        private const long _CameraProjectionType_token = -402591708976904141;
        private const long _OrthoZoom_token = -4289195994098765498;
        private const long _ApertureMode_token = 800592552796906968;
        private const long _ViewCameraToLookAt_token = -250390682556352868;
        private const long _Roll_token = 6774539739449686530;
        private const long _FocalLength_token = -8300892559941463930;
        private const long _GateFit_token = -5513532520748913759;
        private const long _PixelAspectRatio_token = 3025996705784283698;
        public const long _ApertureFormat_token = -5393807858173547796;
        public const long _FilmWidth_token = -4289203924710241219;
        public const long _FilmHeight_token = -3838113150483611248;
        public const long _FilmAspectRatio_token = 2392408135026437066;



        private FBXNode _node;

        public FBXNodeAttribute(FBXDocument document, FBXNode fbxNode, long objectId, string name, string objectClass) : base(document, name, objectId, objectClass)
        {
            _node = fbxNode;
            ObjectType = FBXObjectType.NodeAttribute;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public void ApplyNodeAttributes(FBXLight fbxLight, FBXProcessor processor)
        {
            void ApplyNodeAttributesInternal(FBXNode properties)
            {
                if (properties != null && properties.HasSubNodes)
                {
                    
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _Color_token:
                                {
                                    fbxLight.Color = property.Properties.GetColorValue(4);
                                    break;
                                }
                            case _LightType_token:
                                {
                                    var lightType = (FBXLight.EType)property.Properties.GetIntValue(4);
                                    switch (lightType)
                                    {
                                        case FBXLight.EType.ePoint:
                                            fbxLight.LightType = LightType.Point;
                                            break;
                                        case FBXLight.EType.eDirectional:
                                            fbxLight.LightType = LightType.Directional;
                                            break;
                                        case FBXLight.EType.eSpot:
                                            fbxLight.LightType = LightType.Spot;
                                            break;
                                    }
                                    break;
                                }
                            case _Intensity_token:
                                {
                                    fbxLight.Intensity = property.Properties.GetFloatValue(4) / 100f;
                                    break;
                                }
                            case _InnerAngle_token:
                                {
                                    fbxLight.InnerSpotAngle = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _OuterAngle_token:
                                {
                                    fbxLight.OuterSpotAngle = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _AreaLightShape_token:
                                var areaLightShape = (FBXLight.EAreaLightShape)property.Properties.GetIntValue(4);
                                switch (areaLightShape)
                                {
                                    case FBXLight.EAreaLightShape.eRectangle:
                                        fbxLight.LightType = LightType.Rectangle;
                                        break;
                                    case FBXLight.EAreaLightShape.eSphere:
                                        fbxLight.LightType = LightType.Disc;
                                        break;
                                }
                                break;
                            case _FarAttenuationEnd_token:
                                {
                                    fbxLight.Range = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _CastShadows_token:
                                {
                                    fbxLight.CastShadows = property.Properties.GetBoolValue(4);
                                    break;
                                }
                        }
                    }
                }
            }
            if (_node != null)
            {
                if (Document.NodeAttributeDefinition != null)
                {
                    var nodeAttributeDefinitionProperties = Document.NodeAttributeDefinition._node.GetNodeByName(processor.PropertiesName);
                    ApplyNodeAttributesInternal(nodeAttributeDefinitionProperties);
                }
                var properties = _node.GetNodeByName(processor.PropertiesName);
                ApplyNodeAttributesInternal(properties);
            }
        }

        public void ApplyNodeAttributes(FBXCamera fbxCamera, FBXProcessor processor)
        {
            void ApplyNodeAttributesInternal(FBXNode properties)
            {
                if (properties != null && properties.HasSubNodes)
                {
                    
                    foreach (var property in properties)
                    {
                        var propertyName = property.Properties.GetStringHashValue(0);
                        switch (propertyName)
                        {
                            case _AspectRatioMode_token:
                                {
                                    fbxCamera.AspectRatioMode = (FBXCamera.EAspectRatioMode)property.Properties.GetIntValue(4);
                                    break;
                                }
                            case _AspectWidth_token:
                                {
                                    fbxCamera.AspectWidth = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _AspectHeight_token:
                                {
                                    fbxCamera.AspectHeight = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _Position_token:
                                {
                                    var position = property.Properties.GetVector3Value(4);
                                    fbxCamera.Position = position;
                                    break;
                                }
                            case _UpVector_token:
                                {
                                    var upVector = property.Properties.GetVector3Value(4);
                                    fbxCamera.UpVector = upVector;
                                    break;
                                }
                            case _InterestPosition_token:
                                {
                                    var interestPosition = property.Properties.GetVector3Value(4);
                                    fbxCamera.InterestPosition = interestPosition;
                                    break;
                                }
                            case _NearPlane_token:
                                fbxCamera.NearClipPlane = property.Properties.GetFloatValue(4);
                                break;
                            case _FarPlane_token:
                                fbxCamera.FarClipPlane = property.Properties.GetFloatValue(4);
                                break;
                            case _BackgroundColor_token:
                                fbxCamera.BackgroundColor = property.Properties.GetColorValue(4);
                                break;
                            case _FieldOfViewX_token:
                                fbxCamera.FieldOfViewX = property.Properties.GetFloatValue(4);
                                break;
                            case _FieldOfViewY_token:
                                fbxCamera.FieldOfViewY = property.Properties.GetFloatValue(4);
                                break;
                            case _FieldOfView_token:
                                fbxCamera.FieldOfViewDefault = property.Properties.GetFloatValue(4);
                                break;
                            case _FilmWidth_token:
                                {
                                    fbxCamera.FilmWidth = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _FilmHeight_token:
                                {
                                    fbxCamera.FilmHeight = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _FilmAspectRatio_token:
                                {
                                    fbxCamera.FilmAspectRatio = property.Properties.GetFloatValue(4);
                                    fbxCamera.PhysicalCamera = true;
                                    break;
                                }
                            case _CameraProjectionType_token:
                                var projectionMode = (FBXCamera.EProjectionType)property.Properties.GetIntValue(4);
                                fbxCamera.Ortographic = projectionMode == FBXCamera.EProjectionType.eOrthogonal;
                                break;
                            case _OrthoZoom_token:
                                fbxCamera.OrtographicSize = property.Properties.GetFloatValue(4);
                                break;
                            case _ApertureMode_token:
                                var apertureMode = (FBXCamera.EApertureMode)property.Properties.GetIntValue(4);
                                fbxCamera.ApertureMode = apertureMode;
                                fbxCamera.PhysicalCamera = true;
                                switch (apertureMode)
                                {
                                    case FBXCamera.EApertureMode.eHorizontal:
                                        fbxCamera.FieldOfViewAxis = Camera.FieldOfViewAxis.Horizontal;
                                        break;
                                    case FBXCamera.EApertureMode.eVertical:
                                        fbxCamera.FieldOfViewAxis = Camera.FieldOfViewAxis.Vertical;
                                        break;
                                }
                                break;
                            case _ApertureFormat_token:
                                var apertureFormat = (FBXCamera.EApertureFormat)property.Properties.GetIntValue(4);
                                fbxCamera.ApertureFormat = apertureFormat;
                                break;
                            case _ViewCameraToLookAt_token:
                                {
                                    fbxCamera.ViewCameraToLookAt = property.Properties.GetIntValue(4) > 0;
                                    break;
                                }
                            case _Roll_token:
                                {
                                    fbxCamera.Roll = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _FocalLength_token:
                                {
                                    fbxCamera.FocalLength = property.Properties.GetFloatValue(4);
                                    break;
                                }
                            case _GateFit_token:
                                {
                                    var gateFitMode = (FBXCamera.EGateFit)property.Properties.GetIntValue(4);
                                    fbxCamera.PhysicalCamera = true;
                                    switch (gateFitMode)
                                    {
                                        case FBXCamera.EGateFit.eFitNone:
                                            fbxCamera.GateFitMode = Camera.GateFitMode.None;
                                            break;
                                        case FBXCamera.EGateFit.eFitVertical:
                                            fbxCamera.GateFitMode = Camera.GateFitMode.Vertical;
                                            break;
                                        case FBXCamera.EGateFit.eFitHorizontal:
                                            fbxCamera.GateFitMode = Camera.GateFitMode.Horizontal;
                                            break;
                                        case FBXCamera.EGateFit.eFitFill:
                                            fbxCamera.GateFitMode = Camera.GateFitMode.Fill;
                                            break;
                                        case FBXCamera.EGateFit.eFitOverscan:
                                            fbxCamera.GateFitMode = Camera.GateFitMode.Overscan;
                                            break;
                                    }
                                    break;
                                }
                            case _PixelAspectRatio_token:
                                {
                                    fbxCamera.PixelAspectRatio = property.Properties.GetFloatValue(4);
                                    break;
                                }
                        }
                    }
                }
            }
            if (_node != null)
            {
                if (Document.NodeAttributeDefinition != null)
                {
                    var nodeAttributeDefinitionProperties = Document.NodeAttributeDefinition._node.GetNodeByName(processor.PropertiesName);
                    ApplyNodeAttributesInternal(nodeAttributeDefinitionProperties);
                }
                var properties = _node.GetNodeByName(processor.PropertiesName);
                ApplyNodeAttributesInternal(properties);
            }
        }
    }
}