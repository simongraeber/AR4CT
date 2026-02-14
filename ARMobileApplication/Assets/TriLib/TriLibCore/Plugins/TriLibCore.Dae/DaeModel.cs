using System.Collections.Generic;
using TriLibCore.Dae.Schema;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeModel : IModel
    {
        //todo: remove
        internal node Node;

        internal SortedSet<float> AnimatedTimes;
        
        public string Name { get; set; }
        public bool Used { get; set; }
        public Vector3 Pivot { get; set; }
        public Vector3 LocalPosition { get; set; }
        public Quaternion LocalRotation { get; set; }
        public Vector3 LocalScale { get; set; }
        public bool Visibility { get; set; }
        public IModel Parent { get; set; }
        public List<IModel> Children { get; set; }
        public bool IsBone { get; set; }
        public IGeometryGroup GeometryGroup { get; set; }
        public List<IAnimation> Animations { get; set; }
        public List<IMaterial> Materials { get; set; }
        public List<IModel> Bones { get; set; }
        public Matrix4x4[] BindPoses { get; set; }
        public int[] MaterialIndices { get; set; }
        public Dictionary<string, object> UserProperties { get; set; }
        public bool HasCustomPivot { get; set; }
        public Matrix4x4 OriginalGlobalMatrix { get; set; }

        public DaeMatrices Matrices = new DaeMatrices();
    }
}