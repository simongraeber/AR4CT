using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib Light.
    /// </summary>
    public interface ILight : IModel
    {
        /// <summary>
        /// The light type.
        /// </summary>
        LightType LightType { get; set; }

        /// <summary>
        /// The light color.
        /// </summary>
        Color Color { get; set; }

        /// <summary>
        /// The light intensity.
        /// </summary>
        float Intensity { get; set; }

        /// <summary>
        /// The light range.
        /// </summary>
        float Range { get; set; }

        /// <summary>
        /// The light inner spot angle in degrees.
        /// </summary>
        float InnerSpotAngle { get; set; }

        /// <summary>
        /// The light out spot angle in degrees.
        /// </summary>
        float OuterSpotAngle { get; set; }

        /// <summary>
        /// The area light width.
        /// </summary>
        float Width { get; set; }

        /// <summary>
        /// The area light height.
        /// </summary>
        float Height { get; set; }

        /// <summary>
        /// Defines whether the light cast shadows or not.
        /// </summary>
        bool CastShadows { get; set; }
    }
}