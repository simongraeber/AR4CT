using System;
using System.Collections.Generic;
using TriLibCore.Collections;
using TriLibCore.Dae.Schema;
using TriLibCore.General;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeMatrices : OrderedDictionary<string, IList<double>>
    {
        public Dictionary<string, ItemsChoiceType7> ElementNames = new Dictionary<string, ItemsChoiceType7>();
        public HashSet<ItemsChoiceType7> CurrentElements = new HashSet<ItemsChoiceType7>();

        private int _unnamedCount;

        public Matrix4x4 TransformMatrices()
        {
            var matrix = Matrix4x4.identity;
            foreach (var kvp in this)
            {
                var elementName = ElementNames[kvp.Key];
                switch (elementName)
                {
                    case ItemsChoiceType7.lookat:
                        break;
                    case ItemsChoiceType7.matrix:
                        var unityMatrix = new Matrix4x4();
                        unityMatrix[0] = (float)kvp.Value[0];
                        unityMatrix[1] = (float)kvp.Value[4];
                        unityMatrix[2] = (float)kvp.Value[8];
                        unityMatrix[3] = (float)kvp.Value[12];

                        unityMatrix[4] = (float)kvp.Value[1];
                        unityMatrix[5] = (float)kvp.Value[5];
                        unityMatrix[6] = (float)kvp.Value[9];
                        unityMatrix[7] = (float)kvp.Value[13];

                        unityMatrix[8] = (float)kvp.Value[2];
                        unityMatrix[9] = (float)kvp.Value[6];
                        unityMatrix[10] = (float)kvp.Value[10];
                        unityMatrix[11] = (float)kvp.Value[14];

                        unityMatrix[12] = (float)kvp.Value[3];
                        unityMatrix[13] = (float)kvp.Value[7];
                        unityMatrix[14] = (float)kvp.Value[11];
                        unityMatrix[15] = (float)kvp.Value[15];
                        matrix *= unityMatrix
;
                        break;
                    case ItemsChoiceType7.rotate:
                        matrix *= Matrix4x4.Rotate(Quaternion.AngleAxis((float)kvp.Value[3], new Vector3((float)kvp.Value[0], (float)kvp.Value[1], (float)kvp.Value[2])));
                        break;
                    case ItemsChoiceType7.scale:
                        matrix *= Matrix4x4.Scale(new Vector3((float)kvp.Value[0], (float)kvp.Value[1], (float)kvp.Value[2]));
                        break;
                    case ItemsChoiceType7.skew:
                        break;
                    case ItemsChoiceType7.translate:
                        matrix *= Matrix4x4.Translate(new Vector3((float)kvp.Value[0], (float)kvp.Value[1], (float)kvp.Value[2]));
                        break;
                }
            }
            return matrix;
        }

        public void AddMatrix(ItemsChoiceType7 elementName, string property, IList<double> matrix)
        {
            if (property == null)
            {
                property = $"Unnamed_{_unnamedCount++}";
            }
            ElementNames.Add(property, elementName);
            Add(property, matrix);
            CurrentElements.Add(elementName);
        }

        public void ClearMatrices()
        {
            Clear();
            ElementNames.Clear();
            CurrentElements.Clear();
        }

        public bool HasMatrix(ItemsChoiceType7 type)
        {
            return CurrentElements.Contains(type);
        }
    }
}