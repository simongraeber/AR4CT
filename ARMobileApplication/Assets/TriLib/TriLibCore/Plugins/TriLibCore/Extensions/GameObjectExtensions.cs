using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>Represents a series of Game Object extension methods.</summary>
    public static class GameObjectExtensions
    {
        /// <summary>
        /// Moves the bones in the object hierarchy to their binding poses.
        /// </summary>
        /// <param name="gameObject">The hierarchy root.</param>
        public static void SampleBindPose(GameObject gameObject)
        {
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            for (var j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                var skinnedMeshRenderer = skinnedMeshRenderers[j];
                var skinMatrix = skinnedMeshRenderer.transform.localToWorldMatrix;
                var parentTransforms = new Dictionary<Transform, Transform>(skinnedMeshRenderer.bones.Length);
                for (var k = 0; k < skinnedMeshRenderer.bones.Length; k++)
                {
                    var bone = skinnedMeshRenderer.bones[k];
                    parentTransforms[bone] = bone.parent;
                    bone.SetParent(null, true);
                }
                for (var i = 0; i < skinnedMeshRenderer.bones.Length; i++)
                {
                    var bone = skinnedMeshRenderer.bones[i];
                    var bindMatrixGlobal = skinMatrix * skinnedMeshRenderer.sharedMesh.bindposes[i].inverse;
                    var matrixX = new Vector3(bindMatrixGlobal.m00, bindMatrixGlobal.m10, bindMatrixGlobal.m20);
                    var matrixY = new Vector3(bindMatrixGlobal.m01, bindMatrixGlobal.m11, bindMatrixGlobal.m21);
                    var matrixZ = new Vector3(bindMatrixGlobal.m02, bindMatrixGlobal.m12, bindMatrixGlobal.m22);
                    var matrixP = new Vector3(bindMatrixGlobal.m03, bindMatrixGlobal.m13, bindMatrixGlobal.m23);
                    bone.position = matrixP * (Mathf.Abs(bone.lossyScale.z) / matrixZ.magnitude);
                    bone.rotation = Vector3.Dot(Vector3.Cross(matrixX, matrixY), matrixZ) >= 0 ? Quaternion.LookRotation(matrixZ, matrixY) : Quaternion.LookRotation(-matrixZ, -matrixY);
                }
                for (var k = 0; k < skinnedMeshRenderer.bones.Length; k++)
                {
                    var bone = skinnedMeshRenderer.bones[k];
                    bone.SetParent(parentTransforms[bone], true);
                }
            }
        }

        /// <summary>
        /// Calculates the precise Bounds (including Meshes) of the given GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject used to calculate the Bounds.</param>
        /// <returns>The calculated Bounds.</returns>
        public static Bounds CalculatePreciseBounds(this GameObject gameObject)
        {
            var bounds = new Bounds();
            var boundsSet = false;
            var meshFilters = gameObject.GetComponentsInChildren<MeshFilter>();
            if (meshFilters.Length > 0)
            {
                bounds = GetMeshBounds(meshFilters[0].gameObject, meshFilters[0].sharedMesh);
                boundsSet = true;
                for (var i = 1; i < meshFilters.Length; i++)
                {
                    bounds.Encapsulate(GetMeshBounds(meshFilters[i].gameObject, meshFilters[i].sharedMesh));
                }
            }
            var skinnedMeshRenderers = gameObject.GetComponentsInChildren<SkinnedMeshRenderer>();
            if (skinnedMeshRenderers.Length > 0)
            {
                var mesh = new Mesh();
                if (!boundsSet)
                {
                    skinnedMeshRenderers[0].BakeMesh(mesh);
                    bounds = GetMeshBounds(skinnedMeshRenderers[0].gameObject, mesh);
                }
                for (var i = 1; i < skinnedMeshRenderers.Length; i++)
                {
                    skinnedMeshRenderers[i].BakeMesh(mesh);
                    bounds = GetMeshBounds(skinnedMeshRenderers[i].gameObject, mesh);
                }
                Object.Destroy(mesh);
            }
            return bounds;
        }

        /// <summary>
        /// Returns bounds using the given Mesh vertices multiplied by the given GameObject Transform.
        /// </summary>
        /// <param name="gameObject">The GameObject to get the bounds from.</param>
        /// <param name="mesh">The Mesh to be transformed by the GameObject transform.</param>
        /// <returns>The transformed Bounds.</returns>
        private static Bounds GetMeshBounds(GameObject gameObject, Mesh mesh)
        {
            var bounds = new Bounds();
            var vertices = mesh.vertices;
            if (vertices.Length > 0)
            {
                bounds = new Bounds(gameObject.transform.TransformPoint(vertices[0]), Vector3.zero);
                for (var i = 1; i < vertices.Length; i++)
                {
                    bounds.Encapsulate(gameObject.transform.TransformPoint(vertices[i]));
                }
            }
            return bounds;
        }

        /// <summary>Calculates this Game Object Bounds.</summary>
        /// <param name="gameObject">The GameObject to calculate the Bounds.</param>
        /// <param name="localSpace">Pass <c>true</c> to calculate the Bounds in local space.</param>
        /// <returns>The calculated Bounds.</returns>
        public static Bounds CalculateBounds(this GameObject gameObject, bool localSpace = false)
        {
            var position = gameObject.transform.position;
            var rotation = gameObject.transform.rotation;
            var scale = gameObject.transform.localScale;
            if (localSpace)
            {
                gameObject.transform.position = Vector3.zero;
                gameObject.transform.rotation = Quaternion.identity;
                gameObject.transform.localScale = Vector3.one;
            }
            var bounds = new Bounds();
            var renderers = gameObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0)
            {
                var firstBounds = renderers[0].bounds;
                bounds.center = firstBounds.center;
                bounds.extents = firstBounds.extents;
                for (var i = 1; i < renderers.Length; i++)
                {
                    var renderer = renderers[i];
                    var bounds2 = renderer.bounds;
                    bounds.Encapsulate(bounds2);
                }
                if (bounds.size.magnitude < 0.001f)
                {
                    bounds = CalculatePreciseBounds(gameObject);
                }
            }
            else
            {
                bounds = CalculatePreciseBounds(gameObject);
            }
            if (localSpace)
            {
                gameObject.transform.position = position;
                gameObject.transform.rotation = rotation;
                gameObject.transform.localScale = scale;
            }
            return bounds;
        }
    }
}