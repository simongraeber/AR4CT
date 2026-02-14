using System;
using UnityEngine;

namespace TriLibCore.General
{
    /// <summary>
    /// Represents a human bone to Unity bone relationship.
    /// </summary>
    [Serializable]
    public struct BoneMapping
    {
        /// <summary>
        /// Human bone name.
        /// </summary>
        public HumanBodyBones HumanBone;

        /// <summary>
        /// Human limit data.
        /// </summary>
        public HumanLimit HumanLimit;

        /// <summary>
        /// Bone Transform names.
        /// </summary>
        public string[] BoneNames;

        /// <summary>Represents a human bone to Unity bone relationship.</summary>
        /// <param name="humanBone">The Human bone to map.</param>
        /// <param name="humanLimit">The bone Human Limit.</param>
        /// <param name="boneNames">The bone Transform names.</param>
        public BoneMapping(HumanBodyBones humanBone, HumanLimit humanLimit, string[] boneNames)
        {
            HumanBone = humanBone;
            HumanLimit = humanLimit;
            BoneNames = boneNames;
        }
    }
}