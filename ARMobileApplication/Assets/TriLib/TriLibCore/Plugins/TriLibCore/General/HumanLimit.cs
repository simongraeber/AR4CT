using System;
using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>Represents the rotation limits that define the muscle for a single human bone.</summary>
    [Serializable]
    public class HumanLimit
    {
        /// <summary>
        /// Should this limit use the default values?
        /// </summary>
        public bool useDefaultValues = true;
        /// <summary>
        /// The maximum negative rotation away from the initial value that this muscle can apply
        /// </summary>
        public Vector3 min;
        /// <summary>
        /// The maximum rotation away from the initial value that this muscle can apply
        /// </summary>
        public Vector3 max;
        /// <summary>
        /// The default orientation of a bone when no muscle action is applied
        /// </summary>
        public Vector3 center;
        /// <summary>
        /// Length of the bone to which the limit is applied
        /// </summary>
        public float axisLength;
    }
}