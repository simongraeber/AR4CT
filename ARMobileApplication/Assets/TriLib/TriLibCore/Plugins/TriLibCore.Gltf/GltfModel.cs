using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfModel : IModel
    {
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
        
        public List<IModel> Bones { get; set; }
        public Matrix4x4[] BindPoses { get; set; }
        public int[] MaterialIndices { get; set; }
        public Dictionary<string, object> UserProperties { get; set; } = new Dictionary<string, object>();

        public bool HasCustomPivot { get; set; }

        public Matrix4x4 OriginalGlobalMatrix { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public virtual Matrix4x4 GetLocalMatrix()
        {
            return Matrix4x4.TRS(LocalPosition, LocalRotation, LocalScale);
        }

        public Matrix4x4 GetGlobalMatrix()
        {
            var matrix = GetLocalMatrix();
            var parent = Parent as GltfModel;
            while (parent != null)
            {
                matrix = parent.GetLocalMatrix() * matrix;
                parent = parent.Parent as GltfModel;
            }
            return matrix;
        }
    }
}