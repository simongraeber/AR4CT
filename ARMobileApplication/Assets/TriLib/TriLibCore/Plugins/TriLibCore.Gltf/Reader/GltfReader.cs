using System;
using System.IO;
using TriLibCore.Interfaces;

namespace TriLibCore.Gltf.Reader
{
    public partial class GltfReader : ReaderBase
    {
        public static Func<byte[], GltfTempGeometryGroup> DracoDecompressorCallback;

        /// <summary>
        /// Spot light distance to apply on conversion.
        /// </summary>

        public static float SpotLightDistance = 100f;

        private readonly GtlfProcessor _gtlfProcessor;

        public GltfReader()
        {
            _gtlfProcessor = new GtlfProcessor(this);
        }
        internal enum ProcessingSteps
        {
            Parsing,
            LoadBuffers,
            ConvertTextures,
            ConvertMaterials,
            ConvertGeometryGroups,
            ConvertScenes,
            ConvertBindPoses,
            PostProcessModels,
            ConvertAnimations
        }

        public override string Name => "GLTF2";

        protected override Type LoadingStepEnumType => typeof(ProcessingSteps);

        public static string[] GetExtensions()
        {
            return new[] { "gltf", "glb"};
        }
        
        public override IRootModel ReadStream(Stream stream, AssetLoaderContext assetLoaderContext, string filename = null, Action<AssetLoaderContext, float> onProgress = null)
        {
            base.ReadStream(stream, assetLoaderContext, filename, onProgress);
            SetupStream(ref stream);
            var model = _gtlfProcessor.ParseModel(stream);
            PostProcessModel(ref model);
            return model;
        }

        protected override IRootModel CreateRootModel()
        {
            return new GltfRootModel();
        }
    }
}
