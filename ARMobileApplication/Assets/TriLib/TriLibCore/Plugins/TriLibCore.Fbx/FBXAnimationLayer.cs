using System;
using System.Collections.Generic;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx
{
    public class FBXAnimationLayer : FBXObject, IComparer<FBXAnimationLayer>, IComparable<FBXAnimationLayer>
    {
        public FBXAnimationLayerBlendMode BlendMode;

        public FBXRotationAccumulationOrder RotationAccumulationMode;

        public FBXScaleAccumulationMode ScaleAccumulationMode;

        public int LayerId;

        public float Weight;

        public bool Mute;

        public bool Solo;

        public bool Lock;

        public IList<FBXAnimationCurveNode> CurveNodes;

        public int CurveNodesCount;

        public Dictionary<FBXModel, IList<FBXAnimationCurveNode>> CurveNodesDictionary;

        public Dictionary<FBXModel, IList<FBXAnimationCurveNode>> GeometryCurveNodesDictionary;

        public IList<FBXAnimationStack> AnimationStacks = new List<FBXAnimationStack>();

        public FBXAnimationLayer(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.AnimationLayer;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public sealed override void LoadDefinition()
        {
            if (Document.AnimationLayerDefinition != null)
            {
                BlendMode = Document.AnimationLayerDefinition.BlendMode;
                RotationAccumulationMode = Document.AnimationLayerDefinition.RotationAccumulationMode;
                ScaleAccumulationMode = Document.AnimationLayerDefinition.ScaleAccumulationMode;
                Weight = Document.AnimationLayerDefinition.Weight;
                Mute = Document.AnimationLayerDefinition.Mute;
                Solo = Document.AnimationLayerDefinition.Solo;
                Lock = Document.AnimationLayerDefinition.Lock;
            }
        }

        public int Compare(FBXAnimationLayer x, FBXAnimationLayer y)
        {
            if (ReferenceEquals(x, y)) return 0;
            if (ReferenceEquals(null, y)) return 1;
            if (ReferenceEquals(null, x)) return -1;
            return x.LayerId.CompareTo(y.LayerId);
        }

        public int CompareTo(FBXAnimationLayer other)
        {
            if (ReferenceEquals(this, other)) return 0;
            if (ReferenceEquals(null, other)) return 1;
            return LayerId.CompareTo(other.LayerId);
        }
    }
}