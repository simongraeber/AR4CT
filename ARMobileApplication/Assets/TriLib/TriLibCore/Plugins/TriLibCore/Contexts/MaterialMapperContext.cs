using TriLibCore.General;
using TriLibCore.Interfaces;
using TriLibCore.Mappers;
using UnityEngine;

namespace TriLibCore
{
    /// <summary>
    /// Acts as a container for data used when converting a TriLib <see cref="IMaterial"/> 
    /// into a Unity <see cref="Material"/>. This includes both the original material information,
    /// an intermediate <see cref="VirtualMaterial"/> representation, and the final Unity 
    /// material reference, all of which are coordinated by a corresponding <see cref="MaterialMapper"/>.
    /// </summary>
    public class MaterialMapperContext : IAssetLoaderContext, IAwaitable
    {
        /// <summary>
        /// The original <see cref="IMaterial"/> source data, extracted from the loaded model.
        /// This object contains property values (e.g., texture references, color properties) 
        /// that the <see cref="MaterialMapper"/> can process.
        /// </summary>
        public IMaterial Material;

        /// <summary>
        /// A “virtual” material object that stores property values (colors, textures, floats, keywords)
        /// used to build the final <see cref="UnityMaterial"/>. This intermediate stage allows 
        /// for layering or composition of multiple properties before applying them to the actual material.
        /// </summary>
        public VirtualMaterial VirtualMaterial;

        /// <summary>
        /// The final Unity material instance after the mapper has assigned textures and property values.
        /// This is usually created by instantiating a preset (e.g., a built-in TriLib material) 
        /// or a custom pipeline material, then applying <see cref="VirtualMaterial"/> properties.
        /// </summary>
        public Material UnityMaterial;

        /// <summary>
        /// Gets or sets the <see cref="AssetLoaderContext"/> that contains the overall data 
        /// (e.g., root <see cref="GameObject"/>, import options, callbacks) for the current model loading process.
        /// </summary>
        public AssetLoaderContext Context { get; set; }

        /// <summary>
        /// Optional reference to an “alpha” material used for situations where a second pass is needed 
        /// to render partially transparent geometry. For example, if <see cref="AssetLoaderOptions.AlphaMaterialMode"/> 
        /// is set to <c>CutoutAndTransparent</c>, this field might hold a secondary material for layered blending.
        /// </summary>
        public Material AlphaMaterial;

        /// <summary>
        /// The <see cref="MaterialMapper"/> instance responsible for converting 
        /// the TriLib <see cref="IMaterial"/> to the final Unity <see cref="Material"/>. 
        /// This reference is useful if additional mapper-specific logic or settings are needed 
        /// during or after processing.
        /// </summary>
        public MaterialMapper MaterialMapper;

        /// <summary>
        /// The zero-based index of this material among all parsed materials in the loaded model,
        /// which can be useful for tracking or referencing the original material order.
        /// </summary>
        public int Index;

        /// <summary>
        /// Indicates whether material processing is finished. This can be <c>false</c> if 
        /// asynchronous texture loading or other operations are still ongoing, 
        /// or <c>true</c> if <see cref="UnityMaterial"/> is fully assigned and ready.
        /// </summary>
        public bool Completed { get; set; }
    }
}
