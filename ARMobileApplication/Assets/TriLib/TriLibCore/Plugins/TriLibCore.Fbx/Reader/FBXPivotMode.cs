using System;

namespace TriLibCore.Fbx.Reader
{
    public enum FBXPivotMode
    {
        /// <summary>
        /// Legacy mode: TriLib won't change objects pivot position
        /// </summary>
        Legacy = 0,
        /// <summary>
        /// TriLib will preserve the rotation pivots.
        /// </summary>
        [Obsolete("Now TriLib only preserves the original pivot if both rotation and scale pivots are the same")]
        PreserveRotationPivot = 1,
        /// <summary>
        /// TriLib will preserve the scaling pivots.
        /// </summary>
        [Obsolete("Now TriLib only preserves the original pivot if both rotation and scale pivots are the same")]
        PreserveScalingPivot = 2,
        /// <summary>
        /// TriLib will preserve the pivot.
        /// </summary>
        PreservePivot = 1
    }
}