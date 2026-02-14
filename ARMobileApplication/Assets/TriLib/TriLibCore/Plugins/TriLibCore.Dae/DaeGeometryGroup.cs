using System.Collections.Generic;
using TriLibCore.Geometries;
using UnityEngine;

namespace TriLibCore.Dae
{
    public class DaeGeometryGroup : CommonGeometryGroup
    {
        public List<Vector3> VerticesList;
        public List<Vector2> TexCoordsList;
        public List<Vector3> NormalsList;
        public IList<int> Primitives;
    }
}