using System.Collections.Generic;
using UnityEngine;

namespace TriLibCore.Interfaces
{
    /// <summary>
    /// Represents a TriLib blend shape key (a single blend shape target/frame),
    /// including delta attributes and an optional mapping to the base geometry.
    /// </summary>
    public interface IBlendShapeKey : IObject
    {
        /// <summary>
        /// Gets or sets a mapping where the key is the original vertex index (from the base mesh)
        /// and the value is the index into the blend shape delta arrays (<see cref="Vertices"/>,
        /// <see cref="Normals"/>, <see cref="Tangents"/>).
        /// </summary>
        /// <remarks>
        /// This mapping is used when the blend shape does not provide deltas for every vertex.
        /// When <see cref="FullGeometryShape"/> is <c>true</c>, this mapping may be unused.
        /// </remarks>
        Dictionary<int, int> IndexMap { get; set; }

        /// <summary>
        /// Gets or sets the delta vertex positions for this blend shape key.
        /// </summary>
        /// <remarks>
        /// Each entry represents the positional delta to be applied to the base mesh vertex.
        /// When <see cref="FullGeometryShape"/> is <c>false</c>, the indices are addressed through <see cref="IndexMap"/>.
        /// </remarks>
        List<Vector3> Vertices { get; set; }

        /// <summary>
        /// Gets or sets the delta normals for this blend shape key.
        /// </summary>
        /// <remarks>
        /// When not provided, normals may be calculated depending on importer options.
        /// When <see cref="FullGeometryShape"/> is <c>false</c>, the indices are addressed through <see cref="IndexMap"/>.
        /// </remarks>
        List<Vector3> Normals { get; set; }

        /// <summary>
        /// Gets or sets the delta tangents for this blend shape key.
        /// </summary>
        /// <remarks>
        /// When not provided, tangents may be calculated depending on importer options.
        /// When <see cref="FullGeometryShape"/> is <c>false</c>, the indices are addressed through <see cref="IndexMap"/>.
        /// </remarks>
        List<Vector3> Tangents { get; set; }

        /// <summary>
        /// Gets or sets the weight of this blend shape frame, as used by Unity when adding the frame.
        /// </summary>
        float FrameWeight { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this blend shape provides data for the full geometry
        /// (i.e., deltas exist for all vertices and no index mapping is required).
        /// </summary>
        /// <remarks>
        /// When <c>true</c>, consumers may ignore <see cref="IndexMap"/> and treat delta arrays as
        /// aligned with the base geometry vertex order.
        /// </remarks>
        bool FullGeometryShape { get; set; }
    }
}
