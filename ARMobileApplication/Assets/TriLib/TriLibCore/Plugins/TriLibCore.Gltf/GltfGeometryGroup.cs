using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Gltf
{
    public class GltfTempGeometryGroup
    {
        public IList<Vector3> Vertices;
        public IList<Vector3> NormalsList;
        public IList<Vector2> UVsList;
        public IList<Color> ColorsList;
        public IList<int> IndicesList;
    }
}