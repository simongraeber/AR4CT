using System;
using System.IO;
using System.Threading;
using TriLibCore.Interfaces;

namespace TriLibCore.Dae.Reader
{
    public class DaeReader : ReaderBase
    {
        /// <summary>
        /// Experimental: Any value passed here will be multiplied with any "double precision" data when converting them to floats.
        /// </summary>
        public static double DaeConversionPrecision = 1.0;

        internal enum ProcessingSteps
        {
            Parsing,
            PostProcessGeometry
        }

        public static string[] GetExtensions()
        {
            return new[] { "dae" };
        }

        public override string Name => "DAE";
        public override int LoadingStepsCount => 2;
        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = new DaeProcessor().Process(this, stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new DaeRootModel();
        }
    }
}