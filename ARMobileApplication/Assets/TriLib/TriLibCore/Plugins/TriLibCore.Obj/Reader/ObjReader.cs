using System;
using System.IO;

using TriLibCore.Interfaces;

namespace TriLibCore.Obj.Reader
{
    public partial class ObjReader : ReaderBase
    {
        /// <summary>
        /// Turn on this field to parse OBJ file number data as doubles and improve precision.
        /// </summary>
        public static bool ParseNumbersAsDouble = false;

        /// <summary>
        /// Experimental: Any value passed here will be multiplied with any "double precision" data when converting them to floats.
        /// </summary>
        public static double ObjConversionPrecision = 1.0;

        /// <summary>
        /// Turn off this field to disable OBJ vertex color reading.
        /// </summary>
        public static bool ParseVertexColors = true;

        internal enum ProcessingSteps
        {
            Parsing,
            PostProcessGeometry
        }

        public static string[] GetExtensions()
        {
            return new[] {"obj"};
        }

        public override string Name => "OBJ";

        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = new ObjProcessor().Process(this, stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new ObjRootModel();
        }
    }
}
