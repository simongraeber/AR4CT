using System.Collections.Generic;
using TriLibCore.Extensions;
using TriLibCore.General;
using UnityEngine;

namespace TriLibCore.Mappers
{
    /// <summary>
    /// Provides functionality to convert a loaded model’s bones into a Unity humanoid avatar hierarchy. 
    /// This class enforces humanoid-like orientations (e.g., T-pose) and aligns bone transforms 
    /// to standardized directional vectors. Subclasses can extend this mapper to refine or modify 
    /// default humanoid bone mapping processes.
    /// </summary>
    public class HumanoidAvatarMapper : ScriptableObject
    {
        /// <summary>
        /// Specifies reference "up" directions for known humanoid bones. These vectors are used 
        /// as orientation targets when ensuring a T-pose or standard humanoid posture.
        /// </summary>
        private static readonly IDictionary<string, Vector3> BoneReferenceUp = new Dictionary<string, Vector3>() {
            {"Hips", new Vector3(0.0f,1.0f,0.2f)},
            {"Spine", new Vector3(0.0f,1.0f,0.1f)},
            {"Chest", new Vector3(0.0f,1.0f,-0.1f)},
            {"Neck", new Vector3(0.0f,1.0f,0.1f)},
            {"LeftShoulder", new Vector3(-1.0f,-0.1f,-0.1f)},
            {"LeftUpperArm", new Vector3(-1.0f,0.0f,0.0f)},
            {"LeftLowerArm", new Vector3(-1.0f,0.0f,0.0f)},
            {"RightShoulder", new Vector3(1.0f,-0.1f,-0.1f)},
            {"RightUpperArm", new Vector3(1.0f,0.0f,0.0f)},
            {"RightLowerArm", new Vector3(1.0f,0.0f,0.0f)},
            {"LeftUpperLeg", new Vector3(0.0f,-1.0f,0.0f)},
            {"LeftLowerLeg", new Vector3(0.0f,-1.0f,0.0f)},
            {"LeftFoot", new Vector3(0.0f,-0.4f,0.9f)},
            {"RightUpperLeg", new Vector3(0.0f,-1.0f,0.0f)},
            {"RightLowerLeg", new Vector3(0.0f,-1.0f,0.0f)},
            {"RightFoot", new Vector3(0.0f,-0.4f,0.9f)},
            {"LeftHand", new Vector3(-1.0f,0.0f,0.2f)},
            {"Left Thumb Proximal", new Vector3(-0.6f,-0.4f,0.6f)},
            {"Left Thumb Intermediate", new Vector3(-0.7f,-0.4f,0.6f)},
            {"Left Index Proximal", new Vector3(-1.0f,0.0f,0.2f)},
            {"Left Index Intermediate", new Vector3(-1.0f,-0.1f,0.1f)},
            {"Left Middle Proximal", new Vector3(-1.0f,0.0f,0.1f)},
            {"Left Middle Intermediate", new Vector3(-1.0f,-0.1f,0.0f)},
            {"Left Ring Proximal", new Vector3(-1.0f,0.0f,0.0f)},
            {"Left Ring Intermediate", new Vector3(-1.0f,-0.1f,0.0f)},
            {"Left Little Proximal", new Vector3(-1.0f,0.0f,0.0f)},
            {"Left Little Intermediate", new Vector3(-1.0f,0.0f,0.0f)},
            {"RightHand", new Vector3(1.0f,0.0f,0.2f)},
            {"Right Thumb Proximal", new Vector3(0.6f,-0.4f,0.6f)},
            {"Right Thumb Intermediate", new Vector3(0.7f,-0.4f,0.6f)},
            {"Right Index Proximal", new Vector3(1.0f,0.0f,0.2f)},
            {"Right Index Intermediate", new Vector3(1.0f,-0.1f,0.1f)},
            {"Right Middle Proximal", new Vector3(1.0f,0.0f,0.1f)},
            {"Right Middle Intermediate", new Vector3(1.0f,-0.1f,0.0f)},
            {"Right Ring Proximal", new Vector3(1.0f,0.0f,0.0f)},
            {"Right Ring Intermediate", new Vector3(1.0f,-0.1f,0.0f)},
            {"Right Little Proximal", new Vector3(1.0f,0.0f,0.0f)},
            {"Right Little Intermediate", new Vector3(1.0f,0.0f,0.0f)},
        };

        /// <summary>
        /// Specifies reference “right” directions for known humanoid bones. 
        /// Together with <see cref="BoneReferenceUp"/>, these vectors help 
        /// achieve a standardized bone orientation.
        /// </summary>
        private static readonly IDictionary<string, Vector3> BoneReferenceRight = new Dictionary<string, Vector3>() {
            {"Hips", new Vector3(1.0f,0.0f,0.0f)},
            {"Spine", new Vector3(1.0f,0.0f,0.0f)},
            {"Chest", new Vector3(1.0f,0.0f,0.0f)},
            {"Neck", new Vector3(1.0f,0.0f,0.0f)},
            {"LeftShoulder", new Vector3(-0.1f,1.0f,0.2f)},
            {"LeftUpperArm", new Vector3(0.0f,1.0f,0.2f)},
            {"LeftLowerArm", new Vector3(0.0f,1.0f,0.2f)},
            {"RightShoulder", new Vector3(-0.1f,-1.0f,-0.2f)},
            {"RightUpperArm", new Vector3(0.0f,-1.0f,-0.2f)},
            {"RightLowerArm", new Vector3(0.0f,-1.0f,-0.2f)},
            {"LeftUpperLeg", new Vector3(-1.0f,0.0f,0.0f)},
            {"LeftLowerLeg", new Vector3(-1.0f,0.0f,0.0f)},
            {"LeftFoot", new Vector3(-0.3f,0.0f,0.0f)},
            {"RightUpperLeg", new Vector3(-1.0f,0.0f,0.0f)},
            {"RightLowerLeg", new Vector3(-1.0f,0.0f,0.0f)},
            {"RightFoot", new Vector3(-0.3f,0.0f,0.0f)},
            {"LeftHand", new Vector3(0.1f,1.0f,0.2f)},
            {"Left Thumb Proximal", new Vector3(-0.3f,0.6f,0.1f)},
            {"Left Thumb Intermediate", new Vector3(-0.3f,0.7f,0.1f)},
            {"Left Index Proximal", new Vector3(0.0f,1.0f,0.2f)},
            {"Left Index Intermediate", new Vector3(0.0f,1.0f,0.2f)},
            {"Left Middle Proximal", new Vector3(0.0f,1.0f,0.2f)},
            {"Left Middle Intermediate", new Vector3(-0.1f,1.0f,0.2f)},
            {"Left Ring Proximal", new Vector3(0.0f,1.0f,0.2f)},
            {"Left Ring Intermediate", new Vector3(-0.1f,1.0f,0.2f)},
            {"Left Little Proximal", new Vector3(0.0f,1.0f,0.2f)},
            {"Left Little Intermediate", new Vector3(0.0f,1.0f,0.2f)},
            {"RightHand", new Vector3(0.1f,-1.0f,-0.2f)},
            {"Right Thumb Proximal", new Vector3(-0.3f,-0.6f,-0.1f)},
            {"Right Thumb Intermediate", new Vector3(-0.3f,-0.7f,-0.1f)},
            {"Right Index Proximal", new Vector3(0.0f,-1.0f,-0.2f)},
            {"Right Index Intermediate", new Vector3(0.0f,-1.0f,-0.2f)},
            {"Right Middle Proximal", new Vector3(0.0f,-1.0f,-0.2f)},
            {"Right Middle Intermediate", new Vector3(-0.1f,-1.0f,-0.2f)},
            {"Right Ring Proximal", new Vector3(0.0f,-1.0f,-0.2f)},
            {"Right Ring Intermediate", new Vector3(-0.1f,-1.0f,-0.2f)},
            {"Right Little Proximal", new Vector3(0.0f,-1.0f,-0.2f)},
            {"Right Little Intermediate", new Vector3(0.0f,-1.0f,-0.2f)},
        };

        private const float MaxBoneDirectionError = 0.999f;

        /// <summary>
        /// Ensures the model’s limbs and spine are oriented in a T-pose, if requested. This 
        /// involves rotating specific bone pairs (spine, arms, legs, etc.) and adjusting the 
        /// hips, allowing the rest of the rig to maintain consistent humanoid alignment.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> with references to the loaded <see cref="GameObject"/>, 
        /// user-defined <see cref="AssetLoaderOptions"/>, and other contextual data.
        /// </param>
        /// <param name="mapping">
        /// A dictionary mapping <see cref="BoneMapping"/> keys (representing <see cref="HumanBodyBones"/> 
        /// entries) to actual <see cref="Transform"/> references in the loaded model hierarchy.
        /// </param>
        private void EnforceTPose(
            AssetLoaderContext assetLoaderContext,
            Dictionary<BoneMapping, Transform> mapping)
        {
            Quaternion? headRotation = null;
            var headTransform = FindMappedBone("Head", mapping);
            if (headTransform != null)
            {
                headRotation = headTransform.rotation;
            }

            // Use the hips/spine/upperArm references to define a forward direction
            var spineReferenceForward = GetReferenceForward("Hips", "Spine", "LeftUpperArm", "RightUpperArm", mapping);

            // Align spine/neck/head
            EnforceBoneTPose("Spine", "Chest", mapping, spineReferenceForward);
            EnforceBoneTPose("Chest", "Neck", mapping, spineReferenceForward);
            EnforceBoneTPose("Neck", "Head", mapping, spineReferenceForward);

            // Restore stored head rotation
            if (headTransform != null)
            {
                headTransform.rotation = headRotation.Value;
            }

            // Align left upper/lower leg
            var leftFootTransform = FindMappedBone("LeftFoot", mapping);
            var leftFootRotation = leftFootTransform.rotation;
            EnforceBoneTPose("LeftUpperLeg", "LeftLowerLeg", mapping, spineReferenceForward);
            EnforceBoneTPose("LeftLowerLeg", "LeftFoot", mapping, spineReferenceForward);
            leftFootTransform.rotation = leftFootRotation;

            // Align right upper/lower leg
            var rightFootTransform = FindMappedBone("RightFoot", mapping);
            var rightFootRotation = rightFootTransform.rotation;
            EnforceBoneTPose("RightUpperLeg", "RightLowerLeg", mapping, spineReferenceForward);
            EnforceBoneTPose("RightLowerLeg", "RightFoot", mapping, spineReferenceForward);
            rightFootTransform.rotation = rightFootRotation;

            // Align arms
            EnforceBoneTPose("LeftUpperArm", "LeftLowerArm", mapping, null);
            EnforceBoneTPose("LeftLowerArm", "LeftHand", mapping, null);
            EnforceBoneTPose("RightUpperArm", "RightLowerArm", mapping, null);
            EnforceBoneTPose("RightLowerArm", "RightHand", mapping, null);

            // Align left hand/fingers
            EnforceBoneTPose("LeftHand", "Left Middle Proximal", mapping, null);
            EnforceBoneTPose("Left Thumb Proximal", "Left Thumb Intermediate", mapping, null);
            EnforceBoneTPose("Left Thumb Intermediate", "Left Thumb Distal", mapping, null);
            EnforceBoneTPose("Left Index Proximal", "Left Index Intermediate", mapping, null);
            EnforceBoneTPose("Left Index Intermediate", "Left Index Distal", mapping, null);
            EnforceBoneTPose("Left Middle Proximal", "Left Middle Intermediate", mapping, null);
            EnforceBoneTPose("Left Middle Intermediate", "Left Middle Distal", mapping, null);
            EnforceBoneTPose("Left Ring Proximal", "Left Ring Intermediate", mapping, null);
            EnforceBoneTPose("Left Ring Intermediate", "Left Ring Distal", mapping, null);
            EnforceBoneTPose("Left Little Proximal", "Left Little Intermediate", mapping, null);
            EnforceBoneTPose("Left Little Intermediate", "Left Little Distal", mapping, null);

            // Align right hand/fingers
            EnforceBoneTPose("RightHand", "Right Middle Proximal", mapping, null);
            EnforceBoneTPose("Right Thumb Proximal", "Right Thumb Intermediate", mapping, null);
            EnforceBoneTPose("Right Thumb Intermediate", "Right Thumb Distal", mapping, null);
            EnforceBoneTPose("Right Index Proximal", "Right Index Intermediate", mapping, null);
            EnforceBoneTPose("Right Index Intermediate", "Right Index Distal", mapping, null);
            EnforceBoneTPose("Right Middle Proximal", "Right Middle Intermediate", mapping, null);
            EnforceBoneTPose("Right Middle Intermediate", "Right Middle Distal", mapping, null);
            EnforceBoneTPose("Right Ring Proximal", "Right Ring Intermediate", mapping, null);
            EnforceBoneTPose("Right Ring Intermediate", "Right Ring Distal", mapping, null);
            EnforceBoneTPose("Right Little Proximal", "Right Little Intermediate", mapping, null);
            EnforceBoneTPose("Right Little Intermediate", "Right Little Distal", mapping, null);

            // Slightly rotate hips to face forward
            AdjustHips(assetLoaderContext, spineReferenceForward, Vector3.forward, mapping);
        }

        /// <summary>
        /// Performs a small rotation on the “Hips” bone to align the character’s forward axis
        /// with the given <paramref name="final"/> direction, ensuring the model faces forward in T-pose.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> holding references to loaded <see cref="GameObject"/>s 
        /// and user options.
        /// </param>
        /// <param name="reference">
        /// The computed forward direction based on current bone alignment.
        /// </param>
        /// <param name="final">
        /// The desired final forward axis for the hips (usually <c>Vector3.forward</c>).
        /// </param>
        /// <param name="mapping">
        /// Bone mapping dictionary used to locate the “Hips” transform.
        /// </param>
        private static void AdjustHips(
            AssetLoaderContext assetLoaderContext,
            Vector3 reference,
            Vector3 final,
            Dictionary<BoneMapping, Transform> mapping)
        {
            var hips = FindMappedBone("Hips", mapping);
            reference.y = 0f;
            reference.Normalize();
            var angle = Vector3.Angle(reference, final);
            hips.transform.rotation = Quaternion.AngleAxis(angle, Vector3.up) * hips.transform.rotation;
        }

        /// <summary>
        /// Determines a reference forward vector by examining two pairs of bones—one for the up 
        /// (e.g., “Spine”) and one for the right (e.g., “LeftUpperArm” vs. “RightUpperArm”). 
        /// This vector is used to help align the torso in a humanoid T-pose.
        /// </summary>
        /// <param name="upA">The name of the parent bone for the up vector reference (e.g., “Hips”).</param>
        /// <param name="upB">The child bone for the up vector reference (e.g., “Spine”).</param>
        /// <param name="rightA">A bone on the left side (e.g., “LeftUpperArm”).</param>
        /// <param name="rightB">A bone on the right side (e.g., “RightUpperArm”).</param>
        /// <param name="mapping">
        /// The dictionary mapping TriLib’s <see cref="BoneMapping"/> to <see cref="Transform"/> references.
        /// </param>
        /// <returns>A normalized <c>Vector3</c> representing a forward axis for the model’s torso.</returns>
        private static Vector3 GetReferenceForward(
            string upA,
            string upB,
            string rightA,
            string rightB,
            Dictionary<BoneMapping, Transform> mapping)
        {
            var upTransformA = FindMappedBone(upA, mapping);
            var upTransformB = FindMappedBone(upB, mapping);
            var rightTransformA = FindMappedBone(rightA, mapping);
            var rightTransformB = FindMappedBone(rightB, mapping);

            var referenceUp = GetChildBoneDirection(upTransformA, upTransformB);
            var referenceRight = GetChildBoneDirection(rightTransformA, rightTransformB);
            var referenceForward = Vector3.Cross(referenceRight, referenceUp);
            return referenceForward;
        }

        /// <summary>
        /// Ensures a single bone axis is aligned with expected reference vectors, optionally 
        /// using a <paramref name="referenceForward"/> for cross product alignment 
        /// (e.g., for spine or leg bones).
        /// </summary>
        /// <param name="parentBoneName">The parent bone, e.g. “Spine”, “LeftUpperArm”.</param>
        /// <param name="boneName">The child bone whose direction we want to standardize, e.g. “Chest”.</param>
        /// <param name="mapping">The dictionary mapping TriLib bones to scene <see cref="Transform"/> references.</param>
        /// <param name="referenceForward">
        /// Optional forward direction for additional cross alignment. If <c>null</c>, only 
        /// the main up-axis alignment is performed.
        /// </param>
        private static void EnforceBoneTPose(
            string parentBoneName,
            string boneName,
            Dictionary<BoneMapping, Transform> mapping,
            Vector3? referenceForward)
        {
            var parentTransform = FindMappedBone(parentBoneName, mapping);
            var boneTransform = FindMappedBone(boneName, mapping);
            if (parentTransform == null || boneTransform == null)
            {
                return;
            }

            var boneReferenceUp = BoneReferenceUp[parentBoneName];
            var boneReferenceRight = BoneReferenceRight[parentBoneName];
            var boneUp = GetChildBoneDirection(parentTransform, boneTransform);
            var dot = Vector3.Dot(boneUp, boneReferenceUp);

            // Rotate to match the reference up direction
            if (dot < MaxBoneDirectionError)
            {
                var rotation = Quaternion.FromToRotation(boneUp, boneReferenceUp);
                parentTransform.rotation = rotation * parentTransform.rotation;
            }

            // If available, further align bone’s “right” axis
            if (referenceForward.HasValue)
            {
                boneUp = GetChildBoneDirection(parentTransform, boneTransform);
                var boneRight = Vector3.Cross(boneUp, referenceForward.Value);
                dot = Vector3.Dot(boneRight, boneReferenceRight);
                if (dot < MaxBoneDirectionError)
                {
                    var rotation = Quaternion.FromToRotation(boneRight, boneReferenceRight);
                    parentTransform.rotation = rotation * parentTransform.rotation;
                }
            }
        }

        /// <summary>
        /// Locates a mapped bone within the dictionary of <see cref="BoneMapping"/> to <see cref="Transform"/>.
        /// Uses Unity’s <see cref="HumanTrait.BoneName"/> array to match TriLib’s bones with the requested <paramref name="boneName"/>.
        /// </summary>
        /// <param name="boneName">The humanoid bone name to search for (e.g., “Spine”, “Head”).</param>
        /// <param name="mapping">
        /// The dictionary mapping TriLib’s <see cref="BoneMapping"/> keys to actual <see cref="Transform"/> references in the scene.
        /// </param>
        /// <returns>The <see cref="Transform"/> matching the specified <paramref name="boneName"/>, or <c>null</c> if not found.</returns>
        private static Transform FindMappedBone(
            string boneName,
            Dictionary<BoneMapping, Transform> mapping)
        {
            foreach (var map in mapping)
            {
                if (HumanTrait.BoneName[(int)map.Key.HumanBone] == boneName)
                {
                    return map.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Computes a normalized direction vector from <paramref name="parentTransform"/> 
        /// to <paramref name="transform"/>, representing the “bone up” axis 
        /// in the current hierarchical structure.
        /// </summary>
        /// <param name="parentTransform">The parent bone’s <see cref="Transform"/>.</param>
        /// <param name="transform">The child bone’s <see cref="Transform"/>.</param>
        /// <returns>A <c>Vector3</c> normalized direction from parent to child.</returns>
        private static Vector3 GetChildBoneDirection(Transform parentTransform, Transform transform)
        {
            return (transform.position - parentTransform.position).normalized;
        }

        /// <summary>
        /// Called once the basic bone mapping is established. If <see cref="AssetLoaderOptions.SampleBindPose"/> 
        /// is enabled, the model is sampled at bind pose. If <see cref="AssetLoaderOptions.EnforceTPose"/> 
        /// is enabled, the mapped bones are rotated to align the character in a T-pose configuration 
        /// (spine/arms/legs arranged horizontally).
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> referencing user-defined options and loaded GameObjects.
        /// </param>
        /// <param name="mapping">
        /// The finalized dictionary mapping TriLib <see cref="BoneMapping"/> entries to actual <see cref="Transform"/> references.
        /// </param>
        public void PostSetup(
            AssetLoaderContext assetLoaderContext,
            Dictionary<BoneMapping, Transform> mapping)
        {
            if (assetLoaderContext.RootGameObject != null)
            {
                // Sample the bind pose if configured
                if (assetLoaderContext.Options.SampleBindPose)
                {
                    GameObjectExtensions.SampleBindPose(assetLoaderContext.RootGameObject);
                }

                // Apply T-pose if configured
                if (assetLoaderContext.Options.EnforceTPose)
                {
                    EnforceTPose(assetLoaderContext, mapping);
                }
            }
        }

        /// <summary>
        /// Attempts to map the loaded model bones into a humanoid rig, returning 
        /// a <see cref="Dictionary{BoneMapping, Transform}"/> for further processing. 
        /// This method can be overridden to implement custom bone search or heuristics.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing references to the loaded GameObject 
        /// hierarchy and relevant import options.
        /// </param>
        /// <returns>
        /// A dictionary mapping each humanoid <see cref="BoneMapping"/> entry to the 
        /// corresponding <see cref="Transform"/> in the scene. By default, returns <c>null</c>.
        /// </returns>
        public virtual Dictionary<BoneMapping, Transform> Map(AssetLoaderContext assetLoaderContext)
        {
            return null;
        }
    }
}
