using System;
using System.IO;
using TriLibCore.Interfaces;

namespace TriLibCore.Stl.Reader
{
    public partial class StlReader : ReaderBase
    {

        /// <summary>
        /// Turn on this field to fix in-facing normals on STL files.
        /// </summary>
        public static bool FixInfacingNormals = false;

        /// <summary>
        /// Turn on this field to enable STL normal importing.
        /// </summary>
        public static bool ImportNormals = false;

        /// <summary>
        /// Turn on this field to load your model with the Z axis pointing up.
        /// </summary>
        public static bool LoadWithYUp = false;

        /// <summary>
        /// Turn off this field to stop storing the index of the triangle in the X component of the mesh texture coordinates.
        /// </summary>
        public static bool StoreTriangleIndexInTexCoord0 = true;

        internal enum ProcessingSteps
        {
            Parsing,
            ProcessTriangle
        }
        public override string Name => "STL";

        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public static string[] GetExtensions()
        {
            return new[] {"stl"};
        }

        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = new StlProcessor().Process(this, stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new StlRootModel();
        }
    }
}