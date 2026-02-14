using System;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a Material Shading Setup. Used by Material Mappers to select the most suitable Unity Material template.
    /// </summary>
    [Flags]
    public enum MaterialShadingSetup
    {
        MetallicSmoothness = 1 << 0,
        MetallicRoughness = 1 << 1,
        SpecGlossiness = 1 << 2,
        SpecRoughness = 1 << 3,
        PhongLambert = 1 << 4
    }
}