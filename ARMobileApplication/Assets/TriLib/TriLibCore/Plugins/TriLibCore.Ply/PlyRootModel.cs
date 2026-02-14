using System.Collections.Generic;
using TriLibCore.Interfaces;

namespace TriLibCore.Ply
{
    public class PlyRootModel : PlyModel, IRootModel
    {
        public List<IModel> AllModels { get; set; }
        public List<IGeometryGroup> AllGeometryGroups { get; set; }
        public List<IAnimation> AllAnimations { get; set; }
        public List<IMaterial> AllMaterials { get; set; }
        public List<ITexture> AllTextures { get; set; }
        public List<ICamera> AllCameras { get; set; }
        public List<ILight> AllLights { get; set; }
    }
}