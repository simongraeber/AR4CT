using System.Collections.Generic;

namespace TriLibCore.Interfaces
{
    /// <summary>Represents the top-most (root) TriLib Model of the hierarchy.</summary>
    public interface IRootModel : IModel
    {
        /// <summary>Gets/Sets all Models.</summary>
        List<IModel> AllModels { get; set; }

        // <summary>Gets/Sets all Model Geometry Groups.</summary>
        List<IGeometryGroup> AllGeometryGroups { get; set; }

        // <summary>Gets/Sets all Model Animations.</summary>
        List<IAnimation> AllAnimations { get; set; }

        // <summary>Gets/Sets all Model Materials.</summary>
        List<IMaterial> AllMaterials { get; set; }

        // <summary>Gets/Sets all Model Textures.</summary>
        List<ITexture> AllTextures { get; set; }

        /// <summary>
        /// Gets/Sets all Model Cameras.
        /// </summary>
        List<ICamera> AllCameras { get; set; }

        /// <summary>
        /// Gets/Sets all Model Lights.
        /// </summary>
        List<ILight> AllLights { get; set; }
    }
}