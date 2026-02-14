using System;

namespace TriLibCore.General
{
    /// <summary>Represents humanoid Avatar parameters to pass to the AvatarBuilder.BuildHumanAvatar method.</summary>
    [Serializable]
    public class HumanDescription
    {
        /// <summary>
        /// Amount by which the arm's length is allowed to stretch when using IK.
        /// </summary>
        public float armStretch = 0.05f;

        /// <summary>
        /// Modification to the minimum distance between the feet of a humanoid model.
        /// </summary>
        public float feetSpacing;

        /// <summary>
        /// True for any human that has a translation Degree of Freedom (DoF). It is set to false by default.
        /// </summary>
        public bool hasTranslationDof;

        /// <summary>
        /// Amount by which the leg's length is allowed to stretch when using IK.
        /// </summary>
        public float legStretch = 0.05f;

        /// <summary>
        /// Defines how the lower arm's roll/twisting is distributed between the elbow and wrist joints.
        /// </summary>
        public float lowerArmTwist = 0.5f;

        /// <summary>
        /// Defines how the lower leg's roll/twisting is distributed between the knee and ankle.
        /// </summary>
        public float lowerLegTwist = 0.5f;

        /// <summary>
        /// Defines how the lower arm's roll/twisting is distributed between the shoulder and elbow joints.
        /// </summary>
        public float upperArmTwist = 0.5f;

        /// <summary>
        /// Defines how the upper leg's roll/twisting is distributed between the thigh and knee joints.
        /// </summary>
        public float upperLegTwist = 0.5f;
    }
}