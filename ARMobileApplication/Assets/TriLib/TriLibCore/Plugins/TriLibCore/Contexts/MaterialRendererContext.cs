using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore
{
    /// <summary>
    /// Holds contextual information for assigning a TriLib-supplied <see cref="IMaterial"/> 
    /// to a specific Unity <see cref="Renderer"/> at a particular sub-mesh index. 
    /// This class also references the corresponding <see cref="MaterialMapperContext"/> 
    /// and the broader <see cref="AssetLoaderContext"/> to unify data during material application.
    /// </summary>
    public class MaterialRendererContext : IAssetLoaderContext, IAwaitable
    {
        /// <summary>
        /// The Unity <see cref="Renderer"/> (e.g., <see cref="MeshRenderer"/>, 
        /// <see cref="SkinnedMeshRenderer"/>) that will receive the final material assignment.
        /// </summary>
        public Renderer Renderer;

        /// <summary>
        /// The sub-mesh index within the <see cref="Renderer"/> 
        /// that should be assigned the final Unity <see cref="Material"/>. 
        /// This corresponds to a geometry slot in the mesh.
        /// </summary>
        public int GeometryIndex;

        /// <summary>
        /// The original TriLib <see cref="IMaterial"/> object that contains
        /// surface properties (e.g., textures, colors) to be mapped or converted 
        /// into a Unity <see cref="Material"/>.
        /// </summary>
        public IMaterial Material;

        /// <summary>
        /// Reference to the <see cref="AssetLoaderContext"/> which holds the overarching model 
        /// loading details. This includes references to loaded <see cref="GameObject"/>s, 
        /// import settings, tasks, and callbacks.
        /// </summary>
        public AssetLoaderContext Context { get; set; }

        /// <summary>
        /// Reference to the <see cref="MaterialMapperContext"/> that is managing the conversion 
        /// of this TriLib <see cref="IMaterial"/> into a Unity <see cref="Material"/>. 
        /// This object stores intermediate data.
        /// </summary>
        public MaterialMapperContext MaterialMapperContext { get; set; }

        /// <summary>
        /// The specific <see cref="Mesh"/> (or <see cref="MeshFilter.mesh"/> / <see cref="SkinnedMeshRenderer.sharedMesh"/>)
        /// to which this material is being applied. This reference can be null if not required 
        /// by the mapper logic (e.g., for blend shape or multi-pass scenarios).
        /// </summary>
        public Mesh Mesh { get; set; }

        /// <summary>
        /// Indicates whether the material application has completed. 
        /// Can be <c>false</c> if asynchronous texture loading or other tasks are still in progress.
        /// </summary>
        public bool Completed { get; set; }
    }
}
