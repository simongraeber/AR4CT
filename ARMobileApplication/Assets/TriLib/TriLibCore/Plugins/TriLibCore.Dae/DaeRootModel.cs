using System.Collections.Generic;
using TriLibCore.Interfaces;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeRootModel : DaeModel, IRootModel
    {
        public List<IModel> AllModels { get; set; } = new List<IModel>();
        public List<IGeometryGroup> AllGeometryGroups { get; set; } = new List<IGeometryGroup>();
        public List<IAnimation> AllAnimations { get; set; }= new List<IAnimation>();
        public List<IMaterial> AllMaterials { get; set; } = new List<IMaterial>();
        public List<ITexture> AllTextures { get; set; } = new List<ITexture>();
        public List<ICamera> AllCameras { get; set; }
        public List<ILight> AllLights { get; set; }
    }
}
