using TriLibCore.General;
using UnityEngine;

namespace TriLibCore.Extensions
{
    /// <summary>
    /// Provides extension methods to facilitate working with <see cref="AnimationCurve"/> objects.
    /// </summary>
    public static class AnimationCurveExtensions
    {
        /// <summary>
        /// Automatically adjusts each key's tangents in the provided <see cref="AnimationCurve"/>.
        /// <para>
        /// If the value of a keyframe is outside the range defined by its neighboring keyframes,
        /// the in/out tangents for that key will be set to 0 (flat). Otherwise, an automatically
        /// computed tangent is assigned.
        /// </para>
        /// </summary>
        /// <param name="animationCurve">
        /// The <see cref="AnimationCurve"/> whose key tangents will be adjusted.
        /// </param>
        public static void AutoClampedTangents(this AnimationCurve animationCurve)
        {
            // Helper function to check if a key's value is outside the range of its neighbors
            // and clamp its tangents if so.
            bool ClampValue(int currKeyIndex, int keyCount, Keyframe keyframe, Keyframe nextKey1, Keyframe prevKey1, ref float inTangent, ref float outTangent)
            {
                var clamped = false;
                if (currKeyIndex == 0 || currKeyIndex == keyCount - 1)
                {
                    // First or last keyframe: flat tangents
                    clamped = true;
                    inTangent = 0f;
                    outTangent = 0f;
                }
                else
                {
                    float minValue;
                    float maxValue;
                    if (prevKey1.value < nextKey1.value)
                    {
                        minValue = prevKey1.value;
                        maxValue = nextKey1.value;
                    }
                    else
                    {
                        minValue = nextKey1.value;
                        maxValue = prevKey1.value;
                    }
                    if (keyframe.value >= maxValue || keyframe.value <= minValue)
                    {
                        // Outside the range: flat tangents
                        clamped = true;
                        inTangent = 0f;
                        outTangent = 0f;
                    }
                }
                return clamped;
            }

            // If there's fewer than 2 keyframes, do nothing
            if (animationCurve.length < 2)
            {
                return;
            }

            // For each keyframe, decide whether to clamp or to set auto tangents
            for (var i = 0; i < animationCurve.length; i++)
            {
                var prevKey = animationCurve[Mathf.Clamp(i - 1, 0, animationCurve.length - 1)];
                var thisKey = animationCurve[i];
                var nextKey = animationCurve[Mathf.Clamp(i + 1, 0, animationCurve.length - 1)];
                var inTangent = 0f;
                var outTangent = 0f;

                var clamped = ClampValue(i, animationCurve.length, thisKey, nextKey, prevKey, ref inTangent, ref outTangent);
                if (!clamped)
                {
                    // Use auto tangent if not clamped
                    inTangent = GetTangentAuto(nextKey, prevKey, thisKey, i, animationCurve.length);
                    outTangent = inTangent;
                }

                // Update keyframe tangents
                thisKey.weightedMode = WeightedMode.None;
                thisKey.outTangent = outTangent;
                thisKey.inTangent = inTangent;
                animationCurve.MoveKey(i, thisKey);
            }
        }

        /// <summary>
        /// Calculates the linear in-tangent of the current <paramref name="thisKey"/> based on a previous <paramref name="prevKey"/>.
        /// </summary>
        /// <param name="thisKey">The current keyframe.</param>
        /// <param name="prevKey">The previous keyframe.</param>
        /// <returns>The linear in-tangent value.</returns>
        public static float GetInTangentLinear(Keyframe thisKey, Keyframe prevKey)
        {
            var prevTimeDiff = (thisKey.time - prevKey.time);
            var inTangent = (thisKey.value - prevKey.value);
            inTangent /= prevTimeDiff;
            if (float.IsNaN(inTangent) || float.IsInfinity(inTangent))
            {
                inTangent = 0f;
            }
            return inTangent;
        }

        /// <summary>
        /// Calculates the linear out-tangent of the current <paramref name="thisKey"/> based on the next <paramref name="nextKey"/>.
        /// </summary>
        /// <param name="nextKey">The next keyframe.</param>
        /// <param name="thisKey">The current keyframe.</param>
        /// <returns>The linear out-tangent value.</returns>
        public static float GetOutTangentLinear(Keyframe nextKey, Keyframe thisKey)
        {
            var nextTimeDiff = (nextKey.time - thisKey.time);
            var outTangent = (nextKey.value - thisKey.value);
            outTangent /= nextTimeDiff;
            if (float.IsNaN(outTangent) || float.IsInfinity(outTangent))
            {
                outTangent = 0f;
            }
            return outTangent;
        }

        /// <summary>
        /// Calculates an "auto" tangent for the current <paramref name="thisKey"/>, given the previous and next keyframes.
        /// <para>
        /// If <paramref name="index"/> is 0 or the last key in <paramref name="count"/>, then the tangent is returned
        /// as a linear out-tangent. Otherwise, an average slope is calculated.
        /// </para>
        /// </summary>
        /// <param name="nextKey">The next keyframe.</param>
        /// <param name="prevKey">The previous keyframe.</param>
        /// <param name="thisKey">The current keyframe.</param>
        /// <param name="index">The index of the current keyframe in the curve.</param>
        /// <param name="count">The total number of keyframes in the curve.</param>
        /// <returns>An automatically computed tangent value.</returns>
        public static float GetTangentAuto(Keyframe nextKey, Keyframe prevKey, Keyframe thisKey, int index, int count)
        {
            if (index <= 0 || index >= count - 1)
            {
                // First or last key: return a linear out-tangent
                return GetOutTangentLinear(nextKey, thisKey);
            }

            var prevToNextTimeDiff = nextKey.time - prevKey.time;
            var newTangent = (thisKey.value - prevKey.value) + (nextKey.value - thisKey.value);
            newTangent /= prevToNextTimeDiff;
            return newTangent;
        }

        /// <summary>
        /// Sets the tangents of the given <see cref="AnimationCurve"/> to the specified <paramref name="mode"/>.
        /// <para>
        /// The first and last key tangents are set to 0 to ensure a smooth start/end. Intermediate keys
        /// are set based on the chosen <see cref="TangentMode"/>: Linear or Stepped.
        /// </para>
        /// </summary>
        /// <param name="animationCurve">The <see cref="AnimationCurve"/> to modify.</param>
        /// <param name="mode">The desired <see cref="TangentMode"/>.</param>
        public static void SetTangents(this AnimationCurve animationCurve, TangentMode mode)
        {
            if (animationCurve.length < 2)
            {
                return;
            }

            // First key
            var beginKey = animationCurve[0];
            beginKey.inTangent = 0f;
            beginKey.outTangent = 0f;
            animationCurve.MoveKey(0, beginKey);

            // Last key
            var endKey = animationCurve[animationCurve.length - 1];
            endKey.inTangent = 0f;
            endKey.outTangent = 0f;
            animationCurve.MoveKey(animationCurve.length - 1, endKey);

            // Intermediate keys
            for (var i = 1; i < animationCurve.length - 1; i++)
            {
                var thisKey = animationCurve[i];
                switch (mode)
                {
                    case TangentMode.Linear:
                        {
                            var prevKey = animationCurve[i - 1];
                            var nextKey = animationCurve[i + 1];
                            var inTangent = GetInTangentLinear(thisKey, prevKey);
                            var outTangent = GetOutTangentLinear(nextKey, thisKey);
                            thisKey.inTangent = inTangent;
                            thisKey.outTangent = outTangent;
                            break;
                        }
                    case TangentMode.Stepped:
                        {
                            // Stepped tangents are effectively infinite slope
                            thisKey.inTangent = Mathf.Infinity;
                            thisKey.outTangent = Mathf.Infinity;
                            break;
                        }
                }

                animationCurve.MoveKey(i, thisKey);
            }
        }

        /// <summary>
        /// Adds a new keyframe representing a <see cref="Quaternion"/> to four separate <see cref="AnimationCurve"/> objects
        /// (for <c>X</c>, <c>Y</c>, <c>Z</c>, and <c>W</c>), potentially simplifying by merging keyframes if the error falls
        /// below a certain threshold.
        /// </summary>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing the <see cref="AssetLoaderOptions"/> which define thresholds
        /// and settings for simplification.
        /// </param>
        /// <param name="animationCurveX">The <see cref="AnimationCurve"/> for the <c>X</c> component.</param>
        /// <param name="animationCurveY">The <see cref="AnimationCurve"/> for the <c>Y</c> component.</param>
        /// <param name="animationCurveZ">The <see cref="AnimationCurve"/> for the <c>Z</c> component.</param>
        /// <param name="animationCurveW">The <see cref="AnimationCurve"/> for the <c>W</c> component.</param>
        /// <param name="keyframeX">The keyframe for the <c>X</c> component.</param>
        /// <param name="keyframeY">The keyframe for the <c>Y</c> component.</param>
        /// <param name="keyframeZ">The keyframe for the <c>Z</c> component.</param>
        /// <param name="keyframeW">The keyframe for the <c>W</c> component.</param>
        public static void AddQuaternionKeyframe(
            AssetLoaderContext assetLoaderContext,
            AnimationCurve animationCurveX,
            AnimationCurve animationCurveY,
            AnimationCurve animationCurveZ,
            AnimationCurve animationCurveW,
            Keyframe keyframeX,
            Keyframe keyframeY,
            Keyframe keyframeZ,
            Keyframe keyframeW
        )
        {
            // If not simplifying or if there are fewer than 2 keyframes, simply add the new keyframes
            if (!assetLoaderContext.Options.SimplifyAnimations || animationCurveX.length < 2)
            {
                animationCurveX.AddKey(keyframeX);
                animationCurveY.AddKey(keyframeY);
                animationCurveZ.AddKey(keyframeZ);
                animationCurveW.AddKey(keyframeW);
                return;
            }

            // Retrieve last and second-last keyframes from each curve
            var lastKeyframeX = animationCurveX[animationCurveX.length - 1];
            var lastKeyframeY = animationCurveY[animationCurveY.length - 1];
            var lastKeyframeZ = animationCurveZ[animationCurveZ.length - 1];
            var lastKeyframeW = animationCurveW[animationCurveW.length - 1];

            var secondLastKeyframeX = animationCurveX[animationCurveX.length - 2];
            var secondLastKeyframeY = animationCurveY[animationCurveY.length - 2];
            var secondLastKeyframeZ = animationCurveZ[animationCurveZ.length - 2];
            var secondLastKeyframeW = animationCurveW[animationCurveW.length - 2];

            // Convert to Quaternion for checking angle error
            var lastKeyframe = new Quaternion(lastKeyframeX.value, lastKeyframeY.value, lastKeyframeZ.value, lastKeyframeW.value);
            var secondLastKeyframe = new Quaternion(secondLastKeyframeX.value, secondLastKeyframeY.value, secondLastKeyframeZ.value, secondLastKeyframeW.value);
            var newKeyframe = new Quaternion(keyframeX.value, keyframeY.value, keyframeZ.value, keyframeW.value);

            // Interpolate halfway between the second last and the new keyframe
            var interpolated = Quaternion.Slerp(secondLastKeyframe, newKeyframe, 0.5f);
            // Calculate angle difference with the last keyframe
            var angleError = Quaternion.Angle(interpolated, lastKeyframe);

            // Decide whether to add a new keyframe or merge with the last one
            if (angleError > assetLoaderContext.Options.RotationThreshold)
            {
                animationCurveX.AddKey(keyframeX);
                animationCurveY.AddKey(keyframeY);
                animationCurveZ.AddKey(keyframeZ);
                animationCurveW.AddKey(keyframeW);
            }
            else
            {
                animationCurveX.MoveKey(animationCurveX.length - 1, keyframeX);
                animationCurveY.MoveKey(animationCurveY.length - 1, keyframeY);
                animationCurveZ.MoveKey(animationCurveZ.length - 1, keyframeZ);
                animationCurveW.MoveKey(animationCurveW.length - 1, keyframeW);
            }
        }

        /// <summary>
        /// Adds a new <paramref name="newKeyframe"/> to the <paramref name="animationCurve"/> representing a single
        /// component of a vector (e.g., x, y, or z position or scale). Potentially merges it with the previous keyframe
        /// if the error is below the defined threshold.
        /// </summary>
        /// <param name="animationCurve">
        /// The <see cref="AnimationCurve"/> to which the keyframe will be added (or merged).
        /// </param>
        /// <param name="assetLoaderContext">
        /// The <see cref="AssetLoaderContext"/> containing thresholds and other settings.
        /// </param>
        /// <param name="newKeyframe">The new <see cref="Keyframe"/> to be added or used to merge.</param>
        /// <param name="isScale">
        /// Indicates whether this keyframe represents a scale component. Determines whether
        /// <see cref="AssetLoaderOptions.ScaleThreshold"/> or <see cref="AssetLoaderOptions.PositionThreshold"/> is used.
        /// </param>
        public static void AddVectorKeyframe(this AnimationCurve animationCurve, AssetLoaderContext assetLoaderContext, Keyframe newKeyframe, bool isScale)
        {
            if (!assetLoaderContext.Options.SimplifyAnimations || animationCurve.length < 2)
            {
                animationCurve.AddKey(newKeyframe);
                return;
            }

            var lastKeyframe = animationCurve[animationCurve.length - 1];
            var secondLastKeyframe = animationCurve[animationCurve.length - 2];

            // Compute the interpolated (midway) value
            var interpolated = Mathf.Lerp(secondLastKeyframe.value, newKeyframe.value, 0.5f);
            var error = Mathf.Abs(interpolated - lastKeyframe.value);

            // Use the correct threshold based on whether it's scale or position
            var threshold = isScale ? assetLoaderContext.Options.ScaleThreshold : assetLoaderContext.Options.PositionThreshold;

            // Decide to add a new key or merge with the last key based on the error threshold
            if (error > threshold)
            {
                animationCurve.AddKey(newKeyframe);
            }
            else
            {
                animationCurve.MoveKey(animationCurve.length - 1, newKeyframe);
            }
        }
    }
}
