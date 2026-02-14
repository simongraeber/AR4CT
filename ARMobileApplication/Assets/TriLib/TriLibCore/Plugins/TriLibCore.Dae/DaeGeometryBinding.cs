using TriLibCore.Dae.Schema;

namespace TriLibCore.Dae
{
    internal struct DaeGeometryBinding
    {
        public string Source;
        public string Material;
        public triangles Triangles;
        public polylist PolyList;
        public trifans TriFans;
        public tristrips TriStrips;
        public polygons Polygons;
        public InputLocalOffset VerticesInput;
        public InputLocalOffset NormalsInput;
        public InputLocalOffset TexCoordInput;
        public int VerticesInputOffset;
        public int NormalsInputOffset;
        public int TexCoordInputOffset;
        public int BiggestOffset;
    }
}