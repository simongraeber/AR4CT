using System;
using System.Collections.Generic;
using TriLibCore.Dae.Schema;
using TriLibCore.General;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeAnimationCurve
    {
        public string Object;
        public accessor OutputAccessor;
        public string SubProperty;
        public IList<float> Times;
        public IList<float> Values;
        private const float DefaultTangentValue = 0f;
        private readonly float[] _floatValueA = new float[1];
        private readonly float[] _floatValueB = new float[1];
        private readonly float[] _matrixValueA = new float[16];
        private readonly float[] _matrixValueB = new float[16];
        private readonly float[] _vectorValueA = new float[3];
        private readonly float[] _vectorValueB = new float[3];
        private int _lastIndex;
        public string Property { get; set; }

        public float[] Evaluate(float time, ItemsChoiceType7 itemsChoice)
        {
            GetTwoKeyframes(time, out var keyTimeA, out var keyValueA, out var tangentsA, out var keyTimeB, out var keyValueB, out var tangentsB, itemsChoice);
            return Evaluate(keyTimeA, keyTimeB, keyValueA, keyValueB, tangentsA, tangentsB, time);
        }


        private static float[] Evaluate(float keyTimeA, float keyTimeB, float[] keyValueA, float[] keyValueB, Vector4 tangentsA, Vector4 tangentsB, float time)
        {
            for (var i = 0; i < keyValueA.Length; i++)
            {
                var t = Mathf.InverseLerp(keyTimeA, keyTimeB, time);
                var dt = keyTimeB - keyTimeA;
                var m0 = DefaultTangentValue  * dt;
                var m1 = DefaultTangentValue * dt;
                var t2 = t * t;
                var t3 = t2 * t;
                var a = 2 * t3 - 3 * t2 + 1;
                var b = t3 - 2 * t2 + t;
                var c = t3 - t2;
                var d = -2 * t3 + 3 * t2;
                keyValueA[i] = a * keyValueA[i] + b * m0 + c * m1 + d * keyValueB[i];
            }
            return keyValueA;
        }

        private void GetKeyframe(int keyIndex, out float keyTime, out float[] keyValue, out Vector4 tangents, ItemsChoiceType7 itemsChoice, bool isB = false)
        {
            tangents = default;
            keyTime = Times[keyIndex];
            var valueKeyIndex = keyIndex * (int)OutputAccessor.stride;
            switch (itemsChoice)
            {
                case ItemsChoiceType7.matrix:
                    {
                        keyValue = isB ? _matrixValueB : _matrixValueA;
                        Array.Clear(keyValue, 0, keyValue.Length);
                        for (var i = 0; i < 16; i++)
                        {
                            keyValue[i] = Values[valueKeyIndex + i];
                        }
                        break;
                    }
                case ItemsChoiceType7.translate:
                case ItemsChoiceType7.scale:
                    {
                        keyValue = isB ? _vectorValueB : _vectorValueA;
                        Array.Clear(keyValue, 0, keyValue.Length);
                        for (var i = 0; i < OutputAccessor.param.Length; i++)
                        {
                            var param = OutputAccessor.param[i];
                            switch (param.name)
                            {
                                case "X":
                                    keyValue[0] = Values[valueKeyIndex + i];
                                    break;
                                case "Y":
                                    keyValue[1] = Values[valueKeyIndex + i];
                                    break;
                                case "Z":
                                    keyValue[2] = Values[valueKeyIndex + i];
                                    break;
                            }
                        }
                        break;
                    }
                default:
                    {
                        keyValue = isB ? _floatValueB : _floatValueA;
                        Array.Clear(keyValue, 0, keyValue.Length);
                        keyValue[0] = Values[valueKeyIndex];
                        break;
                    }
            }
        }
        private void GetTwoKeyframes(float time, out float keyTimeA, out float[] keyValueA, out Vector4 tangentsA, out float keyTimeB, out float[] keyValueB, out Vector4 tangentsB, ItemsChoiceType7 itemsChoice)
        {
            for (; ; )
            {
                if (_lastIndex == Times.Count - 1 || Times[_lastIndex] > time)
                {
                    break;
                }
                _lastIndex++;
            }
            GetKeyframe(_lastIndex > 0 ? _lastIndex - 1 : _lastIndex, out keyTimeA, out keyValueA, out tangentsA, itemsChoice);
            GetKeyframe(_lastIndex, out keyTimeB, out keyValueB, out tangentsB, itemsChoice, true);
        }
    }
}