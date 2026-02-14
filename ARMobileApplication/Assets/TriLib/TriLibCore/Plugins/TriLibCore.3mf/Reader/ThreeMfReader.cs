using System;
using System.IO;

using TriLibCore.Interfaces;

namespace TriLibCore.ThreeMf.Reader
{
    public class ThreeMfReader : ReaderBase
    {
        /// <summary>
        /// Experimental: Any value passed here will be multiplied with any "double precision" data when converting them to floats.
        /// </summary>
        public static double ThreeMfConversionPrecision = 1.0;

        internal enum ProcessingSteps
        {
            Parsing,
            Processing
        }

        public override string Name => "3MF";
        internal int ModelCount { get; set; }
        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public static string[] GetExtensions()
        {
            return new[] { "3mf" };
        }
        
        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = new ThreeMfProcessor().Process(this, stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new ThreeMfRootModel();
        }
    }
}