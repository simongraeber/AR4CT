using System.Collections.Generic;
using IxMilia.ThreeMf;
using TriLibCore.Interfaces;

namespace TriLibCore.ThreeMf
{
    public class ThreeMfRootModel : ThreeMfModel, IRootModel
    {
        public List<IModel> AllModels { get; set; }
        public List<IGeometryGroup> AllGeometryGroups { get; set; } = new List<IGeometryGroup>();
        public List<IAnimation> AllAnimations { get; set; }
        public List<IMaterial> AllMaterials { get; set; } = new List<IMaterial>();
        public List<ITexture> AllTextures { get; set; } = new List<ITexture>();
        public List<ICamera> AllCameras { get; set; }
        public List<ILight> AllLights { get; set; }
        public ThreeMfFile File;
    }
}