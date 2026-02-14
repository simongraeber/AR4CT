using UnityEngine;
using UnityEngine.Rendering;

namespace TriLibCore.Utils
{
    /// <summary>
    /// Provides utility methods for querying the active Unity render pipeline.
    /// </summary>
    public static class RenderPipelineUtils
    {
        /// <summary>
        /// Identifies the kind of render pipeline currently in use.
        /// </summary>
        public enum RenderPipelineKind
        {
            /// <summary>
            /// Unity built-in (legacy) render pipeline.
            /// </summary>
            BuiltIn,

            /// <summary>
            /// Universal Render Pipeline (URP).
            /// </summary>
            URP,

            /// <summary>
            /// High Definition Render Pipeline (HDRP).
            /// </summary>
            HDRP,

            /// <summary>
            /// A custom Scriptable Render Pipeline (SRP).
            /// </summary>
            CustomSRP
        }

        /// <summary>
        /// Gets the currently active <see cref="RenderPipelineAsset"/>.
        /// </summary>
        /// <remarks>
        /// This method checks <see cref="GraphicsSettings.currentRenderPipeline"/> first,
        /// falling back to <see cref="QualitySettings.renderPipeline"/> when necessary.
        /// </remarks>
        /// <returns>
        /// The active render pipeline asset, or <c>null</c> when using the built-in pipeline.
        /// </returns>
        public static RenderPipelineAsset GetActiveRenderPipelineAsset()
        {
            return GraphicsSettings.currentRenderPipeline ?? QualitySettings.renderPipeline;
        }

        /// <summary>
        /// Determines the kind of render pipeline currently active.
        /// </summary>
        /// <returns>
        /// A <see cref="RenderPipelineKind"/> value representing the active pipeline.
        /// </returns>
        public static RenderPipelineKind GetActivePipelineKind()
        {
            var rp = GetActiveRenderPipelineAsset();
            if (rp == null)
                return RenderPipelineKind.BuiltIn;

            var fullName = rp.GetType().FullName;

            if (fullName == "UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset")
                return RenderPipelineKind.URP;

            if (fullName == "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")
                return RenderPipelineKind.HDRP;

            return RenderPipelineKind.CustomSRP;
        }

        /// <summary>
        /// Gets a value indicating whether the built-in (legacy) render pipeline is active.
        /// </summary>
        public static bool IsUsingStandardPipeline =>
            GetActivePipelineKind() == RenderPipelineKind.BuiltIn;

        /// <summary>
        /// Gets a value indicating whether the Universal Render Pipeline (URP) is active.
        /// </summary>
        public static bool IsUsingUniversalPipeline =>
            GetActivePipelineKind() == RenderPipelineKind.URP;

        /// <summary>
        /// Gets a value indicating whether the High Definition Render Pipeline (HDRP) is active.
        /// </summary>
        public static bool IsUsingHDRPPipeline =>
            GetActivePipelineKind() == RenderPipelineKind.HDRP;

        /// <summary>
        /// Gets a value indicating whether any Scriptable Render Pipeline (SRP) is active.
        /// </summary>
        public static bool IsUsingAnySRP =>
            GetActiveRenderPipelineAsset() != null;
    }
}
