using System;
using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXAnimationStack : FBXObject, IAnimation
    {
        public string Description;

        public long LocalStart;

        public long LocalStop;

        public long ReferenceStart;

        public long ReferenceStop;

        public int LayersCount;

        public List<IAnimationCurveBinding> AnimationCurveBindings { get; set; }

        public Dictionary<IModel, Dictionary<string, IAnimationCurve>> AnimationCurvesByModel
        {
            get;
            set;
        }

        public int AnimationCurveBindingsCount;

        public HashSet<FBXModel> AnimatedModels;

        public HashSet<long> AnimatedTimes;

        public HashSet<FBXAnimationLayer> AnimatedLayers;

        public float FrameRate
        {
            get
            {
                float frameRate;
                switch (Document.GlobalSettings.TimeMode)
                {
                    case FBXMode.Frames120:
                        frameRate = 120f;
                        break;
                    case FBXMode.Frames100:
                        frameRate = 100f;
                        break;
                    case FBXMode.Frames60:
                        frameRate = 60f;
                        break;
                    case FBXMode.Frames50:
                        frameRate = 50f;
                        break;
                    case FBXMode.Frames48:
                        frameRate = 48f;
                        break;
                    case FBXMode.Frames30:
                    case FBXMode.Frames30Drop:
                        frameRate = 30f;
                        break;
                    case FBXMode.eNTSCDropFrame:
                        frameRate = 29.97f;
                        break;
                    case FBXMode.eNTSCFullFrame:
                        frameRate = 29.97f;
                        break;
                    case FBXMode.ePAL:
                        frameRate = 25f;
                        break;
                    case FBXMode.eFrames24:
                        frameRate = 24f;
                        break;
                    case FBXMode.eFrames1000:
                        frameRate = 1000f;
                        break;
                    case FBXMode.eFilmFullFrame:
                        frameRate = 23.976f;
                        break;
                    case FBXMode.eCustom:
                        frameRate = Document.GlobalSettings.CustomFrameRate;
                        break;
                    case FBXMode.eFrames96:
                        frameRate = 96f;
                        break;
                    case FBXMode.eFrames72:
                        frameRate = 72f;
                        break;
                    case FBXMode.eFrames59dot94:
                        frameRate = 59.94f;
                        break;
                    case FBXMode.eFrames119dot88:
                        frameRate = 119.88f;
                        break;
                    default:
                        frameRate = 30f;
                        break;
                }
                return frameRate;
            }
            set
            {

            }
        }

        public HashSet<float> TranslationKeyTimes
        {
            get  ;
            set ;
        }

        public FBXAnimationStack(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.AnimationStack;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.AnimationStackDefinition != null)
            {
                Description = Document.AnimationStackDefinition.Description;
                LocalStart = Document.AnimationStackDefinition.LocalStart;
                LocalStop = Document.AnimationStackDefinition.LocalStop;
                ReferenceStart = Document.AnimationStackDefinition.ReferenceStart;
                ReferenceStop = Document.AnimationStackDefinition.ReferenceStop;
            }
        }

        public long GetLocalStop()
        {
            if (AnimatedTimes == null || AnimatedTimes.Count == 0)
            {
                return LocalStop;
            }
            var animationStop = long.MinValue;
            foreach (var animatedTime in AnimatedTimes)
            {
                animationStop = Math.Max(animatedTime, animationStop);
            }
            return animationStop;
        }
    }
}
