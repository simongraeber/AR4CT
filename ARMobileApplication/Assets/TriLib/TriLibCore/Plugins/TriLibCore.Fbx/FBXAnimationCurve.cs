using System;
using System.Collections.Generic;
using TriLibCore.Utils;
using UnityEngine;

namespace TriLibCore.Fbx
{
    public class FBXAnimationCurve : FBXObject
    {
        public struct KeyTangent
        {
            /// <summary>Slope left.</summary>
            public float SlopeLeft;

            /// <summary>Weight left.</summary>
            public float WeightLeft;

            /// <summary>Slope right.</summary>
            public float SlopeRight;

            /// <summary>Weight right.</summary>
            public float WeightRight;

            public static KeyTangent GetDefault()
            {
                return new KeyTangent { SlopeLeft = 1f / 3f, SlopeRight = 1f / 3f };
            }
        }

        public const int eTangentAuto = 0x00000100;
        public const int eTangentTCB = 0x00000200;
        public const int eTangentUser = 0x00000400;
        public const int eTangentGenericBreak = 0x00000800;
        public const int eTangentBreak = eTangentGenericBreak | eTangentUser;
        public const int eTangentAutoBreak = eTangentGenericBreak | eTangentAuto;
        public const int eTangentGenericClamp = 0x00001000;
        public const int eTangentGenericTimeIndependent = 0x00002000;
        public const int eTangentGenericClampProgressive = 0x00004000 | eTangentGenericTimeIndependent;

        public const int eInterpolationConstant = 0x00000002;
        public const int eInterpolationLinear = 0x00000004;
        public const int eInterpolationCubic = 0x00000008;

        public const int eConstant = 1;
        public const int eRepetition = 2;
        public const int eMirrorRepetition = 3;
        public const int eKeepSlope = 4;

        public const int eWeightedNone = 0x00000000;
        public const int eWeightedRight = 0x01000000;
        public const int eWeightedNextLeft = 0x02000000;
        public const int eWeightedAll = eWeightedRight | eWeightedNextLeft;

        public const int eConstantStandard = 0x00000000;
        public const int eConstantNext = 0x00000100;

        public const int eRightSlope = 0;
        public const int eNextLeftSlope = 1;
        public const int eWeights = 2;
        public const int eRightWeight = 2;
        public const int eNextLeftWeight = 3;
        public const int eVelocity = 4;
        public const int eRightVelocity = 4;
        public const int eNextLeftVelocity = 5;
        public const int eTCBTension = 0;
        public const int eTCBContinuity = 1;
        public const int eTCBBias = 2;

        public const int LeftTangentIndex = 0;
        public const int RightTangentIndex = 1;
        public const int LeftWeightIndex = 2;
        public const int RightWeightIndex = 3;

        public object Default; 

        public IList<long> KeyTimes; 
        public IList<float> KeyValues; 
        public IList<int> KeyAttrRefCounts; 
        public IList<float> KeyAttrDataValues; 
        public IList<int> KeyFlags; 

        public int PreExtrapolationType; 
        public int PreExtrapolationRepetition; 
        public int PostExtrapolationType; 
        public int PostExtrapolationRepetition; 

        public FBXAnimationCurveNodeBinding AnimationCurveNodeBinding;

        public FBXNode Node;

        private IList<KeyTangent> _keyTangents;
        private bool _initialized;
        private int _lastIndex;
        private float _nextLeftSlope;
        private float _nextLeftWeight;

        private readonly byte[] _bytesFloat = new byte[sizeof(float)]; //todo: move to another centered class

        private int _keyAttrDataValuesCount;
        private int _keyTimesCount;

        private unsafe byte[] GetBytesFloat(float value, int offset)
        {
            var array = _bytesFloat;
            offset = Mathf.Clamp(offset, 0, array.Length - 1);
            fixed (byte* ptr = &array[offset])
            {
                *(float*)ptr = value;
            }
            return array;
        }

        public FBXAnimationCurve(FBXDocument document, string name, long objectId, string objectClass) : base(document, name, objectId, objectClass)
        {
            ObjectType = FBXObjectType.AnimationCurve;
            if (objectId > -1)
            {
                LoadDefinition();
            }
        }

        public void Initialize()
        {
            if (_initialized)
            {
                return;
            }

            _nextLeftSlope = 0f;
            _nextLeftWeight = 1f / 3f;

            _keyTimesCount = KeyTimes.Count;
            _keyAttrDataValuesCount = KeyAttrDataValues.Count;
            _keyTangents = new KeyTangent[_keyTimesCount];

            ProcessCurvesAttr();

            _initialized = true;
        }

        public sealed override void LoadDefinition()
        {
            if (Document.AnimationCurveDefinition != null)
            {
                Default = Document.AnimationCurveDefinition.Default;
            }
        }

        public void GetKeyframe(int keyIndex, out float keyTime, out float keyValue, out int keyFlags, out KeyTangent keyTangents)
        {
            keyTangents = _keyTangents[keyIndex];
            keyTime = Document.ConvertFromFBXTime(KeyTimes[keyIndex]);
            keyValue = KeyValues[keyIndex];
            keyFlags = KeyFlags[keyIndex];
        }

        public void GetKeyframe(long time, out float keyTime, out float keyValue, out int keyFlags, out KeyTangent keyTangents)
        {
            for (; ; )
            {
                if (_lastIndex == _keyTimesCount - 1 || KeyTimes[_lastIndex] >= time)
                {
                    break;
                }
                _lastIndex++;
            }
            GetKeyframe(_lastIndex, out keyTime, out keyValue, out keyFlags, out keyTangents);
        }

        private void GetTwoKeyframes(
            long time,
            out float keyTimeA,
            out float keyValueA,
            out int keyFlagsA,
            out KeyTangent tangentsA,
            out float keyTimeB,
            out float keyValueB,
            out int keyFlagsB,
            out KeyTangent tangentsB
        )
        {
            for (; ; )
            {
                if (_lastIndex == _keyTimesCount - 1 || KeyTimes[_lastIndex] > time)
                {
                    break;
                }
                _lastIndex++;
            }
            GetKeyframe(_lastIndex > 0 ? _lastIndex - 1 : _lastIndex, out keyTimeA, out keyValueA, out keyFlagsA, out tangentsA);
            GetKeyframe(_lastIndex, out keyTimeB, out keyValueB, out keyFlagsB, out tangentsB);
        }

        public float Evaluate(long time, out float keyTime)
        {
            keyTime = Document.ConvertFromFBXTime(time);

            GetTwoKeyframes(
                time,
                out var keyTimeA,
                out var keyValueA,
                out var keyFlagsA,
                out var keyTangentA,
                out var keyTimeB,
                out var keyValueB,
                out var keyFlagsB,
                out var keyTangentB
            );

            var t = Mathf.InverseLerp(keyTimeA, keyTimeB, keyTime);
            var dt = keyTimeB - keyTimeA;

            var m0 = keyTangentA.SlopeRight * dt;
            var m1 = keyTangentB.SlopeLeft * dt;

            var t2 = t * t;
            var t3 = t2 * t;

            var a = 2 * t3 - 3 * t2 + 1;
            var b = t3 - 2 * t2 + t;
            var c = t3 - t2;
            var d = -2 * t3 + 3 * t2;

            return a * keyValueA + b * m0 + c * m1 + d * keyValueB;
        }

        private static float SolveAutoTangentLeft(float prevTime, float time, float prevValue, float value, float weightLeft, float autoBias, int flags, float clampThreshold)
        {
            if (FlagUtils.HasFlag(flags, eTangentGenericClampProgressive))
            {
                return 0.0f;
            }

            if (FlagUtils.HasFlag(flags, eTangentGenericClamp))
            {
                if (Mathf.Abs(prevValue - value) <= clampThreshold)
                {
                    return 0.0f;
                }
            }

            var slope = (value - prevValue) / (time - prevTime);

            if (FlagUtils.HasFlag(flags, eTangentGenericTimeIndependent))
            {
                var absBiasWeight = Mathf.Abs(autoBias) / 100.0f - 5.0f;
                if (absBiasWeight > 0.0f)
                {
                    var biasSign = autoBias > 0.0f ? 1.0f : -1.0f;
                    slope += absBiasWeight * absBiasWeight * biasSign * 40.0f;
                }
            }

            return slope;
        }

        private static float SolveAutoTangentRight(float time, float nextTime, float value, float nextValue, float weightRight, float autoBias, int flags, float clampThreshold)
        {
            if (FlagUtils.HasFlag(flags, eTangentGenericClampProgressive))
            {
                return 0.0f;
            }

            if (FlagUtils.HasFlag(flags, eTangentGenericClamp))
            {
                if (Mathf.Abs(nextValue - value) <= clampThreshold)
                {
                    return 0.0f;
                }
            }

            var slope = (nextValue - value) / (nextTime - time);

            if (FlagUtils.HasFlag(flags, eTangentGenericTimeIndependent))
            {
                var absBiasWeight = Mathf.Abs(autoBias) / 100.0f - 5.0f;
                if (absBiasWeight > 0.0)
                {
                    var biasSign = autoBias > 0.0f ? 1.0f : -1.0f;
                    slope += absBiasWeight * absBiasWeight * biasSign * 40.0f;
                }
            }

            return slope;
        }

        private static float SolveAutoTangent(
            float prevTime,
            float time,
            float nextTime,
            float prevValue,
            float value,
            float nextValue,
            float weightLeft,
            float weightRight,
            int flags,
            float autoBias,
            float clampThreshold
        )
        {
            if (FlagUtils.HasFlag(flags, eTangentGenericClamp))
            {
                if (Mathf.Min(Mathf.Abs(prevValue - value), Mathf.Abs(nextValue - value)) <= clampThreshold)
                {
                    return 0.0f;
                }
            }

            var slope = (nextValue - prevValue) / (nextTime - prevTime);

            if (FlagUtils.HasFlag(flags, eTangentGenericTimeIndependent))
            {
                var slopeLeft = (value - prevValue) / (time - prevTime);
                var slopeRight = (nextValue - value) / (nextTime - time);
                var delta = (time - prevTime) / (nextTime - prevTime);
                slope = slope * 0.5f + (slopeLeft * (1.0f - delta) + slopeRight * delta) * 0.5f;

                var biasWeight = Mathf.Abs(autoBias) / 100.0f;
                if (biasWeight > 0.0001)
                {
                    var biasTarget = autoBias > 0.0f ? slopeRight : slopeLeft;
                    var biasDelta = biasTarget - slope;
                    slope = slope * (1.0f - biasWeight) + biasTarget * biasWeight;
                    var absBiasWeight = biasWeight - 5.0f;
                    if (absBiasWeight > 0.0)
                    {
                        var biasSign = Mathf.Abs(biasDelta) > 0.00001f ? biasDelta : autoBias;
                        biasSign = biasSign > 0.0f ? 1.0f : -1.0f;
                        slope += absBiasWeight * absBiasWeight * biasSign * 40.0f;
                    }
                }
            }

            if (FlagUtils.HasFlag(flags, eTangentGenericClampProgressive))
            {
                var slopeSign = slope >= 0.0f ? 1.0f : -1.0f;
                var absSlope = slopeSign * slope;

                var rangeLeft = weightLeft * (time - prevTime);
                var rangeRight = weightRight * (nextTime - time);

                var maxLeft = rangeLeft > 0.0f ? slopeSign * (value - prevValue) / rangeLeft : 0.0f;
                var maxRight = rangeRight > 0.0f ? slopeSign * (nextValue - value) / rangeRight : 0.0f;

                if (!(maxLeft > 0.0f)) maxLeft = 0.0f;
                if (!(maxRight > 0.0f)) maxRight = 0.0f;

                if (absSlope > maxLeft) absSlope = maxLeft;
                if (absSlope > maxRight) absSlope = maxRight;

                slope = slopeSign * absSlope;
            }

            return slope;
        }

        private void ProcessCurvesAttr()
        {
            var attrIndex = 0;
            var attrElementIndex = 0;

            var slopeLeft = 0.0f;
            var weightLeft = 0.333333f;

            var clampThreshold = 0.1f;

            for (var currKeyIndex = 0; currKeyIndex < _keyTimesCount; currKeyIndex++)
            {
                var nextRefCount = KeyAttrRefCounts[attrIndex];
                var keyFlags = KeyFlags[attrIndex];

                var arrayIndex = attrIndex * 4;
                var rightSlope = GetCurveAttr(arrayIndex + eRightSlope);
                var nextLeftSlope = GetCurveAttr(arrayIndex + eNextLeftSlope);

                var weights = GetCurveAttr(arrayIndex + eWeights);
                var weightBytes = GetBytesFloat(weights, 0);
                var rightWeight = BitConverter.ToInt16(weightBytes, 0) / 10000f;
                var nextLeftWeight = BitConverter.ToInt16(weightBytes, 2) / 10000f;

                var prevKeyIndex = currKeyIndex - 1;
                var nextKeyIndex = currKeyIndex + 1;

                var nextKeyValue = KeyValues[nextKeyIndex];
                var prevKeyValue = KeyValues[prevKeyIndex];
                var currKeyValue = KeyValues[currKeyIndex];

                var nextKeyTime = Document.ConvertFromFBXTime(KeyTimes[nextKeyIndex]);
                var prevKeyTime = Document.ConvertFromFBXTime(KeyTimes[prevKeyIndex]);
                var currKeyTime = Document.ConvertFromFBXTime(KeyTimes[currKeyIndex]);

                var slopeRight = rightSlope;
                var weightRight = rightWeight;
                var nextSlopeLeft = nextLeftSlope;
                var nextWeightLeft = nextLeftWeight;

                if (FlagUtils.HasFlag(keyFlags, eInterpolationConstant))
                {
                    weightRight = nextWeightLeft = 0.333333f;
                    slopeRight = nextSlopeLeft = 0.0f;
                }
                else if (FlagUtils.HasFlag(keyFlags, eInterpolationCubic))
                {
                    if (!FlagUtils.HasFlag(keyFlags, eTangentUser))
                    {
                        if (currKeyIndex > 0 && nextKeyIndex < _keyTimesCount && currKeyTime > prevKeyTime && nextKeyTime > currKeyTime)
                        {
                            if (Mathf.Abs(slopeLeft + slopeRight) <= 0.0001f)
                            {
                                slopeLeft = slopeRight = SolveAutoTangent(
                                    prevKeyTime,
                                    currKeyTime,
                                    nextKeyTime,
                                    prevKeyValue,
                                    currKeyValue,
                                    nextKeyValue,
                                    weightLeft,
                                    weightRight,
                                    keyFlags,
                                    slopeRight,
                                    clampThreshold
                                );
                            }
                            else
                            {
                                slopeLeft = SolveAutoTangent(
                                    prevKeyTime,
                                    currKeyTime,
                                    nextKeyTime,
                                    prevKeyValue,
                                    currKeyValue,
                                    nextKeyValue,
                                    weightLeft,
                                    weightRight,
                                    keyFlags,
                                    -slopeLeft,
                                    clampThreshold
                                );
                                slopeRight = SolveAutoTangent(
                                    prevKeyTime,
                                    currKeyTime,
                                    nextKeyTime,
                                    prevKeyValue,
                                    currKeyValue,
                                    nextKeyValue,
                                    weightLeft,
                                    weightRight,
                                    keyFlags,
                                    slopeRight,
                                    clampThreshold
                                );
                            }
                        }
                        else if (currKeyIndex > 0 && currKeyTime > prevKeyTime)
                        {
                            slopeLeft = slopeRight = SolveAutoTangentLeft(
                                prevKeyTime,
                                currKeyTime,
                                prevKeyValue,
                                currKeyValue,
                                weightLeft,
                                -slopeLeft,
                                keyFlags,
                                clampThreshold
                            );
                        }
                        else if (nextKeyIndex < _keyTimesCount && nextKeyTime > currKeyTime)
                        {
                            slopeLeft = slopeRight = SolveAutoTangentRight(
                                currKeyTime,
                                nextKeyTime,
                                currKeyValue,
                                nextKeyValue,
                                weightRight,
                                slopeRight,
                                keyFlags,
                                clampThreshold
                            );
                        }
                        else
                        {
                            slopeLeft = slopeRight = 0.0f;
                        }
                    }
                }

                var keyTangents = new KeyTangent();

                if (currKeyTime > prevKeyTime)
                {
                    var delta = currKeyTime - prevKeyTime;
                    keyTangents.WeightLeft = weightLeft * delta;
                    keyTangents.SlopeLeft = slopeLeft;
                }
                else
                {
                    keyTangents.WeightLeft = 0.0f;
                    keyTangents.SlopeLeft = 0.0f;
                }

                if (nextKeyTime > currKeyTime)
                {
                    var delta = nextKeyTime - currKeyTime;
                    keyTangents.WeightRight = weightRight * delta;
                    keyTangents.SlopeRight = slopeRight;
                }
                else
                {
                    keyTangents.WeightRight = 0.0f;
                    keyTangents.SlopeRight = 0.0f;
                }

                slopeLeft = nextSlopeLeft;
                weightLeft = nextWeightLeft;

                _keyTangents[currKeyIndex] = keyTangents;

                if (++attrElementIndex >= nextRefCount)
                {
                    attrIndex++;
                    attrElementIndex = 0;
                }
            }
        }

        private float GetCurveAttr(int index)
        {
            return index >= _keyAttrDataValuesCount ? 0f : KeyAttrDataValues[index];
        }
    }
}
