using System;
using System.IO;
using System.Text;
using TriLibCore.Extensions;
using TriLibCore.Fbx.ASCII;
using TriLibCore.Fbx.Binary;
using TriLibCore.Interfaces;

namespace TriLibCore.Fbx.Reader
{
    public class FbxReader : ReaderBase
    {
        /// <summary>
        /// Turn on this field to apply ambient color to Phong materials.
        /// </summary>
        public static bool ApplyAmbientColor;

        /// <summary>
        /// Any value passed here will be multiplied with any "double precision" data when converting them to floats.
        /// </summary>
        public static double FBXConversionPrecision = 1.0;

        /// <summary>
        /// Change field to define how TriLib will handle FBX object pivots. The "legacy" (default) option don't use pivots.
        /// </summary>
        public static FBXPivotMode PivotMode = FBXPivotMode.PreservePivot;

        internal enum ProcessingSteps
        {
            Parsing,
            ProcessHeaderExtension,
            ProcessGlobalSettings,
            ProcessDefinitions,
            ProcessObjects,
            ProcessConnections,
            PostProcessModels,
            PostProcessPivots,
            PostProcessGeometries,
            PostProcessAnimations,
            PostProcessCameras,
            PostProcessTextures
        }

        public override string Name => "FBX";
        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);


        public static string[] GetExtensions()
        {
            return new[] { "fbx" };
        }

        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            if (IsBinary(stream))
            {
                var model = ParseBinary(stream);
                PostProcessModel(ref model);
                return model;
            }
            if (IsASCII(stream))
            {
                var model = ParseASCII(stream);
                PostProcessModel(ref model);
                return model;
            }
            return null;
        }

        protected override IRootModel CreateRootModel()
        {
            var document = new FBXDocument();
            document.Reader = this;
            return document;
        }

        private static bool IsASCII(Stream stream)
        {
            return stream.MatchRegex("FBXHeaderExtension\\s*:");
        }

        private static bool IsBinary(Stream stream)
        {
            var isBinary = false;
            var identifierBytes = new byte[18];
            stream.Read(identifierBytes, 0, 18);
            var identifier = Encoding.UTF8.GetString(identifierBytes);
            if (identifier == "Kaydara FBX Binary")
            {
                isBinary = true;
            }
            stream.Seek(0, SeekOrigin.Begin);
            return isBinary;
        }

        private IRootModel ParseASCII(Stream stream)
        {
            var processor = new FBXProcessor(this);
            var asciiReader = new FBXASCIIReader(processor, stream, !AssetLoaderContext.Options.CloseStreamAutomatically);
            var rootNode = asciiReader.ReadASCIIDocument();
            UpdateLoadingPercentage(1f, (int)FbxReader.ProcessingSteps.Parsing);
            var rootModel = processor.Process(rootNode, false);
            if (AssetLoaderContext.Options.CloseStreamAutomatically)
            {
                asciiReader.Dispose();
            }
            return rootModel;
        }

        
        private IRootModel ParseBinary(Stream stream)
        {
            var processor = new FBXProcessor(this);
            var fbxBinaryReader = new FBXBinaryReader(processor, stream, this, !AssetLoaderContext.Options.CloseStreamAutomatically);
            var rootNode = fbxBinaryReader.ReadBinaryDocument();
            var rootModel = processor.Process(rootNode, true);
            if (AssetLoaderContext.Options.CloseStreamAutomatically)
            {
                fbxBinaryReader.Dispose();
            }
            return rootModel;
        }
    }
}
