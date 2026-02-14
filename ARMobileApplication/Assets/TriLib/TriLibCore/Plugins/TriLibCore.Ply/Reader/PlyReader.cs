using System;
using System.IO;

using TriLibCore.Interfaces;

namespace TriLibCore.Ply.Reader
{
    public class PlyReader : ReaderBase
    {
        /// <summary>
        /// Experimental: Any value passed here will be multiplied with any "double precision" data when converting them to floats.
        /// </summary>
        public static double PlyConversionPrecision = 1.0;

        internal enum ProcessingSteps
        {
            Parsing,
            ConvertMaterials,
            ConvertGeometryGroups,
            PostProcessGeometries
        }

        public override string Name => "PLY";

        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public static string[] GetExtensions()
        {
            return new[] { "ply" };
        }

        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = new PlyProcessor().Process(this, stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new PlyRootModel();
        }
    }
}