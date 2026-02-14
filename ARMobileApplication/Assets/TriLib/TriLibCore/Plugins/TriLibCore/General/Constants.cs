namespace TriLibCore.General
{
    /// <summary>
    /// Defines a collection of constant values used internally by TriLib,
    /// primarily related to animation property paths and mesh processing.
    /// </summary>
    public static class Constants
    {
        #region Animation Properties

        /// <summary>
        /// Local position X property name used during animation processing.
        /// </summary>
        public const string LocalPositionXProperty = "localPosition.x";

        /// <summary>
        /// Local position Y property name used during animation processing.
        /// </summary>
        public const string LocalPositionYProperty = "localPosition.y";

        /// <summary>
        /// Local position Z property name used during animation processing.
        /// </summary>
        public const string LocalPositionZProperty = "localPosition.z";

        /// <summary>
        /// Local rotation X property name used during animation processing.
        /// </summary>
        public const string LocalRotationXProperty = "localRotation.x";

        /// <summary>
        /// Local rotation Y property name used during animation processing.
        /// </summary>
        public const string LocalRotationYProperty = "localRotation.y";

        /// <summary>
        /// Local rotation Z property name used during animation processing.
        /// </summary>
        public const string LocalRotationZProperty = "localRotation.z";

        /// <summary>
        /// Local rotation W property name used during animation processing.
        /// </summary>
        public const string LocalRotationWProperty = "localRotation.w";

        /// <summary>
        /// Component enabled state property name used during animation processing.
        /// </summary>
        public const string EnabledProperty = "m_Enabled";

        /// <summary>
        /// Local Euler angles X property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesXProperty = "localEulerAngles.x";

        /// <summary>
        /// Local Euler angles Y property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesYProperty = "localEulerAngles.y";

        /// <summary>
        /// Local Euler angles Z property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesZProperty = "localEulerAngles.z";

        /// <summary>
        /// Local baked Euler angles X property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesBakedXProperty = "localEulerAnglesBaked.x";

        /// <summary>
        /// Local baked Euler angles Y property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesBakedYProperty = "localEulerAnglesBaked.y";

        /// <summary>
        /// Local baked Euler angles Z property name used during animation processing.
        /// </summary>
        public const string LocalEulerAnglesBakedZProperty = "localEulerAnglesBaked.z";

        /// <summary>
        /// Local scale X property name used during animation processing.
        /// </summary>
        public const string LocalScaleXProperty = "localScale.x";

        /// <summary>
        /// Local scale Y property name used during animation processing.
        /// </summary>
        public const string LocalScaleYProperty = "localScale.y";

        /// <summary>
        /// Local scale Z property name used during animation processing.
        /// </summary>
        public const string LocalScaleZProperty = "localScale.z";

        /// <summary>
        /// Blend shape animation path format.
        /// </summary>
        /// <remarks>
        /// Use <see cref="string.Format(string,object)"/> to inject the blend shape name.
        /// </remarks>
        public const string BlendShapePathFormat = "blendShape.{0}";

        /// <summary>
        /// Root position X property name used during Mecanim animation processing.
        /// </summary>
        public const string RootPositionXProperty = "RootT.x";

        /// <summary>
        /// Root position Y property name used during Mecanim animation processing.
        /// </summary>
        public const string RootPositionYProperty = "RootT.y";

        /// <summary>
        /// Root position Z property name used during Mecanim animation processing.
        /// </summary>
        public const string RootPositionZProperty = "RootT.z";

        /// <summary>
        /// Root rotation X property name used during Mecanim animation processing.
        /// </summary>
        public const string RootRotationXProperty = "RootQ.x";

        /// <summary>
        /// Root rotation Y property name used during Mecanim animation processing.
        /// </summary>
        public const string RootRotationYProperty = "RootQ.y";

        /// <summary>
        /// Root rotation Z property name used during Mecanim animation processing.
        /// </summary>
        public const string RootRotationZProperty = "RootQ.z";

        /// <summary>
        /// Root rotation W property name used during Mecanim animation processing.
        /// </summary>
        public const string RootRotationWProperty = "RootQ.w";

        #endregion

        /// <summary>
        /// Starting index value used to separate quad geometries from triangle
        /// geometries when <c>AssetLoaderOptions.UseQuads</c> is enabled.
        /// </summary>
        /// <remarks>
        /// Unity requires quad and triangle meshes to be handled as separate geometries.
        /// </remarks>
        public const int QuadGeometryIndicesBegin = short.MaxValue;

        /// <summary>
        /// Common number of animation curves generated per animated transform.
        /// </summary>
        public const int CommonAnimationCurveCount = 10;
    }
}
